using System;
using System.Collections.Generic;
using UnityEngine;
using DungeonCrawler.Player;

namespace DungeonCrawler.Map
{
    public class DungeonMapManager : MonoBehaviour
    {
        public static DungeonMapManager Instance { get; private set; }

        [Header("Grid Configuration")]
        [SerializeField] private float stepSize = PlayerGridController.GRID_STEP_SIZE;
        [SerializeField] private int sightDistance = 1; // Number of tiles revealed ahead

        [Header("Runtime State")]
        [SerializeField] private Vector2Int currentGridPosition;
        [SerializeField] private float currentRotationY;

        private readonly HashSet<Vector2Int> visitedTiles = new HashSet<Vector2Int>();
        private Transform playerTransform;
        private PlayerGridController playerController;

        public Vector2Int CurrentGridPosition => currentGridPosition;
        public float CurrentRotationY => currentRotationY;
        public float StepSize => (playerController != null ? playerController.StepSize : (PlayerGridController.Instance != null ? PlayerGridController.Instance.StepSize : PlayerGridController.GRID_STEP_SIZE));
        public IReadOnlyCollection<Vector2Int> VisitedTiles => visitedTiles;

        /// <summary>
        /// Invoked whenever the player's grid coordinate or rotation (Y euler) updates.
        /// Parameters: (Vector2Int gridPosition, float yRotationAngle)
        /// </summary>
        public event Action<Vector2Int, float> OnPlayerMovedOnGrid;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindPlayer();
            SyncGridSettings();
            if (playerTransform != null)
            {
                currentGridPosition = WorldToGrid(playerTransform.position);
                currentRotationY = Mathf.Round(playerTransform.eulerAngles.y / 90f) * 90f;
            }
            UpdatePlayerMapState(true);
        }

        private void SyncGridSettings()
        {
            if (playerController != null)
            {
                stepSize = playerController.StepSize;
            }
            else if (PlayerGridController.Instance != null)
            {
                stepSize = PlayerGridController.Instance.StepSize;
            }
            else
            {
                stepSize = PlayerGridController.GRID_STEP_SIZE;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            Vector2Int newGridPos = WorldToGrid(playerTransform.position);
            float newRotY = Mathf.Round(playerTransform.eulerAngles.y / 90f) * 90f;

            if (newGridPos != currentGridPosition || !Mathf.Approximately(newRotY, currentRotationY))
            {
                currentGridPosition = newGridPos;
                currentRotationY = newRotY;
                UpdatePlayerMapState(false);
            }
        }

        private void FindPlayer()
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO == null)
            {
                playerController = FindFirstObjectByType<PlayerGridController>();
                if (playerController != null) playerGO = playerController.gameObject;
            }

            if (playerGO != null)
            {
                playerTransform = playerGO.transform;
                playerController = playerGO.GetComponent<PlayerGridController>();
            }
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float size = StepSize;
            int gridX = Mathf.RoundToInt(worldPos.x / size);
            int gridY = Mathf.RoundToInt(worldPos.z / size);
            return new Vector2Int(gridX, gridY);
        }

        public Vector3 GridToWorld(Vector2Int gridPos, float y = 0f)
        {
            float size = StepSize;
            return new Vector3(gridPos.x * size, y, gridPos.y * size);
        }

        public bool IsTileVisited(Vector2Int gridPos)
        {
            return visitedTiles.Contains(gridPos);
        }

        public void RevealTile(Vector2Int gridPos)
        {
            if (visitedTiles.Add(gridPos))
            {
                OnPlayerMovedOnGrid?.Invoke(currentGridPosition, currentRotationY);
            }
        }

        private void UpdatePlayerMapState(bool forceNotify)
        {
            bool anyNewTile = false;

            // Reveal current tile
            if (visitedTiles.Add(currentGridPosition))
            {
                anyNewTile = true;
            }

            // Reveal tiles in facing direction up to sightDistance
            Vector2Int facingDir = GetFacingDirection(currentRotationY);
            for (int i = 1; i <= sightDistance; i++)
            {
                Vector2Int aheadTile = currentGridPosition + facingDir * i;
                if (visitedTiles.Add(aheadTile))
                {
                    anyNewTile = true;
                }
            }

            if (anyNewTile || forceNotify)
            {
                OnPlayerMovedOnGrid?.Invoke(currentGridPosition, currentRotationY);
            }
        }

        private Vector2Int GetFacingDirection(float rotY)
        {
            int normalized = (Mathf.RoundToInt(rotY) % 360 + 360) % 360;
            switch (normalized)
            {
                case 0: return new Vector2Int(0, 1);    // North (+Z)
                case 90: return new Vector2Int(1, 0);   // East (+X)
                case 180: return new Vector2Int(0, -1); // South (-Z)
                case 270: return new Vector2Int(-1, 0); // West (-X)
                default: return new Vector2Int(0, 1);
            }
        }
    }
}
