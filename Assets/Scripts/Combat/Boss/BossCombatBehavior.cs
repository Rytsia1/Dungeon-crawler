using System;
using System.Collections;
using UnityEngine;
using DungeonCrawler.Combat.QTE;
using DungeonCrawler.Effects;
using DungeonCrawler.Audio;

namespace DungeonCrawler.Combat.Boss
{
    public class BossCombatBehavior : MonoBehaviour
    {
        [Header("Boss Patterns Configuration")]
        [SerializeField] private int flurryHits = 3;
        [SerializeField] private float flurryDelay = 0.3f;
        [SerializeField] private float cleaveDuration = 1.6f;

        [Header("Delayed Cleave Timing Trick Curve")]
        [SerializeField] private AnimationCurve delayedCleaveCurve;

        private int patternTurnIndex = 0;

        private void Awake()
        {
            SetupCleaveCurve();
        }

        private void SetupCleaveCurve()
        {
            if (delayedCleaveCurve == null || delayedCleaveCurve.length == 0)
            {
                // Slow windup for 65% of the time, then sudden rapid swing
                delayedCleaveCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0.3f),
                    new Keyframe(0.65f, 0.25f, 0.3f, 2.5f),
                    new Keyframe(1.0f, 1.0f, 2.5f, 0f)
                );
            }
        }

        /// <summary>
        /// Executes the specialized Boss attack turn with alternating patterns.
        /// Returns total damage dealt to player and combat log string.
        /// </summary>
        public void ExecuteBossAttack(
            TimingBarQTE timingBar, 
            int basePower, 
            Action<int, string> onComplete)
        {
            if (timingBar == null)
            {
                onComplete?.Invoke(basePower, "Boss menyerang dengan tebasan biasa!");
                return;
            }

            int pattern = patternTurnIndex % 2;
            patternTurnIndex++;

            if (pattern == 0)
            {
                // Pattern 1: Executioner's Flurry (Multi-Hit QTE)
                StartCoroutine(ExecuteFlurryRoutine(timingBar, basePower, onComplete));
            }
            else
            {
                // Pattern 2: Heavy Delayed Cleave (Trick Timing Curve)
                ExecuteDelayedCleave(timingBar, basePower, onComplete);
            }
        }

        private IEnumerator ExecuteFlurryRoutine(
            TimingBarQTE timingBar, 
            int basePower, 
            Action<int, string> onComplete)
        {
            Debug.Log("[BossCombatBehavior] Boss executing 'Executioner's Flurry' (3-Hit Combo)!");

            AudioManager.Instance?.PlaySFX(SFXType.SwordSwing, 0.8f);

            int perHitDamage = Mathf.Max(1, Mathf.RoundToInt(basePower / (float)flurryHits));
            int totalDamageTaken = 0;
            int successfulParries = 0;

            bool isDone = false;

            timingBar.StartMultiQTE(
                flurryHits, 
                flurryDelay, 
                results =>
                {
                    for (int i = 0; i < results.Length; i++)
                    {
                        if (results[i])
                        {
                            successfulParries++;
                            AudioManager.Instance?.PlaySFX(SFXType.Parry, 0.9f);
                        }
                        else
                        {
                            totalDamageTaken += perHitDamage;
                            AudioManager.Instance?.PlaySFX(SFXType.Damage, 0.7f);
                        }
                    }
                    isDone = true;
                },
                KeyCode.Space,
                0.85f,
                "FLURRY DEFENSE! Tekan [SPASI]"
            );

            while (!isDone) yield return null;

            string log = successfulParries == flurryHits
                ? "<color=#00FF66>SEMPURNA!</color> Semua 3 ayunan Executioner's Flurry berhasil ditangkis (0 damage)!"
                : $"<color=#FF5555>FLURRY HITS!</color> Berhasil menahan {successfulParries}/{flurryHits} tebasan. Menerima {totalDamageTaken} damage!";

            if (totalDamageTaken > 0)
            {
                CameraJuice.Instance?.ShakeCamera(0.25f, 0.2f);
            }

            onComplete?.Invoke(totalDamageTaken, log);
        }

        private void ExecuteDelayedCleave(
            TimingBarQTE timingBar, 
            int basePower, 
            Action<int, string> onComplete)
        {
            Debug.Log("[BossCombatBehavior] Boss executing 'Heavy Delayed Cleave' with erratic timing trick!");

            SetupCleaveCurve();

            timingBar.StartQTE(
                KeyCode.Space,
                result =>
                {
                    int incomingDamage = 0;
                    string log = "";

                    switch (result)
                    {
                        case QTEResult.Perfect:
                            incomingDamage = 0;
                            log = "<color=#00FF66>LEGENDARY PARRY!</color> Menangkis Heavy Delayed Cleave Boss secara sempurna!";
                            CameraJuice.Instance?.HitStop(0.12f);
                            CameraJuice.Instance?.ShakeCamera(0.25f, 0.18f);
                            AudioManager.Instance?.PlaySFX(SFXType.Parry, 1f);
                            break;
                        case QTEResult.Good:
                            incomingDamage = Mathf.RoundToInt(basePower * 0.5f);
                            log = $"<color=#FFFF00>HEAVY GUARD!</color> Menahan tebasan kapak boss ({incomingDamage} damage).";
                            CameraJuice.Instance?.ShakeCamera(0.15f, 0.1f);
                            AudioManager.Instance?.PlaySFX(SFXType.Damage, 0.7f);
                            break;
                        case QTEResult.Miss:
                            incomingDamage = Mathf.RoundToInt(basePower * 1.5f);
                            log = $"<color=#FF0000>DEVASTATING CLEAVE!</color> Terkecoh timing ayunan kapak! ({incomingDamage} damage)!";
                            CameraJuice.Instance?.ShakeCamera(0.35f, 0.3f);
                            AudioManager.Instance?.PlaySFX(SFXType.Damage, 1f);
                            break;
                    }

                    onComplete?.Invoke(incomingDamage, log);
                },
                cleaveDuration,
                "HATI-HATI! HEAVY CLEAVE (Trik Kecepatan)!",
                1f,
                delayedCleaveCurve
            );
        }
    }
}
