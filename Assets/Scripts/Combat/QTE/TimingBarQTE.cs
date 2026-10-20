using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DungeonCrawler.Combat.QTE
{
    public class TimingBarQTE : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject widgetRoot;
        [SerializeField] private RectTransform barBackground;
        [SerializeField] private RectTransform cursor;
        [SerializeField] private RectTransform goodZone;
        [SerializeField] private RectTransform perfectZone;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text promptText;

        [Header("Zone Ratios (0.0 - 1.0)")]
        [SerializeField] private float goodZoneCenter = 0.5f;
        [SerializeField] private float goodZoneWidth = 0.30f;
        [SerializeField] private float perfectZoneWidth = 0.10f;

        [Header("Settings")]
        [SerializeField] private float defaultDuration = 1.2f;

        private bool isActive = false;
        private float currentPosition = 0f;
        private Action<QTEResult> completionCallback;
        private KeyCode currentTriggerKey = KeyCode.Space;

        public bool IsActive => isActive;

        private void Awake()
        {
            if (widgetRoot == null) widgetRoot = gameObject;
            widgetRoot.SetActive(false);
            SetupZoneVisuals();
        }

        public void SetupZoneVisuals()
        {
            if (barBackground == null) return;

            float barWidth = barBackground.rect.width > 0 ? barBackground.rect.width : 400f;

            if (goodZone != null)
            {
                goodZone.sizeDelta = new Vector2(barWidth * goodZoneWidth, goodZone.sizeDelta.y);
                float posX = (goodZoneCenter - 0.5f) * barWidth;
                goodZone.anchoredPosition = new Vector2(posX, goodZone.anchoredPosition.y);
            }

            if (perfectZone != null)
            {
                perfectZone.sizeDelta = new Vector2(barWidth * perfectZoneWidth, perfectZone.sizeDelta.y);
                float posX = (goodZoneCenter - 0.5f) * barWidth;
                perfectZone.anchoredPosition = new Vector2(posX, perfectZone.anchoredPosition.y);
            }
        }

        public void StartQTE(
            KeyCode triggerKey, 
            Action<QTEResult> onComplete, 
            float duration = 0f, 
            string prompt = "Tekan [SPASI] di Sweet Spot!",
            float cursorSpeed = 1f,
            AnimationCurve speedCurve = null)
        {
            gameObject.SetActive(true);
            if (widgetRoot != null) widgetRoot.SetActive(true);

            StopAllCoroutines();

            currentTriggerKey = triggerKey;
            completionCallback = onComplete;
            if (duration <= 0f) duration = defaultDuration;
            if (cursorSpeed <= 0f) cursorSpeed = 1f;

            float actualDuration = duration / cursorSpeed;

            if (promptText != null) promptText.text = prompt;
            if (resultText != null) resultText.text = "";

            SetupZoneVisuals();
            isActive = true;

            StartCoroutine(RunTimingRoutine(actualDuration, speedCurve));
        }

        private IEnumerator RunTimingRoutine(float duration, AnimationCurve speedCurve)
        {
            float elapsed = 0f;
            currentPosition = 0f;
            QTEResult result = QTEResult.Miss;
            bool inputRegistered = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedT = Mathf.Clamp01(elapsed / duration);

                // Support non-linear speed curves (e.g. erratic/delayed swing)
                currentPosition = speedCurve != null ? Mathf.Clamp01(speedCurve.Evaluate(normalizedT)) : normalizedT;
                UpdateCursorVisual(currentPosition);

                if (CheckInputTrigger())
                {
                    inputRegistered = true;
                    result = EvaluatePosition(currentPosition);
                    break;
                }

                yield return null;
            }

            if (!inputRegistered)
            {
                result = QTEResult.Miss;
            }

            yield return ShowResultAndFinish(result);
        }

        /// <summary>
        /// Starts a multi-hit QTE sequence (e.g. Boss Flurry attack).
        /// Calls onFinished with an array of booleans indicating success (Good/Perfect) per swing.
        /// </summary>
        public void StartMultiQTE(
            int hitCount, 
            float delayBetween, 
            Action<bool[]> onFinished,
            KeyCode triggerKey = KeyCode.Space,
            float singleDuration = 0.85f,
            string promptPrefix = "MULTI-PARRY! Tekan [SPASI]!")
        {
            gameObject.SetActive(true);
            if (widgetRoot != null) widgetRoot.SetActive(true);

            StopAllCoroutines();
            currentTriggerKey = triggerKey;
            isActive = true;

            StartCoroutine(RunMultiQTERoutine(hitCount, delayBetween, onFinished, singleDuration, promptPrefix));
        }

        private IEnumerator RunMultiQTERoutine(
            int hitCount, 
            float delayBetween, 
            Action<bool[]> onFinished, 
            float singleDuration, 
            string promptPrefix)
        {
            bool[] results = new bool[hitCount];
            SetupZoneVisuals();

            for (int i = 0; i < hitCount; i++)
            {
                if (promptText != null)
                {
                    promptText.text = $"{promptPrefix} ({i + 1}/{hitCount})";
                }
                if (resultText != null) resultText.text = "";

                float elapsed = 0f;
                currentPosition = 0f;
                QTEResult hitResult = QTEResult.Miss;
                bool hitRegistered = false;

                while (elapsed < singleDuration)
                {
                    elapsed += Time.deltaTime;
                    currentPosition = Mathf.Clamp01(elapsed / singleDuration);
                    UpdateCursorVisual(currentPosition);

                    if (CheckInputTrigger())
                    {
                        hitRegistered = true;
                        hitResult = EvaluatePosition(currentPosition);
                        break;
                    }
                    yield return null;
                }

                if (!hitRegistered) hitResult = QTEResult.Miss;

                results[i] = (hitResult == QTEResult.Good || hitResult == QTEResult.Perfect);

                if (resultText != null)
                {
                    resultText.text = results[i] 
                        ? (hitResult == QTEResult.Perfect ? "<color=#00FF66>PARRY!</color>" : "<color=#FFFF00>BLOCK!</color>")
                        : "<color=#FF3333>HIT!</color>";
                }

                yield return new WaitForSeconds(delayBetween);
            }

            isActive = false;
            widgetRoot.SetActive(false);
            onFinished?.Invoke(results);
        }

        private bool CheckInputTrigger()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(currentTriggerKey) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                return true;
