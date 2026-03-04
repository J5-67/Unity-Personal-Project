using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(Collider))]
    public class DialogueTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int startId;
        [SerializeField] private int endId;
        [SerializeField] private bool runOnlyOnce = true;

        private bool _hasRun = false;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (runOnlyOnce && _hasRun) return;

            if (other.CompareTag("Player"))
            {
                if (DialogueTester.Instance != null)
                {
                    DialogueTester.Instance.PlayDialogueRange(startId, endId);
                    _hasRun = true;

                    if (runOnlyOnce)
                    {
                    }
                }
                else
                {

                }
            }
        }
    }
}
