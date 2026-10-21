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

        public bool StartCombat(
            EnemyData enemy, 
            GameObject enemyGameObject = null, 
            EnemyVisualFeedback visualFeedback = null, 
            BossCombatBehavior bossBehavior = null)
        {
            FindReferences();

            if (enemy == null)
            {
                Debug.LogError("[CombatManager] Cannot start combat: EnemyData is null!");
                return false;
            }

            if (enemy.MaxHealth <= 0)
            {
                Debug.LogError($"[CombatManager] Cannot start combat: Enemy '{enemy.EnemyName}' has invalid MaxHealth ({enemy.MaxHealth})!");
                return false;
            }

            if (combatUI == null)
            {
                Debug.LogError("[CombatManager] Cannot start combat: CombatUI is null in the scene!");
                return false;
            }

            activeEnemy = enemy;
            activeEnemyObject = enemyGameObject;
            activeEnemyCurrentHealth = enemy.MaxHealth;

            // Fetch visual feedback and boss behavior if supplied or lookup
            activeVisualFeedback = visualFeedback;
            activeBossBehavior = bossBehavior;

            if (activeEnemyObject != null)
            {
                if (activeVisualFeedback == null)
                    activeVisualFeedback = activeEnemyObject.GetComponentInChildren<EnemyVisualFeedback>();
                if (activeBossBehavior == null)
                    activeBossBehavior = activeEnemyObject.GetComponentInChildren<BossCombatBehavior>();

                // Automatically rotate player/camera to face straight at the engaged enemy
                if (Player.PlayerGridController.Instance != null)
                {
                    Player.PlayerGridController.Instance.RotateTowards(activeEnemyObject.transform.position, 0.25f);
                }
                else
                {
                    GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                    if (playerGO != null)
                    {
                        Vector3 dir = activeEnemyObject.transform.position - playerGO.transform.position;
                        dir.y = 0;
                        if (dir.sqrMagnitude > 0.001f)
                        {
                            playerGO.transform.rotation = Quaternion.LookRotation(dir);
                        }
                    }
                }
            }

            Debug.Log($"[CombatManager] Combat started against {enemy.EnemyName} (HP: {enemy.MaxHealth}, ATK: {enemy.AttackPower}) | VisualFeedback: {activeVisualFeedback != null} | BossBehavior: {activeBossBehavior != null}");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ChangeState(GameState.Combat);
            }

            combatUI.ShowCombatUI(true);
            combatUI.UpdateEnemyHealth(activeEnemyCurrentHealth, enemy.MaxHealth, enemy.EnemyName);
            if (playerStats != null)
            {
                combatUI.UpdatePlayerHealth(playerStats.CurrentHealth, playerStats.MaxHealth);
            }
            combatUI.SetCombatLog($"Pertarungan dimulai melawan {enemy.EnemyName}!");

            OnCombatStarted?.Invoke(enemy);
            StartPlayerTurn();
            return true;
        }

        public void EndCombat(bool playerWon)
        {
            Debug.Log($"[CombatManager] Combat ended. Player won: {playerWon}");
            ChangeCombatState(CombatState.None);

            if (playerWon)
            {
                if (activeVisualFeedback != null)
                {
                    activeVisualFeedback.PlayDeathAnimation();
                }
                else if (activeEnemyObject != null)
                {
                    activeEnemyObject.SetActive(false);
                }

                // Ensure all colliders on activeEnemyObject are disabled so corridor is open
                if (activeEnemyObject != null)
                {
                    foreach (var col in activeEnemyObject.GetComponentsInChildren<Collider>())
                    {
                        col.enabled = false;
                    }
                }
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
            else
            {
                Debug.LogWarning("[CombatManager] CombatUI is null in ExecutePlayerAttack. Auto-completing attack with fallback.");
                OnPlayerAttackQTEComplete(QTEResult.Good);
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
            else
            {
                Debug.LogWarning("[CombatManager] CombatUI is null in ExecutePlayerCast. Auto-completing cast with fallback.");
                OnPlayerCastQTEComplete(true);
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
                bool isCrit = logMessage.Contains("CRITICAL") || logMessage.Contains("MANTRA SUKSES") || logMessage.Contains("Perfect") || logMessage.Contains("PERFECT");
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

            if (activeBossBehavior != null)
            {
                if (combatUI != null)
                {
                    combatUI.SetTurnStatus("<color=#FF0000>PERINGATAN! BOSS BERSIAP MENYERANG!</color>");
                }

                // Telegraph windup delay (~0.8s)
                yield return new WaitForSeconds(0.8f);

                ChangeCombatState(CombatState.EnemyTurn_DefendQTE);
                if (combatUI != null) combatUI.SetTurnStatus("<color=#FF0000>SERANGAN BOSS!</color>");
                activeBossBehavior.ExecuteBossAttack(combatUI != null ? combatUI.TimingBar : null, activeEnemy != null ? activeEnemy.AttackPower : 15, OnEnemyAttackResolved);
            }
            else
            {
                // Select attack telegraph from enemy data if available
                EnemyAttackTelegraph telegraph = null;
                if (activeEnemy != null && activeEnemy.AttackTelegraphs != null && activeEnemy.AttackTelegraphs.Count > 0)
                {
                    int randIdx = UnityEngine.Random.Range(0, activeEnemy.AttackTelegraphs.Count);
                    telegraph = activeEnemy.AttackTelegraphs[randIdx];
                }

                string attackName = telegraph != null && !string.IsNullOrEmpty(telegraph.attackName)
                    ? telegraph.attackName
                    : "Serangan";
                float telegraphDuration = telegraph != null ? Mathf.Max(0.2f, telegraph.telegraphDuration) : 0.8f;
                float qteWindow = telegraph != null ? Mathf.Max(0.3f, telegraph.qteWindowDuration) : 1.1f;
                float multiplier = telegraph != null ? telegraph.damageMultiplier : 1.0f;
                int basePower = activeEnemy != null ? activeEnemy.AttackPower : 10;
                int effectivePower = Mathf.Max(1, Mathf.RoundToInt(basePower * multiplier));

                if (combatUI != null)
                {
                    combatUI.SetTurnStatus($"<color=#FF5555>PERINGATAN!</color> {activeEnemy?.EnemyName ?? "Musuh"} bersiap melancarkan [{attackName}]!");
                }

                yield return new WaitForSeconds(telegraphDuration);

                ChangeCombatState(CombatState.EnemyTurn_DefendQTE);

                if (combatUI != null && combatUI.TimingBar != null)
                {
                    combatUI.SetTurnStatus($"BERTAHAN DARI [{attackName}]! Tekan [SPASI] di Sweet Spot!");
                    combatUI.TimingBar.StartQTE(KeyCode.Space, (res) => OnEnemyAttackDefended(res, effectivePower, attackName), qteWindow, $"PARRY / BLOCK [{attackName}]! Tekan [SPASI]!");
                }
                else
                {
                    if (combatUI != null)
                    {
                        combatUI.SetTurnStatus($"BERTAHAN DARI [{attackName}]! Menahan serangan...");
                    }
                    else
                    {
                        Debug.LogWarning("[CombatManager] CombatUI is null in TransitionToEnemyTurn. Falling back to default defense resolution.");
                    }
                    OnEnemyAttackDefended(QTEResult.Miss, effectivePower, attackName);
                }
            }
        }

        private void OnEnemyAttackDefended(QTEResult defenseResult, int enemyPower, string attackName = "")
        {
            int incomingDamage = 0;
            string prefix = !string.IsNullOrEmpty(attackName) ? $"[{attackName}] " : "";
            string defenseLog = "";

            switch (defenseResult)
            {
                case QTEResult.Perfect:
                    incomingDamage = 0;
                    defenseLog = $"<color=#00FF66>PERFECT PARRY!</color> {prefix}Serangan musuh ditangkis sempurna (0 damage)!";
                    CameraJuice.Instance?.HitStop(0.07f);
                    CameraJuice.Instance?.ShakeCamera(0.16f, 0.12f);
                    AudioManager.Instance?.PlaySFX(SFXType.Parry, 1f);
                    break;
                case QTEResult.Good:
                    incomingDamage = Mathf.RoundToInt(enemyPower * 0.5f);
                    defenseLog = $"<color=#FFFF00>GUARD BLOCKED!</color> {prefix}Menahan sebagian serangan ({incomingDamage} damage).";
                    CameraJuice.Instance?.ShakeCamera(0.12f, 0.08f);
                    AudioManager.Instance?.PlaySFX(SFXType.Damage, 0.65f);
                    break;
                case QTEResult.Miss:
                default:
                    incomingDamage = enemyPower;
                    defenseLog = $"<color=#FF3333>DEFENSE FAILED!</color> {prefix}Terkena serangan penuh ({incomingDamage} damage)!";
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
