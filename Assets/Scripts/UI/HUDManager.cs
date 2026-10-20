using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DungeonCrawler.Core;
using DungeonCrawler.Data;

namespace DungeonCrawler.UI
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Exploration HUD")]
        [SerializeField] private GameObject explorationPanel;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text interactionPromptText;
        [SerializeField] private TMP_Text keyStatusText;
        [SerializeField] private TMP_Text notificationText;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartGameOverButton;

        [Header("Victory Panel")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button restartVictoryButton;

        private Coroutine notificationRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (restartGameOverButton != null)
                restartGameOverButton.onClick.AddListener(RestartScene);
            if (restartVictoryButton != null)
                restartVictoryButton.onClick.AddListener(RestartScene);

            if (interactionPromptText != null)
                interactionPromptText.gameObject.SetActive(false);
            if (notificationText != null)
                notificationText.gameObject.SetActive(false);
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            if (victoryPanel != null)
                victoryPanel.SetActive(false);

            SetObjective("Objektif: Cari Kunci Dungeon di Ruang Tersegel.");
            SetKeyStatus(false);
        }

        private void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                HandleGameStateChanged(GameStateManager.Instance.CurrentState);
            }

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnKeyStatusChanged += SetKeyStatus;
            }
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnKeyStatusChanged -= SetKeyStatus;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            bool isExploring = (state == GameState.Exploring);
            if (explorationPanel != null)
            {
                explorationPanel.SetActive(isExploring);
            }

            if (state == GameState.GameOver)
            {
                ShowGameOverPanel();
            }
            else if (state == GameState.Victory)
            {
                ShowVictoryPanel();
            }
        }

        public void ShowInteractionPrompt(string prompt)
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.text = prompt;
                interactionPromptText.gameObject.SetActive(true);
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
            }
        }

        public void SetObjective(string objective)
        {
            if (objectiveText != null)
            {
                objectiveText.text = objective;
            }
        }

        public void SetKeyStatus(bool hasKey)
        {
            if (keyStatusText != null)
            {
                keyStatusText.text = hasKey 
                    ? "<color=#FFE633>Kunci Dungeon: [DIMILIKI]</color>" 
                    : "<color=#888888>Kunci Dungeon: [BELUM DITEMUKAN]</color>";
            }
        }

        public void ShowNotification(string message, float duration = 2.5f)
        {
            if (notificationText == null) return;

            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
            }
            notificationRoutine = StartCoroutine(NotificationRoutine(message, duration));
        }

        private IEnumerator NotificationRoutine(string message, float duration)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);

            yield return new WaitForSeconds(duration);

            notificationText.gameObject.SetActive(false);
            notificationRoutine = null;
        }

        public void ShowGameOverPanel()
        {
            if (explorationPanel != null) explorationPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        public void ShowVictoryPanel()
        {
            if (explorationPanel != null) explorationPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(true);
        }

        public void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
