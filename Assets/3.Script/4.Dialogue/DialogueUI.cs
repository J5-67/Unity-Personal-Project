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
        [SerializeField] private GameObject dialoguePanel; // 검은 판넬
        [SerializeField] private TMP_Text messageText;     // 대화 내용
        [SerializeField] private TypewriterEffect typewriter; 

        [Header("Left Speaker")]
        [SerializeField] private GameObject leftGroup;     // 왼쪽 그룹 (Portrait_Left)
        [SerializeField] private Image leftPortrait;       // 왼쪽 초상화
        [SerializeField] private TMP_Text leftName;        // 왼쪽 이름

        [Header("Right Speaker")]
        [SerializeField] private GameObject rightGroup;    // 오른쪽 그룹 (Portrait_Right)
        [SerializeField] private Image rightPortrait;      // 오른쪽 초상화
        [SerializeField] private TMP_Text rightName;       // 오른쪽 이름

        [Header("Settings")]
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 비활성 시 어둡게
        [SerializeField] private bool hideInactive = false; // 비활성화된 쪽을 아예 숨길지 여부

        private void Reset()
        {
            typewriter = GetComponentInChildren<TypewriterEffect>();
            messageText = typewriter != null ? typewriter.GetComponent<TMP_Text>() : GetComponentInChildren<TMP_Text>();
        }

        public void Show(string message, SpeakerSide side, string name, Sprite portrait)
        {
            dialoguePanel.SetActive(true);

            // 1. 화자 설정 (왼쪽/오른쪽)
            SetupSpeaker(side, name, portrait);

            // 2. 텍스트 출력
            if (typewriter != null)
            {
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

            // 왼쪽 UI 설정
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
                        // [유니] 이미지가 없어도 이름은 나와야 하니까, 오브젝트를 끄는 게 아니라 이미지 컴포넌트만 꺼줄게!
                        leftPortrait.enabled = (portrait != null);
                    }
                }
                else
                {
                    // 비활성 처리 (숨기거나 어둡게)
                    if (hideInactive) leftGroup.SetActive(false);
                    else if (leftPortrait) leftPortrait.color = inactiveColor;
                }
            }

            // 오른쪽 UI 설정
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
                        // [유니] 여기도 마찬가지로 이미지 컴포넌트만 조절!
                        rightPortrait.enabled = (portrait != null);
                    }
                }
                else
                {
                    // 비활성 처리
                    if (hideInactive) rightGroup.SetActive(false);
                    else if (rightPortrait) rightPortrait.color = inactiveColor;
                }
            }
        }

        public void Hide()
        {
            dialoguePanel.SetActive(false);
        }
    }
}
