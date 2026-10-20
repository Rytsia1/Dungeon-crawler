using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Data
{
    [Serializable]
    public class EnemyAttackTelegraph
    {
        [Tooltip("Name of the enemy attack")]
        public string attackName = "Basic Attack";

        [Tooltip("Wind-up telegraph warning duration before player must react (in seconds)")]
        public float telegraphDuration = 1.0f;

        [Tooltip("Defend/Parry QTE window duration (in seconds)")]
        public float qteWindowDuration = 0.5f;

        [Tooltip("Damage multiplier for this attack")]
        public float damageMultiplier = 1.0f;
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Dungeon Crawler/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField] private string enemyName = "Skeleton";
        [SerializeField] private int maxHealth = 50;
        [SerializeField] private int attackPower = 10;
        [SerializeField] private bool isBoss = false;

        [Header("Telegraph & QTE Timings")]
        [SerializeField] private List<EnemyAttackTelegraph> attackTelegraphs = new List<EnemyAttackTelegraph>();

        public string EnemyName
        {
            get => enemyName;
            set => enemyName = value;
        }

        public int MaxHealth
        {
            get => maxHealth;
            set => maxHealth = value;
        }

        public int AttackPower
        {
            get => attackPower;
            set => attackPower = value;
        }

        public bool IsBoss
        {
            get => isBoss;
            set => isBoss = value;
        }

        public List<EnemyAttackTelegraph> AttackTelegraphs => attackTelegraphs;
    }
}
