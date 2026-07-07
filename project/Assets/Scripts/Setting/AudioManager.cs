using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("오디오 재생기 (인스펙터에서 할당)")]
    [SerializeField] private AudioSource bgmSource; // 배경 음악
    [SerializeField] private AudioSource sfxSource; // 효과음
    [SerializeField] private AudioSource chargingSource; // 차징 효과음

    [Header("오디오 클립")]
    public AudioClip lobbyMusic;
    public AudioClip ingameMusic;
    public AudioClip buttonSound;
    public AudioClip noteSound;
    public AudioClip hitSound;
    public AudioClip nonePassSound;
    public AudioClip guardSound;
    public AudioClip KOLoseSound;
    public AudioClip ChargingSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource != null)
        {
            bgmSource.loop = true;
        }

        if (chargingSource != null)
        {
            chargingSource.loop = true;
        }

    }

    private void Start()
    {
        // 씬이 바뀔 때 BGM을 교체하는 로직
        SceneManager.sceneLoaded += OnSceneLoaded;

        SetMasterVolume(0.5f);

        // SettingsManager가 준비되면 즉시 볼륨을 적용 후 볼륨 설정이 변경될 때마다 OnVolumeChanged 함수를 호출하도록 구독
        if (SettingsManager.Instance != null)
        {
            SetMasterVolume(SettingsManager.Instance.MasterVolume);
            SettingsManager.Instance.OnSettingsChanged += () => SetMasterVolume(SettingsManager.Instance.MasterVolume);
        }

        PlayBGM(lobbyMusic); // 게임 실행 시 로비 음악 재생
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬 로드 시 BGM 자동 교체
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "Ingame Scene":
                PlayBGM(ingameMusic);
                break;
            case "Lobby Scene":
                PlayBGM(lobbyMusic);
                break;
        }
    }

    public void SetMasterVolume(float volume)
    {
        if (bgmSource != null) bgmSource.volume = volume * 0.4f;
        if (sfxSource != null) sfxSource.volume = volume * 0.8f;
        if (chargingSource != null) chargingSource.volume = volume;
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip)
        {
            return;
        }
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayCharging()
    {
        if (ChargingSound == null) return;
        
        chargingSource.clip = ChargingSound;
        chargingSource.Play();
    }

    public void StopCharging()
    {
        if (ChargingSound == null) return;
        
        chargingSource.clip = ChargingSound;
        chargingSource.Stop();
    }
}
