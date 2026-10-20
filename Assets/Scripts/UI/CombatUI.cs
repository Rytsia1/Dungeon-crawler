using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonCrawler.Combat;
using DungeonCrawler.Combat.QTE;
using DungeonCrawler.Core;

namespace DungeonCrawler.UI
{
    public class CombatUI : MonoBehaviour
    {
        public static CombatUI Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject combatPanel;
        [SerializeField] private GameObject actionMenuPanel;

        [Header("Action Buttons")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button castButton;

        [Header("Status & Turn Info")]
        [SerializeField] private TMP_Text turnStatusText;
        [SerializeField] private TMP_Text combatLogText;

        [Header("Player HP Display")]
        [SerializeField] private Slider playerHealthSlider;
        [SerializeField] private TMP_Text playerHealthText;

        [Header("Enemy HP Display")]
        [SerializeField] private Slider enemyHealthSlider;
        [SerializeField] private TMP_Text enemyHealthText;
        [SerializeField] private TMP_Text enemyNameText;

        [Header("QTE Components")]
        [SerializeField] private TimingBarQTE timingBarQTE;
        [SerializeField] private TypingQTE typingQTE;

        public TimingBarQTE TimingBar
        {
            get => timingBarQTE;
            set => timingBarQTE = value;
        }

        public TypingQTE Typing
        {
            get => typingQTE;
            set => typingQTE = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (attackButton != null)
                attackButton.onClick.AddListener(OnAttackClicked);
            if (castButton != null)
                castButton.onClick.AddListener(OnCastClicked);

            ShowCombatUI(false);
        }

        private void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            ShowCombatUI(state == GameState.Combat);
        }

        public void ShowCombatUI(bool show)
        {
            if (combatPanel != null) combatPanel.SetActive(show);
            if (!show)
            {
                ShowActionMenu(false);
                if (timingBarQTE != null) timingBarQTE.gameObject.SetActive(false);
                if (typingQTE != null) typingQTE.gameObject.SetActive(false);
            }
        }

        public void ShowActionMenu(bool show)
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(show);
        }

        public void SetTurnStatus(string message)
        {
            if (turnStatusText != null) turnStatusText.text = message;
        }

        public void SetCombatLog(string message)
        {
            if (combatLogText != null) combatLogText.text = message;
        }

        public void UpdatePlayerHealth(int current, int max)
        {
            if (playerHealthSlider != null)
            {
                playerHealthSlider.maxValue = max;
                playerHealthSlider.value = current;
            }
            if (playerHealthText != null)
            {
                playerHealthText.text = $"HP: {current} / {max}";
            }
        }

        public void UpdateEnemyHealth(int current, int max, string enemyName = "")
        {
            if (enemyHealthSlider != null)
            {
                enemyHealthSlider.maxValue = max;
                enemyHealthSlider.value = current;
            }
            if (enemyHealthText != null)
            {
                enemyHealthText.text = $"{current} / {max}";
            }
            if (!string.IsNullOrEmpty(enemyName) && enemyNameText != null)
            {
                enemyNameText.text = enemyName;
            }
        }

        private void OnAttackClicked()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.ExecutePlayerAttack();
            }
        }

        private void OnCastClicked()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.ExecutePlayerCast();
            }
        }
    }
}