#endif

            return false;
        }

        private QTEResult EvaluatePosition(float pos)
        {
            float halfGood = goodZoneWidth * 0.5f;
            float halfPerfect = perfectZoneWidth * 0.5f;

            float dist = Mathf.Abs(pos - goodZoneCenter);

            if (dist <= halfPerfect)
                return QTEResult.Perfect;
            if (dist <= halfGood)
                return QTEResult.Good;

            return QTEResult.Miss;
        }

        private void UpdateCursorVisual(float pos)
        {
            if (cursor == null || barBackground == null) return;
            float barWidth = barBackground.rect.width > 0 ? barBackground.rect.width : 400f;
            float startX = -barWidth * 0.5f;
            float endX = barWidth * 0.5f;
            cursor.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, pos), cursor.anchoredPosition.y);
        }

        private IEnumerator ShowResultAndFinish(QTEResult result)
        {
            if (resultText != null)
            {
                switch (result)
                {
                    case QTEResult.Perfect:
                        resultText.text = "<color=#00FF66>PERFECT!</color>";
                        break;
                    case QTEResult.Good:
                        resultText.text = "<color=#FFFF00>GOOD!</color>";
                        break;
                    case QTEResult.Miss:
                        resultText.text = "<color=#FF3333>MISS!</color>";
                        break;
                }
            }

            yield return new WaitForSeconds(0.4f);

            isActive = false;
            widgetRoot.SetActive(false);

            completionCallback?.Invoke(result);
        }
    }
}
