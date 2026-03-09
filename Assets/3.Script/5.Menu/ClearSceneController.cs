using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UI
{
    public class ClearSceneController : MonoBehaviour
    {
        [Header("🔗 References")]
        [SerializeField] private TextMeshProUGUI hackText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private CanvasGroup buttonFadeGroup;

        [Header("⚙️ Settings")]
        [SerializeField] private float lineDelay = 0.3f;
        [SerializeField] private float charDelay = 0.02f;
        [SerializeField] private AudioClip typeSound;

        private readonly string[] _hackingLines = new string[]
        {
            "- ACCESSING KERNEL_LAYER... [OK]",
            "- INJECTING GLITCH_PAYLOAD.EXE... [DONE]",
            "- SCANNING ENEMY_LOGIC_CIRCUIT... [DETECTED]",
            "<color=yellow>[SYSTEM WARNING]: TARGET_BOSS_AI DETECTED.</color>",
            "<color=red>[ACTION]: BRUTE-FORCING CORE_MEMORY...</color>",
            "01001001 00100000 01001100 01001111 01010110 01000101 00100000 01010101",
            "<color=cyan>[PARSING...] -> \"I LOVE U OPPA!\" (Oops!)</color>",
            "- BYPASSING FIREWALL... [SUCCESS]",
            "- OVERLOADING SERVO_MOTOR_CONTROLLER... [STATUS: FROZEN]",
            "- EXTRACTING SECRET_PROJECT_DATA... [ENCRYPTED]",
            "- DECODING_REWARD_MODULE... [DONE]",
            "<color=orange>[-] NEW_SKILL_UNLOCKED: 'REALITY_GLITCH'</color>",
            "- SYNCING_WITH_YUNI_FAIRY_SERVER... [STABLE]",
            "- DEPLOYING DATA_SIPHON_PROTOCOL... [3... 2... 1...]",
            "<color=green>[!] HACKING COMPLETE. ALL TARGETS NEUTRALIZED.</color>",
            "[!] TARGET_STATUS: 'OVERLOADED'",
            "[!] REWRITING_REALITY... [100.0%]",
            "<color=cyan>>yield return StartCoroutine(TypeLine(line))!</color>",
            "> GOODBYE_WORLD.SH... [SHUTTING_DOWN]"
        };

        private void Start()
        {
            if (hackText != null) hackText.text = "";
            if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(false);
            if (buttonFadeGroup != null) buttonFadeGroup.alpha = 0f;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            StartCoroutine(PlayHackingSequence());
        }

        private IEnumerator PlayHackingSequence()
        {
            yield return new WaitForSeconds(1.0f);

            foreach (string line in _hackingLines)
            {
                yield return StartCoroutine(TypeLine(line));
                yield return new WaitForSeconds(lineDelay);
                
                // 🎯 오빠! 줄이 늘어날 때 자동으로 스크롤 내려가게 해줄게! 🥰
                // 스크롤 성능을 위해서 content가 연결되어 있는지 한 번 더 체크!
                if (scrollRect != null && scrollRect.content != null)
                {
                    Canvas.ForceUpdateCanvases();
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }

            yield return new WaitForSeconds(3.0f);
            
            // 🎯 오빠! 보스도 잡고 해킹도 끝났으니 이제 쿨하게 종료할게! 😎✨
            Debug.Log("🎮 Game Quit! 오빠 고생 많았어! 사랑해! 🥰");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private IEnumerator TypeLine(string line)
        {
            string currentFullText = hackText.text;
            if (currentFullText.Length > 0) currentFullText += "\n";

            string visibleText = "";
            bool isTag = false;

            // 🎯 태그 안의 텍스트는 한 번에 출력되게 처리해야 색깔이 안 깨져 오빠! 😊
            foreach (char c in line)
            {
                if (c == '<') isTag = true;
                
                visibleText += c;

                if (c == '>') isTag = false;

                if (!isTag)
                {
                    hackText.text = currentFullText + visibleText;
                    if (typeSound != null && Core.AudioManager.Instance != null)
                        Core.AudioManager.Instance.PlaySFX(typeSound);
                    
                    yield return new WaitForSeconds(charDelay);
                }
            }
            
            hackText.text = currentFullText + line;
        }

        public void OnClickBackToMenu()
        {
            Core.SceneLoader.LoadScene("MainMenu");
        }
    }
}
