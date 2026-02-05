using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using Core; 

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
            if (graphicDropdown != null)
            {
                int graphicOption = PlayerPrefs.GetInt("FullScreen", 0); 
                graphicDropdown.value = graphicOption;
                graphicDropdown.RefreshShownValue();
                graphicDropdown.onValueChanged.AddListener(OnGraphicChanged);
                SetFullScreen(graphicOption);
            }

            float sensitivity = PlayerPrefs.GetFloat("Sensitivity", 100f);
            SetupControl(mouseSlider, mouseInput, sensitivity, OnMouseChanged);

            float master = PlayerPrefs.GetFloat("MasterVolume", 100f);
            SetupControl(masterSlider, masterInput, master, OnMasterChanged);

            float bgm = PlayerPrefs.GetFloat("BGMVolume", 100f);
            SetupControl(bgmSlider, bgmInput, bgm, OnBGMChanged);

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

            float realSensitivity = 0.5f + (value / 100f) * 2.0f;

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
            }
        }

        private void OnInputSubmitted(string inputStr, Slider targetSlider, TMP_InputField selfInput, UnityEngine.Events.UnityAction<float> callback)
        {
            if (float.TryParse(inputStr, out float value))
            {
                if (targetSlider)
                {
                    value = Mathf.Clamp(value, targetSlider.minValue, targetSlider.maxValue);
                    targetSlider.value = value; 
                    
                    callback(value);
                }
            }

            selfInput.text = "";
            if (targetSlider) UpdatePlaceholder(selfInput, targetSlider.value);
        }

        #endregion
    }
}
