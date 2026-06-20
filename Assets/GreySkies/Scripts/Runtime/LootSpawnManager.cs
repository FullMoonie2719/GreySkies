using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace GreySkies
{
    public class LootSpawnManager : NetworkBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("List of manual spawn points")]
        [SerializeField] private List<Transform> _manualSpawnPoints = new List<Transform>();
        
        [Tooltip("List of item types that can be spawned")]
        [SerializeField] private List<InventoryItemData> _lootPool = new List<InventoryItemData>();
        
        [Tooltip("How many seconds between spawn checks/spawns")]
        [SerializeField] private float _spawnInterval = 30f;
        
        [Tooltip("Max items that can be active in the world at once")]
        [SerializeField] private int _maxActiveLoot = 20;

        private List<Vector3> _calculatedSpawnNodes = new List<Vector3>();
        private List<GameObject> _activeLootObjects = new List<GameObject>();
        private float _spawnTimer;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            
            GenerateSpawnNodesAroundProps();
            SpawnInitialLoot();
        }

        private void Update()
        {
            if (!IsServer) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                // Clean up any null/destroyed references in our active list
                _activeLootObjects.RemoveAll(item => item == null);
                
                if (_activeLootObjects.Count < _maxActiveLoot)
                {
                    SpawnLootAtRandomNode();
                }
            }
        }

        private void GenerateSpawnNodesAroundProps()
        {
            _calculatedSpawnNodes.Clear();
            
            // Add manual spawn points
            foreach (var pt in _manualSpawnPoints)
            {
                if (pt != null) _calculatedSpawnNodes.Add(pt.position);
            }

            // Find all Tudor Pubs and Telephone Boxes in the scene to generate nodes around them
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
            foreach (var obj in allObjects)
            {
                string nameLower = obj.name.ToLower();
                if (nameLower.Contains("kentishtudorpub") || nameLower.Contains("tudorpub"))
                {
                    // Generate 4-6 random nodes around the pub
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 offset = new Vector3(
                            Random.Range(-8f, 8f),
                            0.5f,
                            Random.Range(-6f, 6f)
                        );
                        Vector3 spawnPos = obj.transform.position + offset;
                        if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
                        {
                            spawnPos.y = hit.point.y + 0.1f;
                        }
                        _calculatedSpawnNodes.Add(spawnPos);
                    }
                }
                else if (nameLower.Contains("k6redtelephonebox") || nameLower.Contains("telephonebox"))
                {
                    // Generate 2-3 nodes near the phone box
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 offset = new Vector3(
                            Random.Range(-2f, 2f),
                            0.5f,
                            Random.Range(-2f, 2f)
                        );
                        Vector3 spawnPos = obj.transform.position + offset;
                        if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
                        {
                            spawnPos.y = hit.point.y + 0.1f;
                        }
                        _calculatedSpawnNodes.Add(spawnPos);
                    }
                }
            }
            
            Debug.Log($"[LootSpawnManager] Generated {_calculatedSpawnNodes.Count} spawn nodes.");
        }

        private void SpawnInitialLoot()
        {
            if (_calculatedSpawnNodes.Count == 0 || _lootPool.Count == 0) return;

            int initialSpawnCount = Mathf.Min(_calculatedSpawnNodes.Count / 2, _maxActiveLoot);
            for (int i = 0; i < initialSpawnCount; i++)
            {
                SpawnLootAtRandomNode();
            }
        }

        private void SpawnLootAtRandomNode()
        {
            if (_calculatedSpawnNodes.Count == 0 || _lootPool.Count == 0) return;

            Vector3 spawnPos = _calculatedSpawnNodes[Random.Range(0, _calculatedSpawnNodes.Count)];
            InventoryItemData randomItemData = _lootPool[Random.Range(0, _lootPool.Count)];

            if (randomItemData != null && randomItemData.Prefab != null)
            {
                GameObject lootInstance = Instantiate(randomItemData.Prefab, spawnPos, Quaternion.identity);
                NetworkObject netObj = lootInstance.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                    
                    // Set up the pickable item properties
                    PickableItem pickable = lootInstance.GetComponent<PickableItem>();
                    if (pickable != null)
                    {
                        pickable.ItemID.Value = randomItemData.ItemID;
                    }
                    else
                    {
                        // Add PickableItem dynamically if not present
                        pickable = lootInstance.AddComponent<PickableItem>();
                        pickable.ItemID.Value = randomItemData.ItemID;
                    }

                    _activeLootObjects.Add(lootInstance);
                    Debug.Log($"[LootSpawnManager] Spawned loot: {randomItemData.ItemID} at {spawnPos}");
                }
                else
                {
                    Debug.LogError($"[LootSpawnManager] Prefab for item {randomItemData.ItemID} is missing a NetworkObject component!");
                    Destroy(lootInstance);
                }
            }
        }
    }
}