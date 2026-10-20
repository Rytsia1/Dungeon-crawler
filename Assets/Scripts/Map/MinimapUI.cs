using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DungeonCrawler.Map
{
    public class MinimapUI : MonoBehaviour
    {
        [Header("Grid Size")]
        [SerializeField] private int gridRadius = 3; // 3 -> (2*3 + 1) = 7x7 tiles
        [SerializeField] private float tileSize = 18f;
        [SerializeField] private float tileSpacing = 2f;

        [Header("Colors")]
        [SerializeField] private Color panelBackgroundColor = new Color(0.04f, 0.05f, 0.08f, 0.92f);
        [SerializeField] private Color visitedFloorColor = new Color(0.20f, 0.25f, 0.35f, 0.85f);
        [SerializeField] private Color fogOfWarColor = new Color(0.05f, 0.05f, 0.07f, 0.95f);
        [SerializeField] private Color wallColor = new Color(0.85f, 0.65f, 0.25f, 0.95f);
        [SerializeField] private Color playerArrowColor = new Color(0.00f, 1.00f, 0.80f, 1.00f);

        [Header("Raycast Collision")]
        [SerializeField] private LayerMask obstacleLayer;

        private RectTransform gridContainer;
        private RectTransform playerArrowRect;
        private TMP_Text titleText;

        private struct TileUI
        {
            public Image floorImage;
            public Image wallNorth;
            public Image wallEast;
            public Image wallSouth;
            public Image wallWest;
        }

        private TileUI[,] tileCells;
        private int gridSize;
        private readonly Dictionary<Vector2Int, byte> cachedWalls = new Dictionary<Vector2Int, byte>();

        private const byte WALL_NORTH = 1 << 0;
        private const byte WALL_EAST  = 1 << 1;
        private const byte WALL_SOUTH = 1 << 2;
        private const byte WALL_WEST  = 1 << 3;

        private void Awake()
        {
            if (obstacleLayer.value == 0)
            {
                obstacleLayer = LayerMask.GetMask("Obstacle");
            }

            BuildMinimapWidget();
        }

        private void Start()
        {
            if (DungeonMapManager.Instance != null)
            {
                DungeonMapManager.Instance.OnPlayerMovedOnGrid += HandlePlayerMoved;
                // Initial render
                HandlePlayerMoved(DungeonMapManager.Instance.CurrentGridPosition, DungeonMapManager.Instance.CurrentRotationY);
            }
        }

        private void OnDestroy()
        {
            if (DungeonMapManager.Instance != null)
            {
                DungeonMapManager.Instance.OnPlayerMovedOnGrid -= HandlePlayerMoved;
            }
        }

        private void BuildMinimapWidget()
        {
            gridSize = gridRadius * 2 + 1;
            tileCells = new TileUI[gridSize, gridSize];

            // 1. Root Container RectTransform
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();

            float totalWidth = gridSize * tileSize + (gridSize - 1) * tileSpacing + 16f;
            float totalHeight = totalWidth + 24f; // extra header space

            rootRect.sizeDelta = new Vector2(totalWidth, totalHeight);

            // Add background panel
            Image bgImage = GetComponent<Image>();
            if (bgImage == null) bgImage = gameObject.AddComponent<Image>();
            bgImage.color = panelBackgroundColor;

            // 2. Title Label (Retro DRPG header)
            GameObject titleGO = new GameObject("MinimapTitle");
            titleGO.transform.SetParent(transform, false);
            titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "MAP [AUTO]";
            titleText.fontSize = 11;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.85f, 0.75f, 0.5f, 1f);

            RectTransform titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -4f);
            titleRect.sizeDelta = new Vector2(0f, 18f);

            // 3. Grid Container
            GameObject gridGO = new GameObject("GridContainer");
            gridGO.transform.SetParent(transform, false);
            gridContainer = gridGO.AddComponent<RectTransform>();
            gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
            gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
            gridContainer.pivot = new Vector2(0.5f, 0.5f);
            gridContainer.anchoredPosition = new Vector2(0f, -8f);
            gridContainer.sizeDelta = new Vector2(gridSize * (tileSize + tileSpacing), gridSize * (tileSize + tileSpacing));

            float startX = -((gridSize - 1) * 0.5f) * (tileSize + tileSpacing);
            float startY = -((gridSize - 1) * 0.5f) * (tileSize + tileSpacing);

            // 4. Instantiate individual tile cells
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    GameObject cellGO = new GameObject($"Cell_{x}_{y}");
                    cellGO.transform.SetParent(gridContainer, false);

                    RectTransform cellRect = cellGO.AddComponent<RectTransform>();
                    cellRect.sizeDelta = new Vector2(tileSize, tileSize);
                    cellRect.anchoredPosition = new Vector2(startX + x * (tileSize + tileSpacing), startY + y * (tileSize + tileSpacing));

                    Image cellImage = cellGO.AddComponent<Image>();
                    cellImage.color = fogOfWarColor;

                    // Wall borders (thin lines on 4 edges)
                    Image wallN = CreateWallBar(cellGO.transform, "WallN", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -1f), new Vector2(0, 2f));
                    Image wallE = CreateWallBar(cellGO.transform, "WallE", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Vector2(-1f, 0), new Vector2(2f, 0));
                    Image wallS = CreateWallBar(cellGO.transform, "WallS", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 1f), new Vector2(0, 2f));
                    Image wallW = CreateWallBar(cellGO.transform, "WallW", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Vector2(1f, 0), new Vector2(2f, 0));

                    tileCells[x, y] = new TileUI
                    {
                        floorImage = cellImage,
                        wallNorth = wallN,
                        wallEast = wallE,
                        wallSouth = wallS,
                        wallWest = wallW
                    };
                }
            }

            // 5. Player Arrow in center cell
            GameObject arrowGO = new GameObject("PlayerArrow");
            arrowGO.transform.SetParent(gridContainer, false);
            playerArrowRect = arrowGO.AddComponent<RectTransform>();
            playerArrowRect.sizeDelta = new Vector2(tileSize, tileSize);
            playerArrowRect.anchoredPosition = Vector2.zero; // centered

            TMP_Text arrowText = arrowGO.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▲";
            arrowText.fontSize = 14;
            arrowText.fontStyle = FontStyles.Bold;
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = playerArrowColor;
        }

        private Image CreateWallBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject wallGO = new GameObject(name);
            wallGO.transform.SetParent(parent, false);

            RectTransform rect = wallGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = wallGO.AddComponent<Image>();
            img.color = wallColor;
            img.enabled = false;
            return img;
        }

        public void HandlePlayerMoved(Vector2Int playerGridPos, float playerRotY)
        {
            if (tileCells == null || DungeonMapManager.Instance == null) return;

            // 1. Rotate player icon
            if (playerArrowRect != null)
            {
                playerArrowRect.localEulerAngles = new Vector3(0f, 0f, -playerRotY);
            }

            // 2. Update each cell relative to player
            for (int cy = 0; cy < gridSize; cy++)
            {
                for (int cx = 0; cx < gridSize; cx++)
                {
                    int dx = cx - gridRadius;
                    int dy = cy - gridRadius;

                    Vector2Int targetWorldTile = playerGridPos + new Vector2Int(dx, dy);
                    TileUI cell = tileCells[cx, cy];

                    bool isVisited = DungeonMapManager.Instance.IsTileVisited(targetWorldTile);

                    if (isVisited)
                    {
                        cell.floorImage.color = visitedFloorColor;

                        // Check and display walls
                        byte walls = GetOrDetectWalls(targetWorldTile);
                        cell.wallNorth.enabled = (walls & WALL_NORTH) != 0;
                        cell.wallEast.enabled  = (walls & WALL_EAST) != 0;
                        cell.wallSouth.enabled = (walls & WALL_SOUTH) != 0;
                        cell.wallWest.enabled  = (walls & WALL_WEST) != 0;
                    }
                    else
                    {
                        cell.floorImage.color = fogOfWarColor;
                        cell.wallNorth.enabled = false;
                        cell.wallEast.enabled = false;
                        cell.wallSouth.enabled = false;
                        cell.wallWest.enabled = false;
                    }
                }
            }
        }

        private byte GetOrDetectWalls(Vector2Int tile)
        {
            if (cachedWalls.TryGetValue(tile, out byte mask))
            {
                return mask;
            }

            float step = DungeonMapManager.Instance != null ? DungeonMapManager.Instance.StepSize : 2f;
            Vector3 center = new Vector3(tile.x * step, 1.0f, tile.y * step);
            float checkDist = step * 0.55f;

            mask = 0;
            if (Physics.Raycast(center, Vector3.forward, checkDist, obstacleLayer)) mask |= WALL_NORTH;
            if (Physics.Raycast(center, Vector3.right, checkDist, obstacleLayer))   mask |= WALL_EAST;
            if (Physics.Raycast(center, Vector3.back, checkDist, obstacleLayer))    mask |= WALL_SOUTH;
            if (Physics.Raycast(center, Vector3.left, checkDist, obstacleLayer))    mask |= WALL_WEST;

            cachedWalls[tile] = mask;
            return mask;
        }
    }
}
