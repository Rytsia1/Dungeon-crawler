using System;
using UnityEngine;

namespace DungeonCrawler.Data
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Attributes")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private int baseAttack = 20;

        [Header("Inventory")]
        [SerializeField] private bool hasDungeonKey = false;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public int BaseAttack => baseAttack;
        public bool IsAlive => currentHealth > 0;

        public bool HasDungeonKey
        {
            get => hasDungeonKey;
            set
            {
                hasDungeonKey = value;
                OnKeyStatusChanged?.Invoke(hasDungeonKey);
            }
        }

        public event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
        public event Action<bool> OnKeyStatusChanged;
        public event Action OnPlayerDied;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || !IsAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            Debug.Log($"[PlayerStats] Player took {damage} damage! Remaining HP: {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth == 0)
            {
                Debug.Log("[PlayerStats] Player has died!");
                OnPlayerDied?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Debug.Log($"[PlayerStats] Player healed {amount} HP! Current HP: {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ResetStats()
        {
            currentHealth = maxHealth;
            hasDungeonKey = false;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnKeyStatusChanged?.Invoke(hasDungeonKey);
        }
    }
}
