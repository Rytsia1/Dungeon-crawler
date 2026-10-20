#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Data;
using DungeonCrawler.Interaction;
using DungeonCrawler.Player;
using DungeonCrawler.Effects;
using DungeonCrawler.Audio;

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

            GameObject torchesGroup = new GameObject("Torches");
            torchesGroup.transform.SetParent(dungeonRoot.transform);

            GameObject ceilingsGroup = new GameObject("Ceilings");
            ceilingsGroup.transform.SetParent(dungeonRoot.transform);

            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (obstacleLayer == -1) obstacleLayer = 0;

            // Materials
            Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            floorMat.color = new Color(0.2f, 0.22f, 0.25f);

            Material ceilingMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            ceilingMat.color = new Color(0.16f, 0.17f, 0.20f);

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

            Material torchWoodMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            torchWoodMat.color = new Color(0.25f, 0.15f, 0.08f);

            Material torchFlameMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            torchFlameMat.color = new Color(1f, 0.55f, 0.1f);
            torchFlameMat.EnableKeyword("_EMISSION");
            torchFlameMat.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.1f) * 3.2f);

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

            // Instantiate Floors & Ceilings
            foreach (var pos in walkableFloors)
            {
                // Floor slab
                GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
                f.name = $"Floor_{pos.x}_{pos.y}";
                f.transform.SetParent(floorsGroup.transform);
                f.transform.position = new Vector3(pos.x * 2f, -0.1f, pos.y * 2f);
                f.transform.localScale = new Vector3(2f, 0.2f, 2f);
                f.GetComponent<Renderer>().sharedMaterial = floorMat;

                // Enclosed Dungeon Ceiling slab
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = $"Ceiling_{pos.x}_{pos.y}";
                c.transform.SetParent(ceilingsGroup.transform);
                c.transform.position = new Vector3(pos.x * 2f, 2.1f, pos.y * 2f);
                c.transform.localScale = new Vector3(2f, 0.2f, 2f);
                c.GetComponent<Renderer>().sharedMaterial = ceilingMat;
            }

            // ================= 3. WALLS GENERATION ================= //
            HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var floor in walkableFloors)
            {
                foreach (var dir in directions)
                {
                    Vector2Int neighbor = floor + dir;
                    if (!walkableFloors.Contains(neighbor))
                    {
                        wallPositions.Add(neighbor);
                    }
                }
            }

            foreach (var wallPos in wallPositions)
            {
                GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.name = $"Wall_{wallPos.x}_{wallPos.y}";
                w.transform.SetParent(wallsGroup.transform);
                w.transform.position = new Vector3(wallPos.x * 2f, 1f, wallPos.y * 2f);
                w.transform.localScale = new Vector3(2f, 2.2f, 2f);
                w.layer = obstacleLayer;
                w.GetComponent<Renderer>().sharedMaterial = wallMat;
            }

            // ================= 4. DEAD END RUBBLE ================= //
            GameObject oldRubble = GameObject.Find("DeadEnd_Rubble");
            if (oldRubble != null) Undo.DestroyObjectImmediate(oldRubble);

            GameObject rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rubble.name = "DeadEnd_Rubble";
            rubble.transform.SetParent(propsGroup.transform);
            rubble.transform.position = new Vector3(-3f * 2f, 0.4f, 4f * 2f);
            rubble.transform.localScale = new Vector3(1.6f, 0.8f, 1.6f);
            rubble.GetComponent<Renderer>().sharedMaterial = rubbleMat;
            rubble.layer = obstacleLayer;

            // ================= 5. SKELETON ENCOUNTER ================= //
            GameObject oldSkel = GameObject.Find("Enemy_Skeleton");
            if (oldSkel != null) Undo.DestroyObjectImmediate(oldSkel);

            GameObject skelObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            skelObj.name = "Enemy_Skeleton";
            skelObj.transform.SetParent(propsGroup.transform);
            skelObj.transform.position = new Vector3(0f, 1f, 6f * 2f);
            skelObj.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            skelObj.GetComponent<Renderer>().sharedMaterial = skeletonMat;
            skelObj.GetComponent<Collider>().isTrigger = true;

            EncounterTrigger skelTrigger = skelObj.AddComponent<EncounterTrigger>();
            skelTrigger.Enemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/SampleSkeleton.asset");

            // ================= 6. DUNGEON KEY ================= //
            GameObject oldKey = GameObject.Find("Dungeon_Key");
            if (oldKey != null) Undo.DestroyObjectImmediate(oldKey);

            GameObject keyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            keyObj.name = "Dungeon_Key";
            keyObj.transform.SetParent(propsGroup.transform);
            keyObj.transform.position = new Vector3(3f * 2f, 0.8f, 6f * 2f);
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

            // ================= 9. TORCHES SETUP ================= //
            // Start Room Torches
            CreateTorch(torchesGroup.transform, new Vector3(0f, 1.4f, -2.85f), Quaternion.Euler(0f, 0f, 0f), torchWoodMat, torchFlameMat);
            CreateTorch(torchesGroup.transform, new Vector3(-2.85f, 1.4f, 0f), Quaternion.Euler(0f, 90f, 0f), torchWoodMat, torchFlameMat);
            CreateTorch(torchesGroup.transform, new Vector3(2.85f, 1.4f, 0f), Quaternion.Euler(0f, -90f, 0f), torchWoodMat, torchFlameMat);

            // Corridor & Junction Torches
            CreateTorch(torchesGroup.transform, new Vector3(0.85f, 1.4f, 6f), Quaternion.Euler(0f, -90f, 0f), torchWoodMat, torchFlameMat);
            CreateTorch(torchesGroup.transform, new Vector3(-1f, 1.4f, 8.85f), Quaternion.Euler(0f, 180f, 0f), torchWoodMat, torchFlameMat);

            // Skeleton Room Torches
            CreateTorch(torchesGroup.transform, new Vector3(-2.85f, 1.4f, 12f), Quaternion.Euler(0f, 90f, 0f), torchWoodMat, torchFlameMat);
            CreateTorch(torchesGroup.transform, new Vector3(0f, 1.4f, 14.85f), Quaternion.Euler(0f, 180f, 0f), torchWoodMat, torchFlameMat);

            // Key Room Torches
            CreateTorch(torchesGroup.transform, new Vector3(7.85f, 1.4f, 12f), Quaternion.Euler(0f, -90f, 0f), torchWoodMat, torchFlameMat);
            CreateTorch(torchesGroup.transform, new Vector3(6f, 1.4f, 14.85f), Quaternion.Euler(0f, 180f, 0f), torchWoodMat, torchFlameMat);

            // Flanking Locked Door Torches (Left & Right of Door at Z=16)
            CreateTorch(torchesGroup.transform, new Vector3(-1.6f, 1.4f, 15.85f), Quaternion.Euler(0f, 0f, 0f), torchWoodMat, torchFlameMat);
            CreateTorch(torchesGroup.transform, new Vector3(1.6f, 1.4f, 15.85f), Quaternion.Euler(0f, 0f, 0f), torchWoodMat, torchFlameMat);

            // Boss Room Arena Torches
            CreateTorch(torchesGroup.transform, new Vector3(-2.85f, 1.6f, 22f), Quaternion.Euler(0f, 90f, 0f), torchWoodMat, torchFlameMat);
            CreateTorch(torchesGroup.transform, new Vector3(4.85f, 1.6f, 22f), Quaternion.Euler(0f, -90f, 0f), torchWoodMat, torchFlameMat);
            CreateTorch(torchesGroup.transform, new Vector3(1f, 1.6f, 24.85f), Quaternion.Euler(0f, 180f, 0f), torchWoodMat, torchFlameMat);

            // ================= 10. ENVIRONMENT LIGHTING & FOG ================= //
            SetupEnvironmentLighting();

            // ================= 11. MANAGERS & PLAYER SETUP ================= //
            SetupManagersAndEffects();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("[DungeonBuilder] Complete Dungeon built with retro lighting, atmospheric fog, 13 strategic wall torches, audio procedural synthesizer, and camera juice!");
        }

        private static void CreateTorch(Transform parent, Vector3 position, Quaternion rotation, Material stickMat, Material flameMat)
        {
            GameObject torch = new GameObject("Torch");
            torch.transform.SetParent(parent);
            torch.transform.position = position;
            torch.transform.rotation = rotation;

            // Stick (brown wood cylinder)
            GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stick.name = "Stick";
            stick.transform.SetParent(torch.transform, false);
            Object.DestroyImmediate(stick.GetComponent<Collider>());
            stick.transform.localPosition = new Vector3(0f, -0.1f, 0.12f);
            stick.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            stick.transform.localScale = new Vector3(0.06f, 0.22f, 0.06f);
            stick.GetComponent<Renderer>().sharedMaterial = stickMat;

            // Flame (orange emissive cube)
            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flame.name = "Flame";
            flame.transform.SetParent(torch.transform, false);
            Object.DestroyImmediate(flame.GetComponent<Collider>());
            flame.transform.localPosition = new Vector3(0f, 0.12f, 0.2f);
            flame.transform.localScale = new Vector3(0.12f, 0.16f, 0.12f);
            flame.GetComponent<Renderer>().sharedMaterial = flameMat;

            // Point Light with FlickeringLight
            GameObject lightObj = new GameObject("TorchPointLight");
            lightObj.transform.SetParent(torch.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.15f, 0.24f);

            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.62f, 0.24f);
            pLight.range = 7.5f;
            pLight.intensity = 1.8f;
            pLight.shadows = LightShadows.None;

            lightObj.AddComponent<FlickeringLight>();
        }

        private static void SetupEnvironmentLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(10f / 255f, 10f / 255f, 15f / 255f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(5f / 255f, 5f / 255f, 10f / 255f);
            RenderSettings.fogDensity = 0.08f;

            GameObject dirLight = GameObject.Find("Directional Light");
            if (dirLight != null)
            {
                Light dl = dirLight.GetComponent<Light>();
                if (dl != null)
                {
                    dl.color = new Color(0.12f, 0.15f, 0.25f);
                    dl.intensity = 0.02f;
                }
            }
        }

        private static void SetupManagersAndEffects()
        {
            GameObject managersGO = GameObject.Find("_Managers");
            if (managersGO != null && managersGO.GetComponent<AudioManager>() == null)
            {
                managersGO.AddComponent<AudioManager>();
            }

            GameObject playerGO = GameObject.Find("Player");
            if (playerGO != null)
            {
                playerGO.transform.position = new Vector3(0f, 1f, 0f);
                playerGO.transform.rotation = Quaternion.identity;

                if (playerGO.GetComponent<PlayerInteraction>() == null)
                {
                    playerGO.AddComponent<PlayerInteraction>();
                }

                Camera cam = playerGO.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(5f / 255f, 5f / 255f, 10f / 255f);

                    if (cam.GetComponent<CameraJuice>() == null)
                    {
                        cam.gameObject.AddComponent<CameraJuice>();
                    }
                    if (cam.GetComponent<RetroSetup>() == null)
                    {
                        cam.gameObject.AddComponent<RetroSetup>();
                    }
                }
            }
        }
    }
}
#endif
