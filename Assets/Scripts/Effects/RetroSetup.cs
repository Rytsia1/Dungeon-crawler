using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.Effects
{
    [RequireComponent(typeof(Camera))]
    public class RetroSetup : MonoBehaviour
    {
        [Header("Retro Resolution Settings (PS1 / 32-Bit Style)")]
        [SerializeField] private bool enableRetroResolution = true;
        [SerializeField] private int targetWidth = 480;
        [SerializeField] private int targetHeight = 270;

        private Camera targetCamera;
        private RenderTexture retroTexture;
        private GameObject retroCanvasGO;
        private RawImage retroDisplayImage;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            if (enableRetroResolution)
            {
                SetupRetroDisplay();
            }
        }

        public void SetupRetroDisplay()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();

            // Create low-resolution Point-filtered RenderTexture
            retroTexture = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                name = "RetroRenderTexture"
            };
            retroTexture.Create();

            targetCamera.targetTexture = retroTexture;

            // Create ScreenSpaceOverlay Canvas to display the chunky pixels behind the HUD
            retroCanvasGO = new GameObject("_RetroDisplayCanvas");
            Canvas canvas = retroCanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100; // Render behind UI HUD and Combat Canvas

            CanvasScaler scaler = retroCanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(targetWidth, targetHeight);

            // RawImage stretched to full viewport
            GameObject imageGO = new GameObject("RetroBlitImage");
            imageGO.transform.SetParent(retroCanvasGO.transform, false);

            retroDisplayImage = imageGO.AddComponent<RawImage>();
            retroDisplayImage.texture = retroTexture;
            retroDisplayImage.raycastTarget = false;

            RectTransform rect = retroDisplayImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Debug.Log($"[RetroSetup] Initialized PS1 chunky rendering: {targetWidth}x{targetHeight} (Point Filtering).");
        }

        private void OnDestroy()
        {
            if (targetCamera != null && targetCamera.targetTexture == retroTexture)
            {
                targetCamera.targetTexture = null;
            }

            if (retroTexture != null)
            {
                retroTexture.Release();
                Destroy(retroTexture);
                retroTexture = null;
            }

            if (retroCanvasGO != null)
            {
                Destroy(retroCanvasGO);
            }
        }
    }
}
