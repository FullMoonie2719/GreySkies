using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace GreySkies
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkInventory : NetworkBehaviour
    {
        [Header("Grid Settings")]
        [Tooltip("Width of the inventory grid")]
        [SerializeField] private int _gridWidth = 10;
        [Tooltip("Height of the inventory grid")]
        [SerializeField] private int _gridHeight = 10;

        [Header("Item Database")]
        [Tooltip("List of all item types that can be stored in this inventory")]
        [SerializeField] private List<InventoryItemData> _itemDatabase = new List<InventoryItemData>();

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;

        // Track items inside the grid using NetworkList.
        // It's server-authoritative by default (write permission = Server).
        public readonly NetworkList<InventoryItemInstance> Items = new NetworkList<InventoryItemInstance>();

        private Dictionary<string, InventoryItemData> _itemDatabaseDict;

        private void Awake()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            _itemDatabaseDict = new Dictionary<string, InventoryItemData>();
            if (_itemDatabase != null)
            {
                foreach (var item in _itemDatabase)
                {
                    if (item != null && !string.IsNullOrEmpty(item.ItemID))
                    {
                        _itemDatabaseDict[item.ItemID] = item;
                    }
                }
            }
        }

        public InventoryItemData GetItemData(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            
            // Check dictionary first
            if (_itemDatabaseDict != null && _itemDatabaseDict.TryGetValue(itemId, out var data))
            {
                return data;
            }

            // Fallback (e.g. if Awake hasn't run yet or list was modified)
            if (_itemDatabase != null)
            {
                foreach (var item in _itemDatabase)
                {
                    if (item != null && item.ItemID == itemId)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if an item can be placed at the specified slot.
        /// </summary>
        public bool CanPlaceItem(string itemId, int x, int y, bool rotated)
        {
            return CanPlaceItem(itemId, x, y, rotated, "");
        }

        /// <summary>
        /// Checks if an item can be placed, optionally ignoring a specific item instance (useful when moving items).
        /// </summary>
        public bool CanPlaceItem(string itemId, int x, int y, bool rotated, string ignoredInstanceId)
        {
            var itemData = GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[NetworkInventory] Item ID {itemId} not found in database.");
                return false;
            }

            int width = rotated ? itemData.Height : itemData.Width;
            int height = rotated ? itemData.Width : itemData.Height;

            // Bounds check
            if (x < 0 || y < 0 || x + width > _gridWidth || y + height > _gridHeight)
            {
                return false;
            }

            // Overlap check
            foreach (var item in Items)
            {
                if (!string.IsNullOrEmpty(ignoredInstanceId) && item.Guid.ToString() == ignoredInstanceId)
                {
                    continue;
                }

                var existingData = GetItemData(item.ItemID.ToString());
                if (existingData == null)
                {
                    // If we can't find the existing item's data, we log a warning but skip to avoid getting stuck.
                    Debug.LogWarning($"[NetworkInventory] Item ID {item.ItemID} inside inventory is not found in database.");
                    continue;
                }

                int existingWidth = item.isRotated ? existingData.Height : existingData.Width;
                int existingHeight = item.isRotated ? existingData.Width : existingData.Height;

                // Overlap test: Two rectangles overlap if:
                // x1 < x2 + w2 AND x2 < x1 + w1 AND y1 < y2 + h2 AND y2 < y1 + h1
                if (x < item.slotX + existingWidth &&
                    item.slotX < x + width &&
                    y < item.slotY + existingHeight &&
                    item.slotY < y + height)
                {
                    return false; // Overlap detected
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to place an item on the server.
        /// </summary>
        public bool TryPlaceItem(string itemId, int x, int y, bool rotated)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkInventory] TryPlaceItem must be called on the Server.");
                return false;
            }

            if (!CanPlaceItem(itemId, x, y, rotated))
            {
                return false;
            }

            string guid = Guid.NewGuid().ToString("N");

            var instance = new InventoryItemInstance
            {
                Guid = guid,
                ItemID = itemId,
                slotX = x,
                slotY = y,
                isRotated = rotated
            };

            Items.Add(instance);
            return true;
        }

        /// <summary>
        /// Attempts to find an open slot and place the item. Server-only.
        /// </summary>
        public bool AutoPlaceItem(string itemId)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkInventory] AutoPlaceItem must be called on the Server.");
                return false;
            }

            var data = GetItemData(itemId);
            if (data == null)
            {
                Debug.LogWarning($"[NetworkInventory] AutoPlaceItem: Item ID {itemId} not found in database.");
                return false;
            }

            // Search the grid for a free slot (unrotated first)
            for (int y = 0; y <= _gridHeight - data.Height; y++)
            {
                for (int x = 0; x <= _gridWidth - data.Width; x++)
                {
                    if (CanPlaceItem(itemId, x, y, false))
                    {
                        if (TryPlaceItem(itemId, x, y, false))
                        {
                            return true;
                        }
                    }
                }
            }

            // Try rotated
            for (int y = 0; y <= _gridHeight - data.Width; y++)
            {
                for (int x = 0; x <= _gridWidth - data.Height; x++)
                {
                    if (CanPlaceItem(itemId, x, y, true))
                    {
                        if (TryPlaceItem(itemId, x, y, true))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Server-authoritative removal of an item from the inventory.
        /// </summary>
        public bool RemoveItem(string instanceId)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkInventory] RemoveItem must be called on the Server.");
                return false;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Guid.ToString() == instanceId)
                {
                    Items.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Server-authoritative RPC to move an item within the grid.
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void MoveItemServerRpc(string instanceId, int targetX, int targetY, bool rotated)
        {
            // Verify item exists
            int itemIndex = -1;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Guid.ToString() == instanceId)
                {
                    itemIndex = i;
                    break;
                }
            }

            if (itemIndex == -1)
            {
                Debug.LogWarning($"[NetworkInventory] MoveItemServerRpc failed: Item instance {instanceId} not found.");
                return;
            }

            var item = Items[itemIndex];

            // Perform authoritative placement check, ignoring the item itself
            if (CanPlaceItem(item.ItemID.ToString(), targetX, targetY, rotated, instanceId))
            {
                var updatedItem = item;
                updatedItem.slotX = targetX;
                updatedItem.slotY = targetY;
                updatedItem.isRotated = rotated;

                Items[itemIndex] = updatedItem;
                Debug.Log($"[NetworkInventory] Moved item {instanceId} ({item.ItemID}) to ({targetX}, {targetY}), rotated={rotated}");
            }
            else
            {
                Debug.LogWarning($"[NetworkInventory] MoveItemServerRpc ignored: Invalid placement for {instanceId} at ({targetX}, {targetY}).");
            }
        }
    }
}
