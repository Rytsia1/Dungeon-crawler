using UnityEngine;
using DungeonCrawler.Core;
using DungeonCrawler.Combat;
using DungeonCrawler.Data;

namespace DungeonCrawler.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public class EncounterTrigger : MonoBehaviour
    {
        [Header("Encounter Configuration")]
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private bool disableGameObjectOnTrigger = false;

        private bool hasTriggered = false;

        public EnemyData Enemy
        {
            get => enemyData;
            set => enemyData = value;
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

            Debug.Log($"[EncounterTrigger] Player encountered enemy on '{gameObject.name}'!");

            if (enemyData == null)
            {
                Debug.LogWarning("[EncounterTrigger] EnemyData is null on this trigger!");
            }

            // Start Combat
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.StartCombat(enemyData);
            }
            else if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ChangeState(GameState.Combat);
            }

            // Disable collider so it doesn't re-trigger
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
