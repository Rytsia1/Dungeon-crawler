using UnityEngine;
using DungeonCrawler.Core;
using DungeonCrawler.Combat;
using DungeonCrawler.Combat.Boss;
using DungeonCrawler.Data;

namespace DungeonCrawler.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public class EncounterTrigger : MonoBehaviour
    {
        [Header("Encounter Configuration")]
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private bool disableGameObjectOnTrigger = false;

        [Header("Enemy 3D References (Auto-resolved if null)")]
        [SerializeField] private GameObject enemyObject;
        [SerializeField] private EnemyVisualFeedback visualFeedback;
        [SerializeField] private BossCombatBehavior bossBehavior;

        private bool hasTriggered = false;

        public EnemyData Enemy
        {
            get => enemyData;
            set => enemyData = value;
        }

        public GameObject EnemyObject
        {
            get => enemyObject;
            set => enemyObject = value;
        }

        public EnemyVisualFeedback VisualFeedback
        {
            get => visualFeedback;
            set => visualFeedback = value;
        }

        public BossCombatBehavior BossBehavior
        {
            get => bossBehavior;
            set => bossBehavior = value;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (enemyObject == null)
            {
                enemyObject = gameObject;
            }
            if (visualFeedback == null && enemyObject != null)
            {
                visualFeedback = enemyObject.GetComponentInChildren<EnemyVisualFeedback>();
            }
            if (bossBehavior == null && enemyObject != null)
            {
                bossBehavior = enemyObject.GetComponentInChildren<BossCombatBehavior>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            // Check if player triggered this
            if (other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerGridController>() != null)
            {
                TriggerEncounter();
            }
        }

        public void TriggerEncounter()
        {
            if (hasTriggered) return;
            hasTriggered = true;

            ResolveReferences();

            Debug.Log($"[EncounterTrigger] Player encountered enemy on '{gameObject.name}'! (Enemy: {(enemyData != null ? enemyData.EnemyName : "null")})");

            if (enemyData == null)
            {
                Debug.LogWarning("[EncounterTrigger] EnemyData is null on this trigger!");
            }

            // Start Combat with all references provided
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.StartCombat(enemyData, enemyObject, visualFeedback, bossBehavior);
            }
            else if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ChangeState(GameState.Combat);
            }

            // Disable trigger collider so it doesn't re-trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            if (disableGameObjectOnTrigger)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
