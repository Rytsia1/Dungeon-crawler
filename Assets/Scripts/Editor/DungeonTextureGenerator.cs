#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DungeonCrawler.Editor
{
    public static class DungeonTextureGenerator
    {
        private const string TexturesDir = "Assets/Textures";
        private const string MaterialsDir = "Assets/Materials";

        [MenuItem("DungeonCrawler/Generate Retro Textures & Materials")]
        public static void GenerateAll()
        {
            EnsureDirectories();

            Texture2D wallTex = GenerateWallTexture(64, 64);
            Texture2D floorTex = GenerateFloorTexture(64, 64);
            Texture2D ceilingTex = GenerateCeilingTexture(64, 64);

            SaveAndConfigureTexture(wallTex, $"{TexturesDir}/Tex_DungeonWall.png");
            SaveAndConfigureTexture(floorTex, $"{TexturesDir}/Tex_DungeonFloor.png");
            SaveAndConfigureTexture(ceilingTex, $"{TexturesDir}/Tex_DungeonCeiling.png");

            AssetDatabase.Refresh();

            Texture2D reimportedWall = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Tex_DungeonWall.png");
            Texture2D reimportedFloor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Tex_DungeonFloor.png");
            Texture2D reimportedCeiling = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/Tex_DungeonCeiling.png");

            CreateOrUpdateMaterial("Mat_DungeonWall", reimportedWall, new Color(0.38f, 0.40f, 0.44f), 0.05f);
            CreateOrUpdateMaterial("Mat_DungeonFloor", reimportedFloor, new Color(0.32f, 0.30f, 0.28f), 0.08f);
            CreateOrUpdateMaterial("Mat_DungeonCeiling", reimportedCeiling, new Color(0.18f, 0.18f, 0.20f), 0.02f);

            // Torch Materials
            CreateColorMaterial("Mat_TorchWood", new Color(0.24f, 0.14f, 0.07f), 0.1f);
            CreateEmissiveMaterial("Mat_TorchFlame", new Color(1.0f, 0.58f, 0.16f), 3.5f);

            // Enemy Materials
            CreateColorMaterial("Mat_EnemyBody", new Color(0.10f, 0.10f, 0.12f), 0.05f);
            CreateEmissiveMaterial("Mat_EnemyEyes", new Color(1.0f, 0.05f, 0.05f), 4.5f);

            // Key & Locked Door Materials
            CreateEmissiveMaterial("Mat_DungeonKey", new Color(1.0f, 0.82f, 0.12f), 2.8f);
            CreateColorMaterial("Mat_LockedDoor", new Color(0.28f, 0.16f, 0.10f), 0.15f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DungeonTextureGenerator] Successfully generated PS1-style 64x64 Point textures and Materials!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(TexturesDir))
            {
                Directory.CreateDirectory(TexturesDir);
            }
            if (!Directory.Exists(MaterialsDir))
            {
                Directory.CreateDirectory(MaterialsDir);
            }
        }

        /// <summary>
        /// Generates retro stone brick pattern with mortar lines, per-brick shade variations, and chisel noise.
        /// </summary>
        private static Texture2D GenerateWallTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color mortarColor = new Color(0.12f, 0.13f, 0.16f); // dark grout
            int rows = 4;
            int rowHeight = height / rows;

            for (int y = 0; y < height; y++)
            {
                int rowIndex = y / rowHeight;
                int yInRow = y % rowHeight;
                bool isHorizontalMortar = (yInRow == 0 || yInRow == rowHeight - 1);

                int xOffset = (rowIndex % 2 == 0) ? 0 : (width / 4);

                for (int x = 0; x < width; x++)
                {
                    int shiftedX = (x + xOffset) % width;
                    int brickWidth = width / 2;
                    int xInBrick = shiftedX % brickWidth;
                    bool isVerticalMortar = (xInBrick == 0 || xInBrick == brickWidth - 1);

                    if (isHorizontalMortar || isVerticalMortar)
                    {
                        float mortarNoise = Random.Range(-0.02f, 0.02f);
                        tex.SetPixel(x, y, mortarColor + new Color(mortarNoise, mortarNoise, mortarNoise));
                    }
                    else
                    {
                        // Stone brick base with subtle blue-gray stone palette
                        int brickId = (shiftedX / brickWidth) + rowIndex * 2;
                        float brickTone = (brickId % 3 == 0) ? 0.35f : (brickId % 3 == 1 ? 0.40f : 0.45f);

                        // Carved stone edge highlights/shadows
                        float edgeShade = 0f;
                        if (yInRow == 1) edgeShade += 0.08f; // top highlight
                        if (yInRow == rowHeight - 2) edgeShade -= 0.10f; // bottom shadow
                        if (xInBrick == 1) edgeShade += 0.05f; // left highlight
                        if (xInBrick == brickWidth - 2) edgeShade -= 0.07f; // right shadow

                        // Dither noise
                        float noise = Random.Range(-0.06f, 0.06f);
                        float finalVal = Mathf.Clamp01(brickTone + edgeShade + noise);

                        Color stoneColor = new Color(finalVal * 0.92f, finalVal * 0.95f, finalVal * 1.05f);
                        tex.SetPixel(x, y, stoneColor);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates retro flagstone/paving stones with dark mortar borders and surface grain.
        /// </summary>
        private static Texture2D GenerateFloorTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color mortarColor = new Color(0.10f, 0.09f, 0.08f);
            int tilesAcross = 4;
            int tileSize = width / tilesAcross;

            for (int y = 0; y < height; y++)
            {
                int yTile = y / tileSize;
                int yInTile = y % tileSize;
                bool isYMortar = (yInTile == 0);

                for (int x = 0; x < width; x++)
                {
                    int xTile = x / tileSize;
                    int xInTile = x % tileSize;
                    bool isXMortar = (xInTile == 0);

                    if (isXMortar || isYMortar)
                    {
                        tex.SetPixel(x, y, mortarColor);
                    }
                    else
                    {
                        int tileId = xTile + yTile * tilesAcross;
                        float baseLuma = ((tileId * 17) % 5 == 0) ? 0.28f : (((tileId * 13) % 3 == 0) ? 0.36f : 0.32f);

                        // Subtle warm cobblestone tint
                        float grainNoise = Random.Range(-0.05f, 0.05f);
                        float bevel = 0f;
                        if (xInTile == 1 || yInTile == 1) bevel = 0.06f; // slight bevel top-left
                        if (xInTile == tileSize - 1 || yInTile == tileSize - 1) bevel = -0.07f; // bevel bottom-right

                        float finalLuma = Mathf.Clamp01(baseLuma + grainNoise + bevel);
                        Color paverColor = new Color(finalLuma * 1.02f, finalLuma * 0.96f, finalLuma * 0.88f);
                        tex.SetPixel(x, y, paverColor);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates dark vaulted stone/wood ceiling with heavy cross-beams and shadow recesses.
        /// </summary>
        private static Texture2D GenerateCeilingTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            int beamSpacing = width / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int xInBeam = x % beamSpacing;
                    bool isBeamEdge = (xInBeam == 0 || xInBeam == 1 || xInBeam == beamSpacing - 1);

                    float baseVal = isBeamEdge ? 0.09f : 0.20f;
                    float woodGrain = Random.Range(-0.04f, 0.04f);

                    float finalVal = Mathf.Clamp01(baseVal + woodGrain);
                    Color col = new Color(finalVal * 0.9f, finalVal * 0.9f, finalVal * 0.95f);
                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            return tex;
        }

        private static void SaveAndConfigureTexture(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = false; // Authentic PS1 crisp point filter
                importer.SaveAndReimport();
            }
        }

        private static void CreateOrUpdateMaterial(string matName, Texture2D texture, Color baseColor, float smoothness)
        {
            string matPath = $"{MaterialsDir}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit") 
                         ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);

            if (texture != null)
            {
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            }

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            EditorUtility.SetDirty(mat);
        }

        private static void CreateColorMaterial(string matName, Color color, float smoothness)
        {
            string matPath = $"{MaterialsDir}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            EditorUtility.SetDirty(mat);
        }

        private static void CreateEmissiveMaterial(string matName, Color emissiveColor, float intensity)
        {
            string matPath = $"{MaterialsDir}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            Color finalColor = emissiveColor * intensity;
            mat.color = finalColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", finalColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", finalColor);

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", finalColor);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", finalColor);

            EditorUtility.SetDirty(mat);
        }
    }
}
#endif
