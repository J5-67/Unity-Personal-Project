using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UI
{
    public enum SpeakerSide
    {
        Left,
        Right
    }

    public class DialogueUI : MonoBehaviour
    {
        [Header("Common UI")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TypewriterEffect typewriter;

        [Header("Left Speaker")]
        [SerializeField] private GameObject leftGroup;
        [SerializeField] private Image leftPortrait;
        [SerializeField] private TMP_Text leftName;

        [Header("Right Speaker")]
        [SerializeField] private GameObject rightGroup;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private TMP_Text rightName;

        [Header("Settings")]
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private bool hideInactive = false;

        private void Reset()
        {
            typewriter = GetComponentInChildren<TypewriterEffect>();
            messageText = typewriter != null ? typewriter.GetComponent<TMP_Text>() : GetComponentInChildren<TMP_Text>();
        }

        private void OnEnable()
        {
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.OnSkipDialogue += OnSkipDialogue;
        }

        private void OnDisable()
        {
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.OnSkipDialogue -= OnSkipDialogue;
        }

        private void OnSkipDialogue()
        {
            SkipTyping();
            Hide();
        }

        public void Show(string message, SpeakerSide side, string name, Sprite portrait, AudioClip typingSound = null)
        {

            message = message.Replace("\\n", "\n");

            if (Core.GameManager.Instance != null) Core.GameManager.Instance.SetDialogueState(true);

            gameObject.SetActive(true);

            Canvas parentCanvas = GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
            {
                parentCanvas.gameObject.SetActive(true);
            }

            dialoguePanel.SetActive(true);

            SetupSpeaker(side, name, portrait);

            if (typewriter != null)
            {
                typewriter.SetTypingSound(typingSound);
                typewriter.Run(message);
            }
            else
            {
                messageText.text = message;
            }
        }

        public bool IsTyping => typewriter != null && typewriter.IsTyping;

        public void SkipTyping()
        {
            if (typewriter != null) typewriter.Skip();
        }

        private void SetupSpeaker(SpeakerSide side, string name, Sprite portrait)
        {
            bool isLeft = (side == SpeakerSide.Left);

            if (leftGroup != null)
            {
                if (isLeft)
                {
                    leftGroup.SetActive(true);
                    if (leftName) leftName.text = name;
                    if (leftPortrait)
                    {
                        leftPortrait.sprite = portrait;
                        leftPortrait.color = activeColor;
                        leftPortrait.enabled = (portrait != null);
                    }
                }
                else
                {
                    if (hideInactive) leftGroup.SetActive(false);
                    else if (leftPortrait) leftPortrait.color = inactiveColor;
                }
            }

            if (rightGroup != null)
            {
                if (!isLeft)
                {
                    rightGroup.SetActive(true);
                    if (rightName) rightName.text = name;
                    if (rightPortrait)
                    {
                        rightPortrait.sprite = portrait;
                        rightPortrait.color = activeColor;
                        rightPortrait.enabled = (portrait != null);
                    }
                }
                else
                {
                    if (hideInactive) rightGroup.SetActive(false);
                    else if (rightPortrait) rightPortrait.color = inactiveColor;
                }
            }
        }

        public void Hide()
        {
            dialoguePanel.SetActive(false);

            if (Core.GameManager.Instance != null) Core.GameManager.Instance.SetDialogueState(false);
        }
    }
}
