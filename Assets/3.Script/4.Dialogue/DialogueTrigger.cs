using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(Collider))] // [유니] 콜라이더가 꼭 필요해!
    public class DialogueTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int startId; // 시작 ID
        [SerializeField] private int endId;   // 끝 ID
        [SerializeField] private bool runOnlyOnce = true; // [유니] 한 번만 실행할지 여부!

        private bool _hasRun = false;

        private void Awake()
        {
            // [유니] 실수로 Trigger 체크 안 했을까 봐 코드로 확실하게!
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // [유니] 이미 실행됐고, 한 번만 실행하는 모드라면 무시!
            if (runOnlyOnce && _hasRun) return;

            // [유니] 플레이어 태그 확인 (Player 태그가 맞는지 꼭 확인해줘 오빠!)
            if (other.CompareTag("Player"))
            {
                // [유니] 싱글톤으로 쉽게 호출! 🎵
                if (DialogueTester.Instance != null)
                {
                    DialogueTester.Instance.PlayDialogueRange(startId, endId);
                    _hasRun = true;
                    
                    // [유니] 더 이상 필요 없으면 오브젝트 꺼버리기 (깔끔하게!)
                    if (runOnlyOnce)
                    {
                        // gameObject.SetActive(false); // 끄고 싶으면 이거 주석 해제!
                    }
                }
                else
                {
                    Debug.LogError("[유니] 앗! Scene에 DialogueTester가 없나 봐! 😭");
                }
            }
        }
    }
}
