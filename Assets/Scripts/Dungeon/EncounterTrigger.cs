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

            ResolveReferences();

            if (enemyData == null)
            {
                Debug.LogError($"[EncounterTrigger] Cannot trigger encounter on '{gameObject.name}': EnemyData is null!");
                return;
            }

            if (CombatManager.Instance == null)
            {
                Debug.LogError($"[EncounterTrigger] Cannot trigger encounter on '{gameObject.name}': CombatManager.Instance is null!");
                return;
            }

            // Start Combat with all references provided and verify success
            bool started = CombatManager.Instance.StartCombat(enemyData, enemyObject, visualFeedback, bossBehavior);
            if (!started)
            {
                Debug.LogWarning($"[EncounterTrigger] StartCombat returned false for '{gameObject.name}'. Trigger remains unconsumed.");
                return;
            }

            hasTriggered = true;
            Debug.Log($"[EncounterTrigger] Encounter successfully started on '{gameObject.name}'! (Enemy: {enemyData.EnemyName})");

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
