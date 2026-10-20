using System.Collections;
using UnityEngine;

namespace DungeonCrawler.Effects
{
    public class CameraJuice : MonoBehaviour
    {
        public static CameraJuice Instance { get; private set; }

        [Header("Default Parameters")]
        [SerializeField] private float defaultStepBobAmount = 0.06f;

        private Vector3 initialLocalPosition;
        private Vector3 shakeOffset = Vector3.zero;
        private Vector3 bobOffset = Vector3.zero;

        private Coroutine shakeCoroutine;
        private Coroutine hitStopCoroutine;
        private Coroutine bobCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            initialLocalPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            transform.localPosition = initialLocalPosition + shakeOffset + bobOffset;
        }

        /// <summary>
        /// Menggetarkan kamera secara cepat untuk memberi dampak benturan / serangan.
        /// Menggunakan unscaled delta time agar tetap bergetar saat hit-stop aktif.
        /// </summary>
        public void ShakeCamera(float duration = 0.2f, float magnitude = 0.12f)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / duration);
                float curMag = magnitude * damp;

                shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * curMag,
                    Random.Range(-1f, 1f) * curMag,
                    Random.Range(-0.5f, 0.5f) * (curMag * 0.5f)
                );

                yield return null;
            }

            shakeOffset = Vector3.zero;
            shakeCoroutine = null;
        }

        /// <summary>
        /// Menghentikan timeScale sesaat (~0.05-0.08 detik) untuk memberi sensasi benturan berat (hit-stop).
        /// </summary>
        public void HitStop(float duration = 0.07f)
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }
            hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = originalTimeScale > 0f ? originalTimeScale : 1f;
            hitStopCoroutine = null;
        }

        /// <summary>
        /// Efek head-bobbing halus saat pemain melangkah di PlayerGridController.
        /// </summary>
        public void DoStepBob(float duration = 0.28f, float bobAmount = 0.05f)
        {
            if (bobAmount <= 0f) bobAmount = defaultStepBobAmount;

            if (bobCoroutine != null)
            {
                StopCoroutine(bobCoroutine);
            }
            bobCoroutine = StartCoroutine(BobRoutine(duration, bobAmount));
        }

        private IEnumerator BobRoutine(float duration, float bobAmount)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Dip down and return smoothly with a sine curve
                float y = -Mathf.Sin(t * Mathf.PI) * bobAmount;
                bobOffset = new Vector3(0f, y, 0f);

                yield return null;
            }

            bobOffset = Vector3.zero;
            bobCoroutine = null;
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            shakeOffset = Vector3.zero;
            bobOffset = Vector3.zero;
        }
    }
}
