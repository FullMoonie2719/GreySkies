using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace GreySkies
{
    [RequireComponent(typeof(NetworkObject))]
    public class PickableItem : NetworkBehaviour
    {
        [Header("Item Configuration")]
        [Tooltip("The unique identifier of this pickable item matching database definitions")]
        public NetworkVariable<FixedString32Bytes> ItemID = new NetworkVariable<FixedString32Bytes>(
            "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Tooltip("The quantity of this item")]
        public int Quantity = 1;

        [Tooltip("The proximity radius for interaction")]
        [SerializeField] private float _proximityRadius = 3f;

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void InteractServerRpc(ulong playerClientId)
        {
            if (!IsServer) return;

            // Proximity check
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerClientId, out var client))
            {
                var playerObj = client.PlayerObject;
                if (playerObj != null)
                {
                    float distance = Vector3.Distance(transform.position, playerObj.transform.position);
                    if (distance > _proximityRadius)
                    {
                        Debug.LogWarning($"[PickableItem] Proximity check failed for player {playerClientId}: distance {distance} is too far (max {_proximityRadius}).");
                        return;
                    }

                    NetworkInventory inventory = playerObj.GetComponent<NetworkInventory>();
                    if (inventory != null)
                    {
                        string itemIdStr = ItemID.Value.ToString();
                        if (inventory.AutoPlaceItem(itemIdStr))
                        {
                            Debug.Log($"[PickableItem] Player {playerClientId} successfully picked up item: {itemIdStr} x{Quantity}");
                            GetComponent<NetworkObject>().Despawn(true);
                        }
                        else
                        {
                            Debug.LogWarning($"[PickableItem] Player {playerClientId} inventory full for item: {itemIdStr}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[PickableItem] Player {playerClientId} object is missing a NetworkInventory component!");
                    }
                }
            }
        }
    }
}