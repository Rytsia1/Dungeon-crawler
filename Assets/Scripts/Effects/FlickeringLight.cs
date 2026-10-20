using UnityEngine;

namespace DungeonCrawler.Effects
{
    [RequireComponent(typeof(Light))]
    public class FlickeringLight : MonoBehaviour
    {
        [Header("Intensity Settings")]
        [SerializeField] private float minIntensity = 1.2f;
        [SerializeField] private float maxIntensity = 2.4f;
        [SerializeField] private float flickerSpeed = 3.5f;

        [Header("Position Jitter (Flame Flicker)")]
        [SerializeField] private bool enableJitter = true;
        [SerializeField] private float jitterAmount = 0.04f;

        private Light targetLight;
        private Vector3 baseLocalPosition;
        private float noiseOffset;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            baseLocalPosition = transform.localPosition;
            noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (targetLight == null) return;

            float time = Time.time * flickerSpeed + noiseOffset;
            float noise = Mathf.PerlinNoise(time, time * 0.5f);
            targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            if (enableJitter)
            {
                float jX = (Mathf.PerlinNoise(time * 1.5f, 0f) - 0.5f) * jitterAmount * 2f;
                float jY = (Mathf.PerlinNoise(0f, time * 1.5f) - 0.5f) * jitterAmount * 2f;
                float jZ = (Mathf.PerlinNoise(time, time) - 0.5f) * jitterAmount * 2f;
                transform.localPosition = baseLocalPosition + new Vector3(jX, jY, jZ);
            }
        }
    }
}
