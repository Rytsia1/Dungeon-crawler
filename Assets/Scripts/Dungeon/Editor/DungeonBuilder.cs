#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Data;
using DungeonCrawler.Interaction;
using DungeonCrawler.Player;

namespace DungeonCrawler.Editor
{
    public static class DungeonBuilder
    {
        [MenuItem("DungeonCrawler/Build Complete Dungeon")]
        public static void BuildDungeon()
        {
            // 1. Root _Dungeon
            GameObject dungeonRoot = GameObject.Find("_Dungeon");
            if (dungeonRoot == null)
            {
                dungeonRoot = new GameObject("_Dungeon");
            }

            for (int i = dungeonRoot.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(dungeonRoot.transform.GetChild(i).gameObject);
            }

            GameObject floorsGroup = new GameObject("Floors");
            floorsGroup.transform.SetParent(dungeonRoot.transform);

            GameObject wallsGroup = new GameObject("Walls");
            wallsGroup.transform.SetParent(dungeonRoot.transform);

            GameObject propsGroup = new GameObject("Props");
            propsGroup.transform.SetParent(dungeonRoot.transform);

            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (obstacleLayer == -1) obstacleLayer = 0;

            // Materials
            Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            floorMat.color = new Color(0.2f, 0.22f, 0.25f);

            Material wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            wallMat.color = new Color(0.38f, 0.40f, 0.44f);

            Material skeletonMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            skeletonMat.color = new Color(0.85f, 0.2f, 0.2f);

            Material bossMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bossMat.color = new Color(0.45f, 0.05f, 0.08f);

            Material keyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            keyMat.color = new Color(1f, 0.85f, 0.15f);
            keyMat.EnableKeyword("_EMISSION");
            keyMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.15f) * 0.8f);

            Material doorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            doorMat.color = new Color(0.35f, 0.22f, 0.12f);

            Material rubbleMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rubbleMat.color = new Color(0.25f, 0.24f, 0.26f);

            // ================= 2. WALKABLE TILES ================= //
            HashSet<Vector2Int> walkableFloors = new HashSet<Vector2Int>();

            // (1) Start Room (3x3): X in [-1, 1], Z in [-1, 1]
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    walkableFloors.Add(new Vector2Int(x, z));

            // (2) Main Corridor: (0, 2), (0, 3), (0, 4)
            walkableFloors.Add(new Vector2Int(0, 2));
            walkableFloors.Add(new Vector2Int(0, 3));
            walkableFloors.Add(new Vector2Int(0, 4));

            // (3) Dead End Branch (West): (-1, 4), (-2, 4), (-3, 4)
            walkableFloors.Add(new Vector2Int(-1, 4));
            walkableFloors.Add(new Vector2Int(-2, 4));
            walkableFloors.Add(new Vector2Int(-3, 4));

            // (4) Skeleton Room (North): (0, 5), (-1, 6), (0, 6), (1, 6), (0, 7)
            walkableFloors.Add(new Vector2Int(0, 5));
            walkableFloors.Add(new Vector2Int(-1, 6));
            walkableFloors.Add(new Vector2Int(0, 6)); // Skeleton spawn
            walkableFloors.Add(new Vector2Int(1, 6));
            walkableFloors.Add(new Vector2Int(0, 7));

            // (5) Key Room (East from 1, 6): (2, 6), (3, 6), (3, 5), (3, 7)
            walkableFloors.Add(new Vector2Int(2, 6));
            walkableFloors.Add(new Vector2Int(3, 6)); // Key location
            walkableFloors.Add(new Vector2Int(3, 5));
            walkableFloors.Add(new Vector2Int(3, 7));

            // (6) Door Cell: (0, 8)
            walkableFloors.Add(new Vector2Int(0, 8));

            // (7) Boss Room (4x4): X in [-1, 2], Z in [9, 12]
            for (int x = -1; x <= 2; x++)
                for (int z = 9; z <= 12; z++)
                    walkableFloors.Add(new Vector2Int(x, z));

