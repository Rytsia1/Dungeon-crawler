#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Data;

namespace DungeonCrawler.Editor
{
    public static class DungeonBuilder
    {
        [MenuItem("DungeonCrawler/Build Prototype Dungeon")]
        public static void BuildDungeon()
        {
            // 1. Find or create root _Dungeon
            GameObject dungeonRoot = GameObject.Find("_Dungeon");
            if (dungeonRoot == null)
            {
                dungeonRoot = new GameObject("_Dungeon");
            }

            // Clear previous children if any
            for (int i = dungeonRoot.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(dungeonRoot.transform.GetChild(i).gameObject);
            }

            GameObject floorsGroup = new GameObject("Floors");
            floorsGroup.transform.SetParent(dungeonRoot.transform);

            GameObject wallsGroup = new GameObject("Walls");
            wallsGroup.transform.SetParent(dungeonRoot.transform);

            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (obstacleLayer == -1)
            {
                Debug.LogWarning("Obstacle layer not found, using Default layer.");
                obstacleLayer = 0;
            }

            Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            floorMat.color = new Color(0.2f, 0.22f, 0.25f);

            Material wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            wallMat.color = new Color(0.4f, 0.42f, 0.45f);

            Material enemyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            enemyMat.color = new Color(0.85f, 0.15f, 0.15f);

            // Walkable grid coordinates (L-corridor)
            Vector2Int[] floorCoords = new Vector2Int[]
            {
                new Vector2Int(0, 0), // Start (Player position: 0, 1, 0)
                new Vector2Int(0, 1),
                new Vector2Int(0, 2),
                new Vector2Int(0, 3), // Turn right
                new Vector2Int(1, 3),
                new Vector2Int(2, 3)  // Enemy position: (4, 1, 6)
            };

            foreach (var coord in floorCoords)
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = $"Floor_{coord.x}_{coord.y}";
                floor.transform.SetParent(floorsGroup.transform);
                floor.transform.position = new Vector3(coord.x * 2f, -0.1f, coord.y * 2f);
                floor.transform.localScale = new Vector3(2f, 0.2f, 2f);
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            }

            // Surrounding Wall coordinates (Obstacle layer)
            Vector2Int[] wallCoords = new Vector2Int[]
            {
                new Vector2Int(-1, -1),
                new Vector2Int(0, -1), // Behind player
                new Vector2Int(1, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 1),
                new Vector2Int(1, 1),
                new Vector2Int(-1, 2),
                new Vector2Int(1, 2),
                new Vector2Int(-1, 3),
                new Vector2Int(-1, 4),
                new Vector2Int(0, 4),  // End of straight corridor
                new Vector2Int(1, 4),
                new Vector2Int(2, 4),
                new Vector2Int(3, 4),
                new Vector2Int(3, 3),  // Behind enemy
                new Vector2Int(3, 2),
                new Vector2Int(2, 2)
            };

            foreach (var coord in wallCoords)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall_{coord.x}_{coord.y}";
                wall.transform.SetParent(wallsGroup.transform);
                wall.transform.position = new Vector3(coord.x * 2f, 1f, coord.y * 2f);
                wall.transform.localScale = new Vector3(2f, 2f, 2f);
                wall.layer = obstacleLayer;
                wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            }

            // Enemy GameObject at (2, 3) -> world (4, 1, 6)
            GameObject enemyObj = GameObject.Find("Enemy_Skeleton");
            if (enemyObj != null)
            {
                Undo.DestroyObjectImmediate(enemyObj);
            }

            enemyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            enemyObj.name = "Enemy_Skeleton";
            enemyObj.transform.position = new Vector3(4f, 1f, 6f);
            enemyObj.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            enemyObj.GetComponent<Renderer>().sharedMaterial = enemyMat;

            // Trigger collider setup
            Collider col = enemyObj.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Add EncounterTrigger
            EncounterTrigger trigger = enemyObj.AddComponent<EncounterTrigger>();
            EnemyData skeletonData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/SampleSkeleton.asset");
            if (skeletonData != null)
            {
                trigger.Enemy = skeletonData;
                EditorUtility.SetDirty(trigger);
                Debug.Log("[DungeonBuilder] SampleSkeleton.asset successfully assigned to EncounterTrigger!");
            }
            else
            {
                Debug.LogWarning("[DungeonBuilder] SampleSkeleton.asset not found at Assets/Data/SampleSkeleton.asset");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("[DungeonBuilder] Prototype Dungeon built successfully with floors, walls, and Enemy_Skeleton!");
        }
    }
}
#endif
