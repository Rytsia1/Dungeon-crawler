using System.Collections;
using UnityEngine;
using DungeonCrawler.Core;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DungeonCrawler.Player
{
    [RequireComponent(typeof(Collider))]
    public class PlayerGridController : MonoBehaviour
    {
        public static PlayerGridController Instance { get; private set; }

        [Header("Grid Movement Settings")]
        [SerializeField] private float stepSize = 2f;
        [SerializeField] private float moveDuration = 0.28f;
        [SerializeField] private float rotateDuration = 0.2f;

        [Header("Collision & Raycast")]
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float raycastHeightOffset = 0.5f;

        [Header("Wall Bump Effect")]
        [SerializeField] private bool enableWallBump = true;
        [SerializeField] private float bumpDistance = 0.25f;
        [SerializeField] private float bumpDuration = 0.1f;

        private bool isMoving = false;
        public bool IsMoving => isMoving;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (obstacleLayer.value == 0)
            {
                obstacleLayer = LayerMask.GetMask("Obstacle");
            }

            SnapToGrid();
        }

        private void Update()
        {
            if (isMoving) return;

            // Check GameState: strictly only allow movement when Exploring and no combat is running
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Exploring)
            {
                return;
            }
            if (Combat.CombatManager.Instance != null && Combat.CombatManager.Instance.CurrentCombatState != Combat.CombatState.None)
            {
                return;
            }

            HandleInput();
        }

        private void HandleInput()
        {
            bool moveForward = false;
            bool moveBackward = false;
            bool turnLeft = false;
            bool turnRight = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                moveForward = Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame;
                moveBackward = Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame;
                turnLeft = Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame;
                turnRight = Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!moveForward && !moveBackward && !turnLeft && !turnRight)
            {
                moveForward = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
                moveBackward = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
                turnLeft = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
                turnRight = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            }
#endif

            if (moveForward)
            {
                TryMove(transform.forward);
            }
            else if (moveBackward)
            {
                TryMove(-transform.forward);
            }
            else if (turnLeft)
            {
                StartCoroutine(RotateRoutine(-90f));
            }
            else if (turnRight)
            {
                StartCoroutine(RotateRoutine(90f));
            }
        }

        private void TryMove(Vector3 direction)
        {
            Vector3 origin = transform.position + Vector3.up * raycastHeightOffset;
            float checkDistance = stepSize;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, checkDistance, obstacleLayer))
            {
                Debug.Log($"[PlayerGridController] Obstacle detected ahead ({hit.collider.name}). Movement blocked.");
                if (enableWallBump)
                {
                    StartCoroutine(BumpRoutine(direction));
                }
                return;
            }

            Vector3 targetPosition = transform.position + direction * stepSize;
            StartCoroutine(MoveRoutine(targetPosition));
        }

        private IEnumerator MoveRoutine(Vector3 targetPosition)
        {
            isMoving = true;
            Vector3 startPosition = transform.position;
            float elapsed = 0f;

            // Audio & visual head-bobbing feedback
            DungeonCrawler.Audio.AudioManager.Instance?.PlaySFX(DungeonCrawler.Audio.SFXType.Footstep);
            DungeonCrawler.Effects.CameraJuice.Instance?.DoStepBob(moveDuration, 0.05f);

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
                yield return null;
            }

            transform.position = SnapPosition(targetPosition);
            isMoving = false;
        }

        private IEnumerator RotateRoutine(float angle)
        {
            isMoving = true;
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = startRotation * Quaternion.Euler(0f, angle, 0f);
            float elapsed = 0f;

            while (elapsed < rotateDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rotateDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
                yield return null;
            }

            Vector3 euler = targetRotation.eulerAngles;
            float snappedY = Mathf.Round(euler.y / 90f) * 90f;
            transform.rotation = Quaternion.Euler(0f, snappedY, 0f);
            isMoving = false;
        }

        /// <summary>
        /// Automatically rotates the player to face towards an enemy or point of interest.
        /// Snaps rotation to cardinal 90 degree direction to preserve DRPG grid alignment.
        /// </summary>
        public void RotateTowards(Vector3 targetWorldPosition, float duration = 0.25f)
        {
            Vector3 dir = targetWorldPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion lookRot = Quaternion.LookRotation(dir);
            float angle = lookRot.eulerAngles.y;
            float snappedY = Mathf.Round(angle / 90f) * 90f;
            Quaternion snappedRot = Quaternion.Euler(0f, snappedY, 0f);

            StopAllCoroutines();
            isMoving = false;
            StartCoroutine(RotateTowardsRoutine(snappedRot, duration));
        }

        private IEnumerator RotateTowardsRoutine(Quaternion targetRot, float duration)
        {
            isMoving = true;
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                yield return null;
            }

            transform.rotation = targetRot;
            isMoving = false;
        }

        private IEnumerator BumpRoutine(Vector3 direction)
        {
            isMoving = true;
            Vector3 originalPos = transform.position;
            Vector3 bumpTarget = originalPos + direction * bumpDistance;

            // Wall bump thud and camera shake
            DungeonCrawler.Audio.AudioManager.Instance?.PlaySFX(DungeonCrawler.Audio.SFXType.Damage, 0.6f);
            DungeonCrawler.Effects.CameraJuice.Instance?.ShakeCamera(bumpDuration * 1.5f, 0.08f);

            float halfDuration = bumpDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(originalPos, bumpTarget, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(bumpTarget, originalPos, elapsed / halfDuration);
                yield return null;
            }

            transform.position = originalPos;
            isMoving = false;
        }

        private void SnapToGrid()
        {
            transform.position = SnapPosition(transform.position);
            Vector3 euler = transform.rotation.eulerAngles;
            float snappedY = Mathf.Round(euler.y / 90f) * 90f;
            transform.rotation = Quaternion.Euler(0f, snappedY, 0f);
        }

        private Vector3 SnapPosition(Vector3 pos)
        {
            float snappedX = Mathf.Round(pos.x / stepSize) * stepSize;
            float snappedZ = Mathf.Round(pos.z / stepSize) * stepSize;
            return new Vector3(snappedX, pos.y, snappedZ);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 origin = transform.position + Vector3.up * raycastHeightOffset;
            Gizmos.DrawRay(origin, transform.forward * stepSize);
        }
    }
}
