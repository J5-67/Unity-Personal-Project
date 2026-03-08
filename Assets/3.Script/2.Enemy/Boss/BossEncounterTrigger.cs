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
        
        [Header("🎵 Audio")]
        [SerializeField] private AudioClip bossBGM;

        [Header("Settings")]
        [SerializeField] private bool triggerOnce = true;
        private bool _isTriggered = false;

        private void Awake()
        {
            // 보스는 처음엔 비활성화 상태여야 해 오빠! 🥰
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
            // 1. 입구 문 닫기! (오빤 이제 못 나가~ 😎)
            if (entryDoor != null)
            {
                entryDoor.Close();
            }

            // 잠시 긴장감을 조성하고...
            yield return new WaitForSeconds(1.0f);

            // 2. 보스 활성화! (BossHealth의 OnEnable에서 연출이 시작될 거야)
            if (bossObject != null)
            {
                bossObject.SetActive(true);
            }

            // 3. 보스 체력바 UI 나타내기!
            if (bossHealthUI != null)
            {
                bossHealthUI.gameObject.SetActive(true);
            }

            // 4. 보스 BGM으로 교체! (부드럽게 페이드 인 해줄게 오빠! 🥰)
            if (bossBGM != null && Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.FadeBGM(bossBGM, 1.5f);
            }

            // 🎉 보스 등장!
            Debug.Log("보스 등장 연출 시작!");
        }
    }
}
