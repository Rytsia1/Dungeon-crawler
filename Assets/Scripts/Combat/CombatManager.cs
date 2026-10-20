using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DungeonCrawler.Core;
using DungeonCrawler.Data;
using DungeonCrawler.Combat.QTE;
using DungeonCrawler.UI;
using DungeonCrawler.Audio;
using DungeonCrawler.Effects;
using DungeonCrawler.Combat.Boss;

namespace DungeonCrawler.Combat
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private CombatState currentCombatState = CombatState.None;

        [Header("Active Combat")]
        [SerializeField] private EnemyData activeEnemy;
        [SerializeField] private int activeEnemyCurrentHealth;
        [SerializeField] private GameObject activeEnemyObject;
        private EnemyVisualFeedback activeVisualFeedback;
        private BossCombatBehavior activeBossBehavior;

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private CombatUI combatUI;

        [Header("Spell Words Pool")]
        [SerializeField] private List<string> spellWords = new List<string> { "IGNIS", "MORTIS", "FULGUR", "LUX", "SANCTUS" };

        public CombatState CurrentCombatState => currentCombatState;
        public EnemyData ActiveEnemy => activeEnemy;
        public int ActiveEnemyCurrentHealth => activeEnemyCurrentHealth;
        public GameObject ActiveEnemyObject => activeEnemyObject;

        public event Action<CombatState> OnCombatStateChanged;
        public event Action<EnemyData> OnCombatStarted;
        public event Action<bool> OnCombatEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            if (playerStats == null)
            {
                playerStats = PlayerStats.Instance ?? FindFirstObjectByType<PlayerStats>();
            }
            if (combatUI == null)
            {
                combatUI = CombatUI.Instance ?? FindFirstObjectByType<CombatUI>();
            }
        }

        public void StartCombat(EnemyData enemy, GameObject enemyGameObject = null)
        {
            if (enemy == null)
            {
                Debug.LogError("[CombatManager] Cannot start combat: EnemyData is null!");
                return;
            }

            FindReferences();

            activeEnemy = enemy;
            activeEnemyObject = enemyGameObject;
            activeEnemyCurrentHealth = enemy.MaxHealth;

            // Fetch visual feedback and boss behavior if available
            if (activeEnemyObject != null)
            {
                activeVisualFeedback = activeEnemyObject.GetComponent<EnemyVisualFeedback>();
                activeBossBehavior = activeEnemyObject.GetComponent<BossCombatBehavior>();
            }
            else
            {
                activeVisualFeedback = null;
                activeBossBehavior = null;
            }

            Debug.Log($"[CombatManager] Combat started against {enemy.EnemyName} (HP: {enemy.MaxHealth}, ATK: {enemy.AttackPower}) | BossBehavior: {activeBossBehavior != null}");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ChangeState(GameState.Combat);
            }

            if (combatUI != null)
            {
                combatUI.ShowCombatUI(true);
                combatUI.UpdateEnemyHealth(activeEnemyCurrentHealth, enemy.MaxHealth, enemy.EnemyName);
                if (playerStats != null)
                {
                    combatUI.UpdatePlayerHealth(playerStats.CurrentHealth, playerStats.MaxHealth);
                }
                combatUI.SetCombatLog($"Pertarungan dimulai melawan {enemy.EnemyName}!");
            }

            OnCombatStarted?.Invoke(enemy);
            StartPlayerTurn();
        }

        public void EndCombat(bool playerWon)
        {
            Debug.Log($"[CombatManager] Combat ended. Player won: {playerWon}");
            ChangeCombatState(CombatState.None);

            if (playerWon && activeVisualFeedback != null)
            {
                activeVisualFeedback.PlayDeathAnimation();
            }

            if (combatUI != null)
            {
                combatUI.SetTurnStatus(playerWon ? "<color=#00FF66>VICTORY!</color>" : "<color=#FF3333>DEFEAT...</color>");
                combatUI.ShowActionMenu(false);
            }

            StartCoroutine(FinishCombatRoutine(playerWon));
        }

        private IEnumerator FinishCombatRoutine(bool playerWon)
        {
            yield return new WaitForSeconds(1.5f);

            if (combatUI != null)
            {
                combatUI.ShowCombatUI(false);
            }

            OnCombatEnded?.Invoke(playerWon);

            if (GameStateManager.Instance != null)
            {
                if (!playerWon)
                {
                    GameStateManager.Instance.ChangeState(GameState.GameOver);
                }
                else
                {
                    bool isBossFight = (activeEnemy != null && (activeEnemy.IsBoss || activeEnemy.EnemyName.Contains("Executioner")));
                    GameStateManager.Instance.ChangeState(isBossFight ? GameState.Victory : GameState.Exploring);
                }
            }

            activeEnemy = null;
            activeEnemyObject = null;
            activeVisualFeedback = null;
            activeBossBehavior = null;
            activeEnemyCurrentHealth = 0;
        }

        public void ChangeCombatState(CombatState nextState)
        {
            if (currentCombatState == nextState)
                return;

            CombatState previousState = currentCombatState;
            currentCombatState = nextState;
            Debug.Log($"[CombatManager] CombatState: {previousState} -> {nextState}");

            OnCombatStateChanged?.Invoke(currentCombatState);
        }

        // ================= PLAYER TURN ================= //

        public void StartPlayerTurn()
        {
            if (activeEnemyCurrentHealth <= 0) return;

            ChangeCombatState(CombatState.PlayerTurn_SelectAction);

            if (combatUI != null)
            {
                combatUI.SetTurnStatus("Giliranmu! Pilih aksi:");
                combatUI.ShowActionMenu(true);
            }
        }

        public void ExecutePlayerAttack()
        {
            if (currentCombatState != CombatState.PlayerTurn_SelectAction) return;

            ChangeCombatState(CombatState.PlayerTurn_ExecutingQTE);

            if (combatUI != null)
            {
                combatUI.ShowActionMenu(false);
                combatUI.SetTurnStatus("SERANGAN FISIK!");

                if (combatUI.TimingBar != null)
                {
                    combatUI.TimingBar.StartQTE(KeyCode.Space, OnPlayerAttackQTEComplete, 1.2f, "Tekan [SPASI] di Sweet Spot!");
                }
                else
                {
                    // Fallback if UI timing bar missing
                    OnPlayerAttackQTEComplete(QTEResult.Good);
                }
            }
        }

        private void OnPlayerAttackQTEComplete(QTEResult result)
        {
            ChangeCombatState(CombatState.TurnResolution);

            int baseAttack = playerStats != null ? playerStats.BaseAttack : 15;
            int finalDamage = 0;
            string logResult = "";

            switch (result)
            {
                case QTEResult.Perfect:
                    finalDamage = Mathf.RoundToInt(baseAttack * 1.5f);
                    logResult = $"<color=#00FF66>CRITICAL HIT! (Perfect)</color> - {finalDamage} damage!";
                    CameraJuice.Instance?.HitStop(0.08f);
                    CameraJuice.Instance?.ShakeCamera(0.2f, 0.15f);
                    AudioManager.Instance?.PlaySFX(SFXType.SwordSwing, 0.9f);
                    AudioManager.Instance?.PlaySFX(SFXType.Parry, 1f);
                    break;
                case QTEResult.Good:
                    finalDamage = baseAttack;
                    logResult = $"<color=#FFFF00>Serangan Kena! (Good)</color> - {finalDamage} damage!";
                    CameraJuice.Instance?.ShakeCamera(0.12f, 0.08f);
                    AudioManager.Instance?.PlaySFX(SFXType.SwordSwing, 0.8f);
                    break;
                case QTEResult.Miss:
                    finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseAttack * 0.3f));
                    logResult = $"<color=#FF5555>Serangan Tergores (Miss)</color> - {finalDamage} damage!";
                    AudioManager.Instance?.PlaySFX(SFXType.Miss, 0.7f);
                    break;
            }

            ApplyDamageToEnemy(finalDamage, logResult);
        }

        public void ExecutePlayerCast()
        {
            if (currentCombatState != CombatState.PlayerTurn_SelectAction) return;

            ChangeCombatState(CombatState.PlayerTurn_ExecutingQTE);

            string word = "IGNIS";
            if (spellWords != null && spellWords.Count > 0)
            {
                word = spellWords[UnityEngine.Random.Range(0, spellWords.Count)];
            }

            if (combatUI != null)
            {
                combatUI.ShowActionMenu(false);
                combatUI.SetTurnStatus($"RAPALKAN MANTRA: {word}!");

                if (combatUI.Typing != null)
                {
                    combatUI.Typing.StartTypingQTE(word, 3.5f, OnPlayerCastQTEComplete);
                }
                else
                {
                    OnPlayerCastQTEComplete(true);
                }
            }
        }

        private void OnPlayerCastQTEComplete(bool success)
        {
            ChangeCombatState(CombatState.TurnResolution);

            int baseAttack = playerStats != null ? playerStats.BaseAttack : 15;
            int finalDamage = 0;
            string logResult = "";

            if (success)
            {
                finalDamage = Mathf.RoundToInt(baseAttack * 2.5f);
                logResult = $"<color=#00FFFF>MANTRA SUKSES! Ledakan Sihir</color> menimbulkan {finalDamage} damage!";
                CameraJuice.Instance?.HitStop(0.06f);
                CameraJuice.Instance?.ShakeCamera(0.25f, 0.18f);
                AudioManager.Instance?.PlaySFX(SFXType.Parry, 1f);
            }
            else
            {
                finalDamage = 0;
                logResult = "<color=#FF3333>Mantra gagal dirapalkan!</color> 0 damage.";
                AudioManager.Instance?.PlaySFX(SFXType.Miss, 0.7f);
            }

            ApplyDamageToEnemy(finalDamage, logResult);
        }

        private void ApplyDamageToEnemy(int damage, string logMessage)
        {
            activeEnemyCurrentHealth = Mathf.Max(0, activeEnemyCurrentHealth - damage);

            // Enemy hit flash & particles visual feedback
            if (activeVisualFeedback != null && damage > 0)
            {
                bool isCrit = logMessage.Contains("CRITICAL") || logMessage.Contains("MANTRA SUKSES");
                activeVisualFeedback.PlayHitFeedback(isCrit);
            }

            if (combatUI != null)
            {
                combatUI.UpdateEnemyHealth(activeEnemyCurrentHealth, activeEnemy.MaxHealth, activeEnemy.EnemyName);
                combatUI.SetCombatLog(logMessage);
            }

            Debug.Log($"[CombatManager] Enemy took {damage} dmg. Remaining HP: {activeEnemyCurrentHealth}/{activeEnemy.MaxHealth}");

            if (activeEnemyCurrentHealth <= 0)
            {
                EndCombat(true);
            }
            else
            {
                StartCoroutine(TransitionToEnemyTurn());
            }
        }

        // ================= ENEMY TURN ================= //

        private IEnumerator TransitionToEnemyTurn()
        {
            yield return new WaitForSeconds(1.0f);

            ChangeCombatState(CombatState.EnemyTurn_Telegraph);

            if (combatUI != null)
            {
                combatUI.SetTurnStatus($"<color=#FF5555>PERINGATAN!</color> {activeEnemy.EnemyName} bersiap menyerang!");
            }

            // Telegraph windup delay (~0.8s)
            yield return new WaitForSeconds(0.8f);

            ChangeCombatState(CombatState.EnemyTurn_DefendQTE);

            if (activeBossBehavior != null)
            {
                // Specialized Boss Attack Pattern (Multi-hit Flurry or Delayed Cleave)
                if (combatUI != null) combatUI.SetTurnStatus("<color=#FF0000>SERANGAN BOSS!</color>");
                activeBossBehavior.ExecuteBossAttack(combatUI != null ? combatUI.TimingBar : null, activeEnemy.AttackPower, OnEnemyAttackResolved);
            }
            else
            {
                if (combatUI != null)
                {
                    combatUI.SetTurnStatus("BERTAHAN! Tekan [SPASI] di Sweet Spot!");

                    if (combatUI.TimingBar != null)
                    {
                        combatUI.TimingBar.StartQTE(KeyCode.Space, OnEnemyAttackDefended, 1.1f, "PARRY / BLOCK! Tekan [SPASI]!");
                    }
                    else
                    {
                        OnEnemyAttackDefended(QTEResult.Miss);
                    }
                }
            }
        }

        private void OnEnemyAttackDefended(QTEResult defenseResult)
        {
            int enemyPower = activeEnemy != null ? activeEnemy.AttackPower : 10;
            int incomingDamage = 0;
            string defenseLog = "";

            switch (defenseResult)
            {
                case QTEResult.Perfect:
                    incomingDamage = 0;
                    defenseLog = "<color=#00FF66>PERFECT PARRY!</color> Serangan musuh ditangkis sempurna (0 damage)!";
                    CameraJuice.Instance?.HitStop(0.07f);
                    CameraJuice.Instance?.ShakeCamera(0.16f, 0.12f);
                    AudioManager.Instance?.PlaySFX(SFXType.Parry, 1f);
                    break;
                case QTEResult.Good:
                    incomingDamage = Mathf.RoundToInt(enemyPower * 0.5f);
                    defenseLog = $"<color=#FFFF00>GUARD BLOCKED!</color> Menahan sebagian serangan ({incomingDamage} damage).";
                    CameraJuice.Instance?.ShakeCamera(0.12f, 0.08f);
                    AudioManager.Instance?.PlaySFX(SFXType.Damage, 0.65f);
                    break;
                case QTEResult.Miss:
                    incomingDamage = enemyPower;
                    defenseLog = $"<color=#FF3333>DEFENSE FAILED!</color> Terkena serangan penuh ({incomingDamage} damage)!";
                    CameraJuice.Instance?.ShakeCamera(0.3f, 0.25f);
                    AudioManager.Instance?.PlaySFX(SFXType.Damage, 1f);
                    break;
            }

            OnEnemyAttackResolved(incomingDamage, defenseLog);
        }

        private void OnEnemyAttackResolved(int incomingDamage, string defenseLog)
        {
            ChangeCombatState(CombatState.TurnResolution);

            if (playerStats != null)
            {
                playerStats.TakeDamage(incomingDamage);
                if (combatUI != null)
                {
                    combatUI.UpdatePlayerHealth(playerStats.CurrentHealth, playerStats.MaxHealth);
                }
            }

            if (combatUI != null)
            {
                combatUI.SetCombatLog(defenseLog);
            }

            if (incomingDamage > 0 && combatUI != null)
            {
                combatUI.TriggerDamageFlash(0.25f);
            }

            Debug.Log($"[CombatManager] Player turn resolved. Received {incomingDamage} dmg.");

            if (playerStats != null && playerStats.CurrentHealth <= 0)
            {
                EndCombat(false);
            }
            else
            {
                StartCoroutine(ReturnToPlayerTurn());
            }
        }

        private IEnumerator ReturnToPlayerTurn()
        {
            yield return new WaitForSeconds(1.2f);
            StartPlayerTurn();
        }
    }
}
