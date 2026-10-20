using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DungeonCrawler.Combat.QTE
{
    public class TypingQTE : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject widgetRoot;
        [SerializeField] private TMP_Text spellWordText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Slider timerBar;
        [SerializeField] private TMP_Text resultText;

        [Header("Feedback Colors")]
        [SerializeField] private Color typedColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private Color untypedColor = Color.white;
        [SerializeField] private Color nextCharColor = new Color(1f, 0.9f, 0.2f);

        private bool isActive = false;
        private string targetWord = "";
        private int currentTypedIndex = 0;
        private float timeRemaining = 0f;
        private float totalTime = 3.5f;
        private Action<bool> completionCallback;

        public bool IsActive => isActive;

        private void Awake()
        {
            if (widgetRoot == null) widgetRoot = gameObject;
            widgetRoot.SetActive(false);
        }

        public void StartTypingQTE(string word, float timeLimit, Action<bool> onComplete)
        {
            gameObject.SetActive(true);
            if (widgetRoot != null) widgetRoot.SetActive(true);

            StopAllCoroutines();

            targetWord = word.ToUpperInvariant();
            totalTime = timeLimit > 0f ? timeLimit : 3.5f;
            timeRemaining = totalTime;
            currentTypedIndex = 0;
            completionCallback = onComplete;

            if (resultText != null) resultText.text = "";
            if (promptText != null) promptText.text = "Ketik Mantra Sihir:";

            UpdateWordDisplay();
            isActive = true;

            StartCoroutine(TypingRoutine());
        }

        private IEnumerator TypingRoutine()
        {
            while (timeRemaining > 0f && currentTypedIndex < targetWord.Length)
            {
                timeRemaining -= Time.deltaTime;
                if (timerBar != null)
                {
                    timerBar.value = Mathf.Clamp01(timeRemaining / totalTime);
                }

                CheckKeyboardInput();

                yield return null;
            }

            bool success = currentTypedIndex >= targetWord.Length;

            if (resultText != null)
            {
                resultText.text = success 
                    ? "<color=#00FF88>MANTRA SUKSES!</color>" 
                    : "<color=#FF3333>MANTRA GAGAL!</color>";
            }

            yield return new WaitForSeconds(0.45f);

            isActive = false;
            widgetRoot.SetActive(false);

            completionCallback?.Invoke(success);
        }

        private void CheckKeyboardInput()
        {
            if (currentTypedIndex >= targetWord.Length) return;

            char expectedChar = targetWord[currentTypedIndex];
            bool hitExpected = false;
            bool hitWrong = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                Key expectedKey = CharToKey(expectedChar);
                if (expectedKey != Key.None && Keyboard.current[expectedKey].wasPressedThisFrame)
                {
                    hitExpected = true;
                }
                else
                {
                    hitWrong = true;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!hitExpected && !hitWrong && !string.IsNullOrEmpty(Input.inputString))
            {
                foreach (char c in Input.inputString)
                {
                    if (char.ToUpperInvariant(c) == expectedChar)
                    {
                        hitExpected = true;
                        break;
                    }
                    else if (char.IsLetter(c))
                    {
                        hitWrong = true;
                    }
                }
            }
#endif

            if (hitExpected)
            {
                currentTypedIndex++;
                UpdateWordDisplay();
            }
            else if (hitWrong)
            {
                timeRemaining = Mathf.Max(0f, timeRemaining - 0.3f);
            }
        }

        private void UpdateWordDisplay()
        {
            if (spellWordText == null) return;

            string typedHex = ColorUtility.ToHtmlStringRGB(typedColor);
            string nextHex = ColorUtility.ToHtmlStringRGB(nextCharColor);
            string untypedHex = ColorUtility.ToHtmlStringRGB(untypedColor);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < targetWord.Length; i++)
            {
                if (i < currentTypedIndex)
                {
                    sb.Append($"<color=#{typedHex}><b>{targetWord[i]}</b></color> ");
                }
                else if (i == currentTypedIndex)
                {
                    sb.Append($"<color=#{nextHex}><u><b>{targetWord[i]}</b></u></color> ");
                }
                else
                {
                    sb.Append($"<color=#{untypedHex}>{targetWord[i]}</color> ");
                }
            }

            spellWordText.text = sb.ToString();
        }

#if ENABLE_INPUT_SYSTEM
        private Key CharToKey(char c)
        {
            if (c >= 'A' && c <= 'Z')
            {
                return (Key)((int)Key.A + (c - 'A'));
            }
            if (c >= '0' && c <= '9')
            {
                return (Key)((int)Key.Digit0 + (c - '0'));
            }
            if (c == ' ') return Key.Space;
            return Key.None;
        }
#endif
    }
}
