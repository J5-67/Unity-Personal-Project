using UnityEngine;
using System.Collections;
using Interaction;
namespace Enemy.Boss
{
    public class BossEncounterTrigger : MonoBehaviour
    {
        [SerializeField] private GameObject bossObject;
        [SerializeField] private Door entryDoor;
        [SerializeField] private BossHealthUI bossHealthUI;
        [Header("🎵 Audio Layers")]
        [SerializeField] private AudioClip[] phaseMusicLayers;
        [Header("Settings")]
        [SerializeField] private bool triggerOnce = true;
        private bool _isTriggered = false;
        private void Awake()
        {
            if (phaseMusicLayers != null && phaseMusicLayers.Length > 0 && Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.PreloadClips(phaseMusicLayers);
            }
            if (bossObject != null)
            {
                bossObject.SetActive(false);
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if (_isTriggered) return;
            if (other.CompareTag("Player"))
            {
                _isTriggered = true;
                StartCoroutine(BossSequenceRoutine());
            }
        }
        private IEnumerator BossSequenceRoutine()
        {
            if (entryDoor != null)
            {
                entryDoor.Close();
            }
            yield return new WaitForSeconds(1.0f);
            if (bossObject != null)
            {
                bossObject.SetActive(true);
            }
            if (bossHealthUI != null)
            {
                bossHealthUI.gameObject.SetActive(true);
            }
            if (phaseMusicLayers != null && phaseMusicLayers.Length > 0 && Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.StartPhaseMusic(phaseMusicLayers);
            }
        }
    }
}
