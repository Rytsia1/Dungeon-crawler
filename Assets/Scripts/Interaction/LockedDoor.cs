using System.Collections;
using UnityEngine;
using DungeonCrawler.Data;
using DungeonCrawler.UI;

namespace DungeonCrawler.Interaction
{
    public class LockedDoor : MonoBehaviour, IInteractable
    {
        [Header("Door Configuration")]
        [SerializeField] private bool isLocked = true;
        [SerializeField] private Transform doorPivot;
        [SerializeField] private Collider barrierCollider;
        [SerializeField] private float openAngle = -90f;
        [SerializeField] private float openDuration = 0.8f;

        private bool isOpening = false;

        public bool IsLocked => isLocked;

        private void Awake()
        {
            if (doorPivot == null) doorPivot = transform;
            if (barrierCollider == null) barrierCollider = GetComponent<Collider>();
        }

        public string GetPromptText()
        {
            if (!isLocked) return "";
            return "[E] Buka Pintu Tersegel";
        }

        public void Interact()
        {
            if (!isLocked || isOpening) return;

            PlayerStats stats = PlayerStats.Instance ?? FindObjectOfType<PlayerStats>();
            bool hasKey = stats != null && stats.HasDungeonKey;

            if (!hasKey)
            {
                Debug.Log("[LockedDoor] Door is locked! Player does not have key.");
                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.ShowNotification("<color=#FF5555>Pintu terkunci rapat... Butuh Kunci Dungeon.</color>", 2.5f);
                }
                return;
            }

            // Unlock and open
            isLocked = false;
            Debug.Log("[LockedDoor] Unlocking door with Dungeon Key!");
            DungeonCrawler.Audio.AudioManager.Instance?.PlaySFX(DungeonCrawler.Audio.SFXType.DoorCreak);

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowNotification("<color=#00FF66>Pintu Tersegel Terbuka!</color>", 2.5f);
                HUDManager.Instance.SetObjective("Objektif: Sesuatu menunggu di baliknya... Kalahkan Boss!");
                HUDManager.Instance.HideInteractionPrompt();
            }

            StartCoroutine(OpenDoorRoutine());
        }

        private IEnumerator OpenDoorRoutine()
        {
            isOpening = true;

            // Remove obstacle collider so player grid movement is not blocked
            if (barrierCollider != null)
            {
                barrierCollider.enabled = false;
            }
            gameObject.layer = 0; // Set to Default layer
            foreach (Transform child in transform)
            {
                child.gameObject.layer = 0;
            }

            Quaternion startRot = doorPivot.localRotation;
            Quaternion targetRot = startRot * Quaternion.Euler(0f, openAngle, 0f);
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                doorPivot.localRotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                yield return null;
            }

            doorPivot.localRotation = targetRot;
            isOpening = false;
        }
    }
}
