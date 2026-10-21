using UnityEngine;
using DungeonCrawler.Data;
using DungeonCrawler.UI;

namespace DungeonCrawler.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class KeyPickup : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private string keyName = "Kunci Dungeon";
        [SerializeField] private string pickupPrompt = "[E] Ambil Kunci";
        [SerializeField] private float bobbingSpeed = 2.5f;
        [SerializeField] private float bobbingHeight = 0.15f;
        [SerializeField] private float rotationSpeed = 60f;

        private bool isPickedUp = false;
        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            if (isPickedUp) return;

            // Idle floating animation
            float newY = startPos.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        public string GetPromptText()
        {
            return isPickedUp ? "" : pickupPrompt;
        }

        public void Interact()
        {
            if (isPickedUp) return;
            isPickedUp = true;

            Debug.Log($"[KeyPickup] Player picked up: {keyName}");

            // Play procedural retro key jingle SFX
            DungeonCrawler.Audio.AudioManager.Instance?.PlaySFX(DungeonCrawler.Audio.SFXType.KeyJingle);

            // Set player inventory
            PlayerStats stats = PlayerStats.Instance ?? FindFirstObjectByType<PlayerStats>();
            if (stats != null)
            {
                stats.HasDungeonKey = true;
            }

            // Update HUD
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.SetKeyStatus(true);
                HUDManager.Instance.SetObjective("Objektif: Buka Pintu Tersegel menuju Ruang Boss.");
                HUDManager.Instance.ShowNotification("Memperoleh KUNCI DUNGEON!", 2.5f);
                HUDManager.Instance.HideInteractionPrompt();
            }

            // Disable visuals and collider
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;

            Destroy(gameObject, 0.2f);
        }
    }
}