            // Create Floor Cubes
            foreach (var coord in walkableFloors)
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = $"Floor_{coord.x}_{coord.y}";
                floor.transform.SetParent(floorsGroup.transform);
                floor.transform.position = new Vector3(coord.x * 2f, -0.1f, coord.y * 2f);
                floor.transform.localScale = new Vector3(2f, 0.2f, 2f);
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            }

            // ================= 3. SURROUNDING WALLS ================= //
            HashSet<Vector2Int> wallCells = new HashSet<Vector2Int>();
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
                new Vector2Int(-1, 0),                         new Vector2Int(1, 0),
                new Vector2Int(-1, 1),  new Vector2Int(0, 1),  new Vector2Int(1, 1)
            };

            foreach (var floor in walkableFloors)
            {
                foreach (var dir in directions)
                {
                    Vector2Int neighbor = floor + dir;
                    if (!walkableFloors.Contains(neighbor))
                    {
                        wallCells.Add(neighbor);
                    }
                }
            }

            foreach (var coord in wallCells)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall_{coord.x}_{coord.y}";
                wall.transform.SetParent(wallsGroup.transform);
                wall.transform.position = new Vector3(coord.x * 2f, 1f, coord.y * 2f);
                wall.transform.localScale = new Vector3(2f, 2f, 2f);
                wall.layer = obstacleLayer;
                wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            }

            // ================= 4. DEAD END RUBBLE DETAIL ================= //
            GameObject rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rubble.name = "DeadEnd_Rubble";
            rubble.transform.SetParent(propsGroup.transform);
            rubble.transform.position = new Vector3(-3f * 2f, 0.4f, 4f * 2f);
            rubble.transform.localScale = new Vector3(1.1f, 0.8f, 0.9f);
            rubble.transform.rotation = Quaternion.Euler(12f, 35f, -8f);
            rubble.GetComponent<Renderer>().sharedMaterial = rubbleMat;

            // ================= 5. SKELETON ENEMY ================= //
            GameObject oldSkel = GameObject.Find("Enemy_Skeleton");
            if (oldSkel != null) Undo.DestroyObjectImmediate(oldSkel);

            GameObject skeletonObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            skeletonObj.name = "Enemy_Skeleton";
            skeletonObj.transform.SetParent(propsGroup.transform);
            skeletonObj.transform.position = new Vector3(0f, 1f, 6f * 2f);
            skeletonObj.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            skeletonObj.GetComponent<Renderer>().sharedMaterial = skeletonMat;
            skeletonObj.GetComponent<Collider>().isTrigger = true;

            EncounterTrigger skelTrigger = skeletonObj.AddComponent<EncounterTrigger>();
            skelTrigger.Enemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/SampleSkeleton.asset");

            // ================= 6. DUNGEON KEY ================= //
            GameObject oldKey = GameObject.Find("Dungeon_Key");
            if (oldKey != null) Undo.DestroyObjectImmediate(oldKey);

            GameObject keyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keyObj.name = "Dungeon_Key";
            keyObj.transform.SetParent(propsGroup.transform);
            keyObj.transform.position = new Vector3(3f * 2f, 0.9f, 6f * 2f);
            keyObj.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            keyObj.GetComponent<Renderer>().sharedMaterial = keyMat;
            keyObj.AddComponent<KeyPickup>();

            // ================= 7. LOCKED DOOR ================= //
            GameObject oldDoor = GameObject.Find("Locked_Door");
            if (oldDoor != null) Undo.DestroyObjectImmediate(oldDoor);

            GameObject doorFrame = new GameObject("Locked_Door");
            doorFrame.transform.SetParent(propsGroup.transform);
            doorFrame.transform.position = new Vector3(0f, 0f, 8f * 2f);
            doorFrame.layer = obstacleLayer;

            GameObject doorPivot = new GameObject("DoorPivot");
            doorPivot.transform.SetParent(doorFrame.transform);
            doorPivot.transform.localPosition = new Vector3(-1f, 0f, 0f); // Left hinge

            GameObject doorSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorSlab.name = "DoorModel";
            doorSlab.transform.SetParent(doorPivot.transform);
            doorSlab.transform.localPosition = new Vector3(1f, 1f, 0f);
            doorSlab.transform.localScale = new Vector3(1.95f, 2f, 0.35f);
            doorSlab.GetComponent<Renderer>().sharedMaterial = doorMat;
            doorSlab.layer = obstacleLayer;

            BoxCollider doorCol = doorSlab.GetComponent<BoxCollider>();

            LockedDoor lockedDoorComp = doorFrame.AddComponent<LockedDoor>();
            SerializedObject doorSO = new SerializedObject(lockedDoorComp);
            doorSO.FindProperty("doorPivot").objectReferenceValue = doorPivot.transform;
            doorSO.FindProperty("barrierCollider").objectReferenceValue = doorCol;
            doorSO.ApplyModifiedProperties();

            // ================= 8. BOSS EXECUTIONER ================= //
            GameObject oldBoss = GameObject.Find("Boss_Executioner");
            if (oldBoss != null) Undo.DestroyObjectImmediate(oldBoss);

            GameObject bossObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bossObj.name = "Boss_Executioner";
            bossObj.transform.SetParent(propsGroup.transform);
            bossObj.transform.position = new Vector3(0.5f * 2f, 1.4f, 11f * 2f); // Center of 4x4 Boss Room
            bossObj.transform.localScale = new Vector3(2.2f, 1.5f, 2.2f);
            bossObj.GetComponent<Renderer>().sharedMaterial = bossMat;
            bossObj.GetComponent<Collider>().isTrigger = true;

            EncounterTrigger bossTrigger = bossObj.AddComponent<EncounterTrigger>();
            bossTrigger.Enemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/BossExecutioner.asset");

            // ================= 9. SETUP PLAYER ================= //
            GameObject playerGO = GameObject.Find("Player");
            if (playerGO != null)
            {
                playerGO.transform.position = new Vector3(0f, 1f, 0f);
                playerGO.transform.rotation = Quaternion.identity;

                if (playerGO.GetComponent<PlayerInteraction>() == null)
                {
                    playerGO.AddComponent<PlayerInteraction>();
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("[DungeonBuilder] Complete Dungeon built with 7 layout areas, Dungeon Key, Locked Door, Skeleton, and Boss Executioner!");
        }
    }
}
#endif
