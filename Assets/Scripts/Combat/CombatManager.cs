using System;
using UnityEngine;
using DungeonCrawler.Core;
using DungeonCrawler.Data;

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

        public CombatState CurrentCombatState => currentCombatState;
        public EnemyData ActiveEnemy => activeEnemy;
        public int ActiveEnemyCurrentHealth => activeEnemyCurrentHealth;

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

        public void StartCombat(EnemyData enemy)
        {
            if (enemy == null)
            {
                Debug.LogError("[CombatManager] Cannot start combat: EnemyData is null!");
                return;
            }

            activeEnemy = enemy;
            activeEnemyCurrentHealth = enemy.MaxHealth;
            Debug.Log($"[CombatManager] Combat started against {enemy.EnemyName} (HP: {enemy.MaxHealth}, ATK: {enemy.AttackPower})");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ChangeState(GameState.Combat);
            }

            OnCombatStarted?.Invoke(enemy);
            ChangeCombatState(CombatState.PlayerTurn_SelectAction);
        }

        public void EndCombat(bool playerWon)
        {
            Debug.Log($"[CombatManager] Combat ended. Player won: {playerWon}");
            ChangeCombatState(CombatState.None);

            OnCombatEnded?.Invoke(playerWon);

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ChangeState(playerWon ? GameState.Exploring : GameState.GameOver);
            }

            activeEnemy = null;
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

        // Basic turn progression placeholder
        public void AdvanceTurn()
        {
            switch (currentCombatState)
            {
                case CombatState.PlayerTurn_SelectAction:
                    ChangeCombatState(CombatState.PlayerTurn_ExecutingQTE);
                    break;
                case CombatState.PlayerTurn_ExecutingQTE:
                    ChangeCombatState(CombatState.TurnResolution);
                    break;
                case CombatState.EnemyTurn_Telegraph:
                    ChangeCombatState(CombatState.EnemyTurn_DefendQTE);
                    break;
                case CombatState.EnemyTurn_DefendQTE:
                    ChangeCombatState(CombatState.TurnResolution);
                    break;
                case CombatState.TurnResolution:
                    ChangeCombatState(CombatState.EnemyTurn_Telegraph);
                    break;
                default:
                    Debug.LogWarning($"[CombatManager] Cannot advance turn from state: {currentCombatState}");
                    break;
            }
        }
    }
}
