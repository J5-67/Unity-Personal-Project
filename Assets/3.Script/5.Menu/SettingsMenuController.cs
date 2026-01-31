using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

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

        private StringBuilder _sb = new StringBuilder(10); // [유니] 스트링 빌더 미리 생성!

        private void Start()
        {
            // [유니] 저장된 값 불러오기 및 리스너 등록
            InitSettings();
        }

        private void InitSettings()
        {
            // Graphic
            if (graphicDropdown != null)
            {
                // 디폴트 0: Full Screen, 1: Windowed 라고 가정!
                int graphicOption = PlayerPrefs.GetInt("FullScreen", 0); 
                graphicDropdown.value = graphicOption;
                graphicDropdown.RefreshShownValue();
                graphicDropdown.onValueChanged.AddListener(OnGraphicChanged);
                
                // [유니] 시작할 때도 적용!
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

        // [유니] 초기 세팅 헬퍼 함수
        private void SetupControl(Slider slider, TMP_InputField input, float value, UnityEngine.Events.UnityAction<float> onSliderChange)
        {
            // 값 적용
            if(slider) 
            {
                slider.value = value;
                slider.onValueChanged.AddListener(onSliderChange);
            }
            
            UpdatePlaceholder(input, value);

            if(input)
            {
                // [유니] 인풋필드 입력 종료 시 이벤트 연결
                input.onEndEdit.AddListener((str) => OnInputSubmitted(str, slider, input, onSliderChange));
            }
        }

        // [유니] 플레이스홀더 텍스트만 변경하는 함수!
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
            // 0: FullScreen, 1: Windowed
            bool isFull = (index == 0);
            Screen.fullScreen = isFull;
        }

        private void OnMouseChanged(float value)
        {
            // [유니] 슬라이더 값이 바뀌면 저장하고 플레이스홀더 업데이트!
            PlayerPrefs.SetFloat("Sensitivity", value);
            UpdatePlaceholder(mouseInput, value);
        }

        private void OnMasterChanged(float value)
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            UpdatePlaceholder(masterInput, value);
            // [유니] 실제 오디오 믹서 연결은 나중에 여기서 하면 돼!
        }

        private void OnBGMChanged(float value)
        {
            PlayerPrefs.SetFloat("BGMVolume", value);
            UpdatePlaceholder(bgmInput, value);
        }

        private void OnSFXChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            UpdatePlaceholder(sfxInput, value);
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
