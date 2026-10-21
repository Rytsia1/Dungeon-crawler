using UnityEngine;
using DungeonCrawler.Core;
using DungeonCrawler.Interaction;
using DungeonCrawler.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DungeonCrawler.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float interactDistance = 3.0f;
        [SerializeField] private LayerMask interactLayer = ~0; // Everything by default
        [SerializeField] private Camera playerCamera;

        private IInteractable currentInteractable = null;

        private void Awake()
        {
            ResolveCamera();
        }

        private void ResolveCamera()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    playerCamera = Camera.main;
                }
            }
        }

        private void Update()
        {
            // Only allow interaction while strictly Exploring and not in combat
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Exploring)
            {
                if (currentInteractable != null)
                {
                    ClearCurrentInteractable();
                }
                return;
            }
            if (Combat.CombatManager.Instance != null && Combat.CombatManager.Instance.CurrentCombatState != Combat.CombatState.None)
            {
                if (currentInteractable != null)
                {
                    ClearCurrentInteractable();
                }
                return;
            }

            CheckForInteractable();
            HandleInput();
        }

        private void CheckForInteractable()
        {
            if (playerCamera == null)
            {
                ResolveCamera();
                if (playerCamera == null) return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            bool hitFound = false;
            RaycastHit hit;

            // 1. First try direct raycast
            if (Physics.Raycast(ray, out hit, interactDistance, interactLayer, QueryTriggerInteraction.Collide))
            {
                hitFound = true;
            }
            // 2. Fallback to SphereCast with radius 0.45m for responsive pickup without pixel-hunting
            else if (Physics.SphereCast(ray, 0.45f, out hit, interactDistance, interactLayer, QueryTriggerInteraction.Collide))
            {
                hitFound = true;
            }

            if (hitFound)
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    currentInteractable = interactable;
                    if (HUDManager.Instance != null)
                    {
                        HUDManager.Instance.ShowInteractionPrompt(interactable.GetPromptText());
                    }
                    return;
                }
            }

            if (currentInteractable != null)
            {
                ClearCurrentInteractable();
            }
        }

        private void ClearCurrentInteractable()
        {
            currentInteractable = null;
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.HideInteractionPrompt();
            }
        }

        private void HandleInput()
        {
            if (currentInteractable == null) return;

            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!interactPressed && Input.GetKeyDown(KeyCode.E))
            {
                interactPressed = true;
            }
#endif

            if (interactPressed)
            {
                Debug.Log($"[PlayerInteraction] Interacting with: {currentInteractable}");
                currentInteractable.Interact();
            }
        }
    }
}
