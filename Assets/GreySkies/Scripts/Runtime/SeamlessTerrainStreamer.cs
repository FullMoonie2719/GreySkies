using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace GreySkies
{
    public class SeamlessTerrainStreamer : MonoBehaviour
    {
        [Header("Streaming Settings")]
        [Tooltip("Size of each grid sector in world units (meters).")]
        public float SectorSize = 500.0f;

        [Tooltip("How many adjacent sectors around the player to keep loaded (1 = 3x3 grid).")]
        public int LoadRadius = 1;

        [Tooltip("The naming prefix of the sector scenes (e.g., 'Kent_Sector_').")]
        public string ScenePrefix = "Kent_Sector_";

        [Header("Target Tracking")]
        [Tooltip("The player transform to track. If null, will try to find the local NetworkPlayerController.")]
        public Transform TrackedTarget;

        private Vector2Int _currentSector = new Vector2Int(-999, -999);
        private readonly HashSet<Vector2Int> _loadedSectors = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _loadingSectors = new HashSet<Vector2Int>();

        private void Start()
        {
            StartCoroutine(StreamingUpdateRoutine());
        }

        private IEnumerator StreamingUpdateRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(1.0f);
            while (true)
            {
                if (TrackedTarget == null)
                {
                    // Attempt to locate local player
                    var localController = FindAnyObjectByType<NetworkPlayerController>();
                    if (localController != null && localController.IsOwner)
                    {
                        TrackedTarget = localController.transform;
                    }
                }

                if (TrackedTarget != null)
                {
                    UpdateStreaming(TrackedTarget.position);
                }

                yield return wait;
            }
        }

        private void UpdateStreaming(Vector3 position)
        {
            // Calculate current sector coordinate
            int x = Mathf.FloorToInt(position.x / SectorSize);
            int z = Mathf.FloorToInt(position.z / SectorSize);
            Vector2Int targetSector = new Vector2Int(x, z);

            if (targetSector != _currentSector)
            {
                _currentSector = targetSector;
                RefreshSectors(_currentSector);
            }
        }

        private void RefreshSectors(Vector2Int centerSector)
        {
            HashSet<Vector2Int> desiredSectors = new HashSet<Vector2Int>();

            // Determine which sectors should be loaded
            for (int dx = -LoadRadius; dx <= LoadRadius; dx++)
            {
                for (int dz = -LoadRadius; dz <= LoadRadius; dz++)
                {
                    desiredSectors.Add(new Vector2Int(centerSector.x + dx, centerSector.y + dz));
                }
            }

            // Unload distant sectors
            List<Vector2Int> sectorsToUnload = new List<Vector2Int>();
            foreach (var loaded in _loadedSectors)
            {
                if (!desiredSectors.Contains(loaded))
                {
                    sectorsToUnload.Add(loaded);
                }
            }

            foreach (var sector in sectorsToUnload)
            {
                StartCoroutine(UnloadSectorRoutine(sector));
            }

            // Load newly entered sectors
            foreach (var desired in desiredSectors)
            {
                if (!_loadedSectors.Contains(desired) && !_loadingSectors.Contains(desired))
                {
                    StartCoroutine(LoadSectorRoutine(desired));
                }
            }
        }

        private IEnumerator LoadSectorRoutine(Vector2Int sector)
        {
            _loadingSectors.Add(sector);
            string sceneName = GetSceneNameForSector(sector);

            // Verify if scene is build-indexable (only load if it can be loaded)
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                while (asyncLoad != null && !asyncLoad.isDone)
                {
                    yield return null;
                }
                _loadedSectors.Add(sector);
            }
            else
            {
                // Sector scene is not defined/built yet, which is expected during expansion
                Debug.LogWarning($"SeamlessTerrainStreamer: Sector scene '{sceneName}' cannot be loaded. Ensure it is added to build settings.");
            }

            _loadingSectors.Remove(sector);
        }

        private IEnumerator UnloadSectorRoutine(Vector2Int sector)
        {
            _loadedSectors.Remove(sector);
            string sceneName = GetSceneNameForSector(sector);

            // Double check if scene is actually loaded
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
                while (asyncUnload != null && !asyncUnload.isDone)
                {
                    yield return null;
                }
            }
        }

        private string GetSceneNameForSector(Vector2Int sector)
        {
            return $"{ScenePrefix}{sector.x}_{sector.y}";
        }
    }
}