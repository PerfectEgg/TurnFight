using UnityEngine;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // 설정값 속성 (디폴트 설정)
    public float MasterVolume { get; private set; } = 0.5f;
    public bool ShowInputTiming { get; private set; } = false;
    public float InputTimingOffset { get; private set; } = 0f;

    // 설정값이 변경될 때 다른 스크립트에 알리기 위한 이벤트
    public event Action OnSettingsChanged;

    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string SHOW_TIMING_KEY = "ShowInputTiming";
    private const string TIMING_OFFSET_KEY = "InputTimingOffset";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings(); // 게임 시작 시 저장된 설정 불러오기
    }

    // 설정값을 PlayerPrefs에서 불러오기
    private void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 0.5f);
        ShowInputTiming = PlayerPrefs.GetInt(SHOW_TIMING_KEY, 0) == 1;

        InputTimingOffset = PlayerPrefs.GetFloat(TIMING_OFFSET_KEY, -12f);
    }

    // 설정값을 PlayerPrefs에 저장하기
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, MasterVolume);
        PlayerPrefs.SetInt(SHOW_TIMING_KEY, ShowInputTiming ? 1 : 0);
        PlayerPrefs.SetFloat(TIMING_OFFSET_KEY, InputTimingOffset);
        PlayerPrefs.Save();
        
        OnSettingsChanged?.Invoke(); // 설정이 저장되었음을 모두에게 알림
    }

    // UI 컨트롤러가 호출할 함수들
    public void SetMasterVolume(float volume)
    {
        MasterVolume = volume;
        OnSettingsChanged?.Invoke(); // 볼륨이 실시간으로 변경됨을 알림
    }

    public void SetShowInputTiming(bool isVisible)
    {
        ShowInputTiming = isVisible;
        OnSettingsChanged?.Invoke();
    }

    public void SetInputTimingOffset(float offset)
    {
        InputTimingOffset = offset;
    }
}
