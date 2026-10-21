using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.Effects
{
    [RequireComponent(typeof(Camera))]
    public class RetroSetup : MonoBehaviour
    {
        [Header("Retro Resolution Settings (PS1 / 32-Bit Style)")]
        [SerializeField] private bool enableRetroResolution = false;
        [SerializeField] private int targetWidth = 480;
        [SerializeField] private int targetHeight = 270;

        private Camera targetCamera;
        private Camera blitCamera;
        private GameObject blitCamGO;
        private RenderTexture retroTexture;
        private GameObject retroCanvasGO;
        private RawImage retroDisplayImage;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (enableRetroResolution)
            {
                SetupRetroDisplay();
            }
            else if (targetCamera != null && targetCamera.targetTexture != null)
            {
                // Ensure targetTexture is null so camera renders directly to Display 1
                targetCamera.targetTexture = null;
            }
        }

        public void SetupRetroDisplay()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();

            // Prevent duplicating resources if already initialized
            if (retroTexture != null && blitCamGO != null && retroCanvasGO != null)
            {
                return;
            }

            Cleanup();

            // Create low-resolution Point-filtered RenderTexture
            retroTexture = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                name = "RetroRenderTexture"
            };
            retroTexture.Create();

            targetCamera.targetTexture = retroTexture;

            // Dedicated screen blit camera targeting Display 1 so Unity never shows "No cameras rendering"
            blitCamGO = new GameObject("_RetroBlitCamera");
            blitCamGO.transform.SetParent(transform, false);
            blitCamera = blitCamGO.AddComponent<Camera>();
            blitCamera.clearFlags = CameraClearFlags.Nothing;
            blitCamera.cullingMask = 0; // Do not re-render 3D world
            blitCamera.depth = 100;
            blitCamera.targetDisplay = 0; // Target Display 1
            blitCamera.targetTexture = null; // Renders directly to Display 1!

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

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
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

            if (blitCamGO != null)
            {
                Destroy(blitCamGO);
                blitCamGO = null;
            }

            if (retroCanvasGO != null)
            {
                Destroy(retroCanvasGO);
                retroCanvasGO = null;
            }
        }
    }
}
