using System;
using UnityEngine;

namespace DungeonCrawler.Core
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("State Settings")]
        [SerializeField] private GameState currentState = GameState.Exploring;

        public GameState CurrentState => currentState;

        public event Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ChangeState(GameState newState)
        {
            if (currentState == newState)
                return;

            GameState previousState = currentState;
            currentState = newState;
            Debug.Log($"[GameStateManager] State changed: {previousState} -> {newState}");

            OnGameStateChanged?.Invoke(currentState);
        }
    }
}
