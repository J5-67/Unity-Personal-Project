using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using Core; // [유니] AudioManager 사용을 위해 추가!

namespace UI
{
    public class SettingsMenuController : MonoBehaviour
    {
        [Header("Graphic Settings")]
        [SerializeField] private TMP_Dropdown graphicDropdown;

        [Header("Mouse Settings")]
        [SerializeField] private Slider mouseSlider;
        [SerializeField] private TMP_InputField mouseInput;

        [Header("Audio Settings")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private TMP_InputField masterInput;

        [SerializeField] private Slider bgmSlider;
        [SerializeField] private TMP_InputField bgmInput;

        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_InputField sfxInput;

        private StringBuilder _sb = new StringBuilder(10); 

        private void Start()
        {
            InitSettings();
        }

        private void InitSettings()
        {
            // Graphic
            if (graphicDropdown != null)
            {
                int graphicOption = PlayerPrefs.GetInt("FullScreen", 0); 
                graphicDropdown.value = graphicOption;
                graphicDropdown.RefreshShownValue();
                graphicDropdown.onValueChanged.AddListener(OnGraphicChanged);
                SetFullScreen(graphicOption);
            }

            // Mouse
            float sensitivity = PlayerPrefs.GetFloat("Sensitivity", 100f);
            SetupControl(mouseSlider, mouseInput, sensitivity, OnMouseChanged);

            // Master
            float master = PlayerPrefs.GetFloat("MasterVolume", 100f);
            SetupControl(masterSlider, masterInput, master, OnMasterChanged);

            // BGM
            float bgm = PlayerPrefs.GetFloat("BGMVolume", 100f);
            SetupControl(bgmSlider, bgmInput, bgm, OnBGMChanged);

            // SFX
            float sfx = PlayerPrefs.GetFloat("SFXVolume", 100f);
            SetupControl(sfxSlider, sfxInput, sfx, OnSFXChanged);
        }

        private void SetupControl(Slider slider, TMP_InputField input, float value, UnityEngine.Events.UnityAction<float> onSliderChange)
        {
            if(slider) 
            {
                slider.value = value;
                slider.onValueChanged.AddListener(onSliderChange);
            }
            
            UpdatePlaceholder(input, value);

            if(input)
            {
                input.onEndEdit.AddListener((str) => OnInputSubmitted(str, slider, input, onSliderChange));
            }
        }

        private void UpdatePlaceholder(TMP_InputField input, float value)
        {
            if (input != null && input.placeholder is TMP_Text placeholderText)
            {
                _sb.Clear();
                _sb.Append(Mathf.RoundToInt(value));
                placeholderText.text = _sb.ToString();
            }
        }

        #region Event Handlers

        private void OnGraphicChanged(int index)
        {
            PlayerPrefs.SetInt("FullScreen", index);
            SetFullScreen(index);
        }
        
        private void SetFullScreen(int index)
        {
            bool isFull = (index == 0);
            Screen.fullScreen = isFull;
        }

        private void OnMouseChanged(float value)
        {
            PlayerPrefs.SetFloat("Sensitivity", value);
            UpdatePlaceholder(mouseInput, value);

            // [유니] UI 값(0~100)을 실제 감도(0.5 ~ 2.5)로 변환!
            // 0 -> 0.5
            // 100 -> 2.5 (0.5 + 2.0)
            float realSensitivity = 0.5f + (value / 100f) * 2.0f;

            // [유니] 씬에 있는 PlayerAim 찾아서 즉시 적용!
            PlayerAim playerAim = FindAnyObjectByType<PlayerAim>();
            if (playerAim != null)
            {
                playerAim.SetSensitivity(realSensitivity);
            }
        }

        private void OnMasterChanged(float value)
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            UpdatePlaceholder(masterInput, value);
            
            // [유니] AudioManager에 즉시 반영! 📢
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
        }

        private void OnBGMChanged(float value)
        {
            PlayerPrefs.SetFloat("BGMVolume", value);
            UpdatePlaceholder(bgmInput, value);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(value);
            }
        }

        private void OnSFXChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            UpdatePlaceholder(sfxInput, value);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
                // [유니] SFX는 조절할 때마다 소리가 나면 좋겠지? (옵션)
                // AudioManager.Instance.PlayTestSFX(); 
            }
        }

        // [유니] 인풋필드 입력이 끝났을 때 처리 (공통)
        private void OnInputSubmitted(string inputStr, Slider targetSlider, TMP_InputField selfInput, UnityEngine.Events.UnityAction<float> callback)
        {
            if (float.TryParse(inputStr, out float value))
            {
                // [유니] 슬라이더 범위 내로 클램핑 (보통 0~100)
                if (targetSlider)
                {
                    value = Mathf.Clamp(value, targetSlider.minValue, targetSlider.maxValue);
                    targetSlider.value = value; // 여기서 슬라이더 이벤트(callback)가 발생할 수도 있음 (설정에 따라 다름)
                    
                    // [유니] 값을 강제로 한 번 더 저장/업데이트 (슬라이더 이벤트가 안 돌 수도 있으니까)
                    callback(value);
                }
            }

            // [유니] 입력 텍스트 비우고, 플레이스홀더는 갱신된 값 보여주기
            selfInput.text = "";
            if (targetSlider) UpdatePlaceholder(selfInput, targetSlider.value);
        }

        #endregion
    }
}
