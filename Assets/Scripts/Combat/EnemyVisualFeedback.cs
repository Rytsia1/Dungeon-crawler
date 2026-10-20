using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Combat
{
    public class EnemyVisualFeedback : MonoBehaviour
    {
        [Header("Feedback Settings")]
        [SerializeField] private float flashDuration = 0.08f;
        [SerializeField] private float deathDuration = 0.5f;

        private MeshRenderer[] targetRenderers;
        private List<Color> originalColors = new List<Color>();
        private List<Color> originalEmissionColors = new List<Color>();
        private ParticleSystem hitParticles;
        private bool isDying = false;

        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorID = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            CacheRenderers();
            CreateHitParticleSystem();
        }

        private void CacheRenderers()
        {
            targetRenderers = GetComponentsInChildren<MeshRenderer>();
            originalColors.Clear();
            originalEmissionColors.Clear();

            foreach (var r in targetRenderers)
            {
                if (r == null || r.material == null) continue;

                Color c = Color.white;
                if (r.material.HasProperty(BaseColorID))
                    c = r.material.GetColor(BaseColorID);
                else if (r.material.HasProperty(ColorID))
                    c = r.material.GetColor(ColorID);

                originalColors.Add(c);

                Color em = Color.black;
                if (r.material.HasProperty(EmissionColorID))
                    em = r.material.GetColor(EmissionColorID);
                originalEmissionColors.Add(em);
            }
        }

        private void CreateHitParticleSystem()
        {
            GameObject fxGO = new GameObject("_HitParticles");
            fxGO.transform.SetParent(transform, false);
            fxGO.transform.localPosition = new Vector3(0, 1.0f, 0);

            hitParticles = fxGO.AddComponent<ParticleSystem>();

            var main = hitParticles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.3f;
            main.startLifetime = 0.25f;
            main.startSpeed = 4f;
            main.startSize = 0.12f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = hitParticles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

            var shape = hitParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var renderer = fxGO.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                // Assign default particle material
                Material pMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
                pMat.color = Color.yellow;
                renderer.material = pMat;
            }
        }

        /// <summary>
        /// Flashes enemy white for 0.08s and bursts hit sparks.
        /// </summary>
        public void PlayHitFeedback(bool isCritical)
        {
            if (isDying) return;

            StopCoroutine(nameof(HitFlashRoutine));
            StartCoroutine(HitFlashRoutine());

            if (hitParticles != null)
            {
                var main = hitParticles.main;
                main.startColor = isCritical 
                    ? new Color(1f, 0.85f, 0.1f, 1f)  // Gold/Yellow critical sparks
                    : new Color(0.9f, 0.2f, 0.2f, 1f); // Red sparks for normal hit
                hitParticles.Play();
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            if (targetRenderers == null || targetRenderers.Length == 0) yield break;

            // Apply solid bright white flash
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var r = targetRenderers[i];
                if (r == null || r.material == null) continue;

                if (r.material.HasProperty(BaseColorID)) r.material.SetColor(BaseColorID, Color.white);
                else if (r.material.HasProperty(ColorID)) r.material.SetColor(ColorID, Color.white);

                if (r.material.HasProperty(EmissionColorID)) r.material.SetColor(EmissionColorID, Color.white * 2f);
            }

            yield return new WaitForSeconds(flashDuration);

            // Restore original material colors
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var r = targetRenderers[i];
                if (r == null || r.material == null || i >= originalColors.Count) continue;

                if (r.material.HasProperty(BaseColorID)) r.material.SetColor(BaseColorID, originalColors[i]);
                else if (r.material.HasProperty(ColorID)) r.material.SetColor(ColorID, originalColors[i]);

                if (r.material.HasProperty(EmissionColorID)) r.material.SetColor(EmissionColorID, originalEmissionColors[i]);
            }
        }

        /// <summary>
        /// Tilts body and sinks into the floor over 0.5s, then disables GameObject.
        /// </summary>
        public void PlayDeathAnimation(Action onFinished = null)
        {
            if (isDying) return;
            isDying = true;

            StopAllCoroutines();
            StartCoroutine(DeathRoutine(onFinished));
        }

        private IEnumerator DeathRoutine(Action onFinished)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos - Vector3.up * 1.6f;

            Quaternion startRot = transform.rotation;
            Quaternion targetRot = startRot * Quaternion.Euler(25f, 0f, 35f);

            float elapsed = 0f;
            while (elapsed < deathDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / deathDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);

                yield return null;
            }

            onFinished?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
