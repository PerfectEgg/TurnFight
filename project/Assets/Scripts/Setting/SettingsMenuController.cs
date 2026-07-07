using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    // 볼륨 조절 슬라이더
    [SerializeField] private Slider volumeSlider;
    // 볼륨 조절 값 텍스트
    [SerializeField] private TMP_Text volumeValueText;
    // fast/slow 표시 토글
    [SerializeField] private Toggle showTimingToggle;
    // 판정 타이밍 조절 슬라이더
    [SerializeField] private Slider timingOffsetSlider;
    // 판정 타이밍 조절 값 텍스트
    [SerializeField] private TMP_Text timingOffsetValueText;

    [Header("Buttons")]
    // 키 변경 창 표시
    [SerializeField] private Button keyBindingButton;

    [Header("Other Panels")]
    // 키 변경 전용 패널
    [SerializeField] private GameObject keyBindingPanel;
    
    private void Start()
    {
        // SettingsManager에서 현재 설정 값을 불러와 UI에 적용
        if (SettingsManager.Instance != null)
        {
            volumeSlider.value = SettingsManager.Instance.MasterVolume;
            showTimingToggle.isOn = SettingsManager.Instance.ShowInputTiming;
            timingOffsetSlider.value = SettingsManager.Instance.InputTimingOffset;
            
            UpdateVolumeText(volumeSlider.value);
            UpdateTimingOffsetText(timingOffsetSlider.value);
        }

        // 각 UI 요소에 리스너를 추가하여 값이 변경될 때마다 함수가 호출
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        showTimingToggle.onValueChanged.AddListener(OnShowTimingChanged);
        timingOffsetSlider.onValueChanged.AddListener(OnTimingOffsetChanged);
        
        keyBindingButton.onClick.AddListener(OnKeyBindingPressed);
    }

    // --- UI 이벤트 핸들러 함수들 ---

    private void OnVolumeChanged(float value)
    {
        UpdateVolumeText(value);
        if(SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetMasterVolume(value);
        }
    }

    private void OnShowTimingChanged(bool isOn)
    {
        if(SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetShowInputTiming(isOn);
        }
    }

    private void OnTimingOffsetChanged(float value)
    {
        UpdateTimingOffsetText(value);
        if(SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetInputTimingOffset(value);
        }
    }

    private void OnKeyBindingPressed()
    {
        LobbyManager.instance.OnClick_ShowKeyBindingPanelPanel();
    }
    
    // 이 함수는 이 스크립트가 비활성화될 때 호출
    private void OnDisable()
    {
        // 패널이 닫힐 때 현재 설정값을 최종 저장
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings();
        }
    }

    // --- UI 텍스트 업데이트 헬퍼 ---

    private void UpdateVolumeText(float value)
    {
        volumeValueText.text = (value * 100).ToString("F0"); // 0~1 값을 0~100으로 변환
    }

    private void UpdateTimingOffsetText(float value)
    {
        timingOffsetValueText.text = value.ToString("F0") + "ms";
    }
}
