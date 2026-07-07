#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;

    [Header("UI Panels")]
    [Tooltip("메인 로비 패널")]
    [SerializeField] private GameObject mainLobbyPanel;
    [Tooltip("싱글 플레이 선택 시 나타나는 패널")]
    [SerializeField] private GameObject selectSingleModelPanel;
    [Tooltip("멀티 플레이 선택 시 나타나는 패널")]
    [SerializeField] private GameObject multiplayerPanel;
    [Tooltip("튜토리얼 선택 시 나타나는 패널")]
    [SerializeField] private GameObject tutorialPanel;
    [Tooltip("X 선택 시 나타나는 패널")]
    [SerializeField] private GameObject exitPanel;
    [Tooltip("컴퓨터와 대결하기 선택 시 나타나는 패널")]
    [SerializeField] private GameObject singlePlayerPanel;
    [Tooltip("플레이어와 대결하기 선택 시 나타나는 패널")]
    [SerializeField] private GameObject localPlayerPanel;
    [Tooltip("방 생성 및 참가 시 나타나는 패널")]
    [SerializeField] private GameObject roomPanel;
    [Tooltip("방 생성 난이도 선택 시 나타나는 패널")]
    [SerializeField] private GameObject selectDifficultyLevelPanel;
    [Tooltip("코드 입력 시 나타나는 패널")]
    [SerializeField] private GameObject joinRoomPanel;
    [Tooltip("호스트 연결 끊김 시 나타나는 패널")]
    [SerializeField] private GameObject hostDisconnectedPanel;
    [Tooltip("암호 불일치 시 나타나는 패널")]
    [SerializeField] private GameObject joinFailedPanel;

    [Header("Setting UI")]
    [Tooltip("설정 선택 시 나타나는 패널")]
    [SerializeField] private GameObject settingPanel;
    [Tooltip("키 변경 선택 시 나타나는 패널")]
    [SerializeField] private GameObject keyBindingPanel;

    [Header("Multiplay UI")]
    [Tooltip("참여 코드")]
    [SerializeField] private TMP_Text createdJoinCodeText;
    [Tooltip("참여 코드 InputField")]
    [SerializeField] private TMP_InputField joinCodeInputField;

    [Header("Room UI")]
    [Tooltip("플레이어 1 패널")]
    [SerializeField] private GameObject p1Panel;
    [Tooltip("플레이어 1 상태 텍스트")]
    [SerializeField] private TMP_Text p1StatusText;
    [Tooltip("대기 중 텍스트")]
    [SerializeField] private GameObject waitingText;
    [Tooltip("플레이어 2 패널")]
    [SerializeField] private GameObject p2Panel;
    [Tooltip("플레이어 2 상태 텍스트")]
    [SerializeField] private TMP_Text p2StatusText;
    [Tooltip("레디 버튼")]
    [SerializeField] private GameObject readyButton;
    [Tooltip("게임 시작 버튼")]
    [SerializeField] private GameObject startGameButton;
    

    // 인세임 씬 설정
    [SerializeField] private string IngameSceneName = "Ingame Scene";

    // 사용자가 선택한 게임 모드를 저장할 임시 변수
    private GameState selectedGameState;

    private void Start()
    {
        if (exitPanel != null)
        {
            exitPanel.SetActive(false);
        }
        ShowPanel(mainLobbyPanel);

        // RelayManager의 이벤트를 구독하여 감시 시작
        if (RelayManager.Instance != null)
        {
            RelayManager.Instance.OnClientDisconnected += HandleHostDisconnected;
            RelayManager.Instance.OnJoinFailed += HandleJoinFailed; // 참가 실패 이벤트 구독
        }

        // NetworkLobbyManager가 스폰될 때 UI 업데이트를 시작하도록 이벤트 '예약'
        NetworkLobbyManager.OnInstanceSpawned += SetupRoomUI;
        // 클라이언트 연결 성공 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void OnDestroy()
    {
        // 파괴 시 이벤트 구독을 해제하여 메모리 누수 방지
        if (RelayManager.Instance != null)
        {
            RelayManager.Instance.OnClientDisconnected -= HandleHostDisconnected;
            RelayManager.Instance.OnJoinFailed -= HandleJoinFailed;
        }
        if (NetworkLobbyManager.Instance != null)
        {
            NetworkLobbyManager.Instance.PlayerStates.OnListChanged -= OnPlayerListChanged;
        }
        NetworkLobbyManager.OnInstanceSpawned -= SetupRoomUI;

        if (NetworkManager.Singleton != null)
        {
            // 클라이언트 연결 성공 이벤트 구독 해제
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }

    // 뒤로 가기 버튼 클릭할 시
    public void OnClick_BackToMainLobby()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);

        ShowPanel(mainLobbyPanel);
    }

    // 싱글 플레이 버튼 클릭할 시
    public void OnClick_ShowSelectSingleModelPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);

        ShowPanel(selectSingleModelPanel);
    }

    // 컴퓨터와 대결하기 버튼 클릭할 시
    public void OnClick_ShowSinglePlayerPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);

        selectedGameState = GameState.Single;
        ShowPanel(singlePlayerPanel);
    }

    // 플레이어와 대결하기 버튼 클릭할 시
    public void OnClick_ShowLocalPlayerPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        selectedGameState = GameState.Local;
        ShowPanel(localPlayerPanel);
    }

    // 싱글 플레이에서 버튼 클릭할 시
    public void OnClick_BackToSelectSingleMode()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        ShowPanel(selectSingleModelPanel);
    }

    // 멀티 플레이 버튼 클릭할 시
    public void OnClick_ShowMultiplayerPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        selectedGameState = GameState.Multi;
        ShowPanel(multiplayerPanel);
    }

    // 튜토리얼 버튼 클릭할 시
    public void OnClick_ShowTutorialPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        ShowPanel(tutorialPanel);
    }

    // 세팅 버튼 클릭할 시
    public void OnClick_ShowSettingPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        ShowPanel(settingPanel);
    }

    // 키 변경 버튼 클릭할 시
    public void OnClick_ShowKeyBindingPanelPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        ShowPanel(keyBindingPanel);
    }

    // X 버튼 클릭할 시
    public void OnClick_ShowExitPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        if (exitPanel != null)
        {
            exitPanel.SetActive(true);
        }
    }

    // 키 변경 패널에서 뒤로 가기 버튼 클릭할 시
    public void OnClick_BackToSetting()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        ShowPanel(settingPanel);
    }

    // 취소 버튼 클릭할 시
    public void OnClick_Cancel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        if (exitPanel != null)
        {
            exitPanel.SetActive(false);
        }
    }

    // 종료 버튼 클릭할 시
    public void OnClick_Exit()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        #if UNITY_EDITOR
        // 유니티 에디터에서 플레이 중일 경우, 에디터의 플레이 모드를 중지
        EditorApplication.isPlaying = false;
        #else
        // 실제 빌드된 게임(PC, Mac 등)에서는 어플리케이션을 종료
        Application.Quit();
        #endif
    }

    // 게임 난이도를 클릭할 시
    public void OnClick_StartGameWithDifficulty(string difficulty)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        GameDifficulty selectedDifficulty;
        PolicyManager policyManager = new PolicyManager();

        // 문자열로 난이도 설정
        switch (difficulty.ToLower())
        {
            case "eazy":
                selectedDifficulty = GameDifficulty.Eazy;
                policyManager.Configure(PolicyManager.Difficulty.Normal);
                break;
            case "hard":
                selectedDifficulty = GameDifficulty.Hard;
                policyManager.Configure(PolicyManager.Difficulty.Hard);
                break;
            default:
                selectedDifficulty = GameDifficulty.Normal;
                policyManager.Configure(PolicyManager.Difficulty.Normal);
                break;
        }

        NetworkManager.Singleton.StartHost();

        StartGame(selectedDifficulty, policyManager);
    }

    // 게임 시작 함수
    private void StartGame(GameDifficulty difficulty, PolicyManager policy)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(selectedGameState);
            GameManager.Instance.SetDifficultyAndPolicy(difficulty, policy);

            NetworkManager.Singleton.SceneManager.LoadScene(IngameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다!");
        }
    }

    // UI 패널을 관리할 헬퍼 함수
    private void ShowPanel(GameObject panelToShow)
    {
        if (mainLobbyPanel != null) mainLobbyPanel.SetActive(false);
        if (selectSingleModelPanel != null) selectSingleModelPanel.SetActive(false);
        if (singlePlayerPanel != null) singlePlayerPanel.SetActive(false);
        if (localPlayerPanel != null) localPlayerPanel.SetActive(false);
        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);
        if (selectDifficultyLevelPanel != null) selectDifficultyLevelPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
        if (hostDisconnectedPanel != null) hostDisconnectedPanel.SetActive(false);
        if (joinFailedPanel != null) joinFailedPanel.SetActive(false);
        if (keyBindingPanel != null) keyBindingPanel.SetActive(false);

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }
    }
    
    /// <summary>
    /// 멀티 플레이 영역
    /// </summary>

    // 멀티플레이 난이도 선택 패널 버튼 핸들러
    public async void OnClick_SelectDifficultyLevelPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        ShowPanel(selectDifficultyLevelPanel);
    }
    
    // 멀티플레이 방 생성 패널 버튼 핸들러
    public async void OnClick_CreateMultiplayerRoom(string difficulty)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        GameDifficulty selectedDifficulty;
        PolicyManager policyManager = new PolicyManager();

        // 문자열로 난이도 설정
        switch (difficulty.ToLower())
        {
            case "hard":
                selectedDifficulty = GameDifficulty.Hard;
                policyManager.Configure(PolicyManager.Difficulty.Hard);
                break;
            default:
                selectedDifficulty = GameDifficulty.Normal;
                policyManager.Configure(PolicyManager.Difficulty.Normal);
                break;
        }

        // 미리 게임 매니저 세팅
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(selectedGameState);
            GameManager.Instance.SetDifficultyAndPolicy(selectedDifficulty, policyManager);
        }

        // 게임 세팅이 끝난 후, 방 대기 패널로 전환
        ShowPanel(roomPanel);
        string joinCode = await RelayManager.Instance.CreateRelay();
        if (joinCode != null)
        {
            createdJoinCodeText.text = $"방 {joinCode}";
            
            if (GameManager.Instance != null && GameManager.Instance.IsHost)
            {
                GameManager.Instance.NetworkDifficulty.Value = selectedDifficulty;
                // NetworkDifficulty.OnValueChanged 콜백에 의해
                // 호스트 자신의 SetDifficultyAndPolicy도 자동으로 호출됩니다.
            }
        }
    }

    // 코드 입력 패널 버튼 핸들러
    public async void OnClick_JoinRoomWithCode()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        string joinCode = joinCodeInputField.text;
        if (!string.IsNullOrEmpty(joinCode))
        {
            await RelayManager.Instance.JoinRelay(joinCode);
        }
    }

    // 방 참가 클릭 시
    public void OnClick_ShowJoinRoomPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        ShowPanel(joinRoomPanel); // 코드 입력 패널로 전환
    }

    // 멀티 플레이 방에서 뒤로 가기 버튼 클릭할 시
    public void OnClick_BackToMultiPlayer()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        if (RelayManager.Instance != null)
        {
            RelayManager.Instance.ShutdownRelay();
        }

        selectedGameState = GameState.Multi;
        ShowPanel(multiplayerPanel);
    }

    // 호스트가 방을 나갈 때 릴레이 서버를 종료하는 함수
    public void OnClick_LeaveRoomAsHost()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        if (RelayManager.Instance != null)
        {
            RelayManager.Instance.ShutdownRelay();
        }
    }

    // 호스트 연결 끊김 시
    private void HandleHostDisconnected()
    {
        ShowPanel(hostDisconnectedPanel);
    }

    // 방 암호가 일치하지 않을 시
    private void HandleJoinFailed()
    {
        ShowPanel(joinFailedPanel);
    }

    // hostDisconnectedPanel의 확인 버튼을 누를 시 멀티 플레이 화면으로 전환
    public void OnClick_AcknowledgeDisconnect()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        ShowPanel(multiplayerPanel);
    }

    // joinFailedPanel의 확인 버튼을 누를 시 방 암호 입력 화면으로 전환
    public void OnClick_AcknowledgeJoinFailed()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        ShowPanel(joinRoomPanel);
    }

    // NetworkLobbyManager 생성 후 세팅 이벤트
    private void SetupRoomUI()
    {
        NetworkLobbyManager.Instance.PlayerStates.OnListChanged += OnPlayerListChanged;
        ShowPanel(roomPanel);
        UpdateRoomUI();
    }

    // 네트워크 플레이어 리스트 변경 시 자동으로 호출되어 UI 업데이트
    private void OnPlayerListChanged(NetworkListEvent<NetworkPlayerState> changeEvent)
    {
        UpdateRoomUI();
    }

    // UI 업데이트 로직
    private void UpdateRoomUI()
    {
        if (NetworkLobbyManager.Instance == null) return;
        var states = NetworkLobbyManager.Instance.PlayerStates;

        if (waitingText != null)
        {
            waitingText.SetActive(states.Count < 2);
        }

        p1Panel.SetActive(states.Count > 0);
        if (states.Count > 0)
        {
            p1StatusText.text = states[0].IsReady ? "<color=green>준비 완료</color>" : "대기 중";
        }

        p2Panel.SetActive(states.Count > 1);
        if (states.Count > 1)
        {
            p2StatusText.text = states[1].IsReady ? "<color=green>준비 완료</color>" : "대기 중";
        }

        if (startGameButton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                startGameButton.SetActive(NetworkLobbyManager.Instance.AreAllPlayersReady());
            }
            else
            {
                startGameButton.SetActive(false);
            }
        }
    }

    // 클라이언트가 연결되었을 때 호출될 함수
    private void HandleClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("클라이언트: 릴레이 서버에 성공적으로 연결되었습니다. 룸 UI를 설정합니다.");
            
            // SetupRoomUI()를 호출하면
            // 1. ShowPanel(roomPanel)이 실행되어 룸 패널이 켜지고,
            // 2. PlayerStates.OnListChanged 이벤트 구독이 시작됩니다. (중요)
            SetupRoomUI();
        }
        // (else인 경우, 즉 호스트 입장에서 클라이언트가 연결된 경우는
        // NetworkLobbyManager의 OnClientConnected에서 이미 처리 중입니다.)
    }
    
    // --- 버튼 핸들러들 ---
    public void OnClick_Ready()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        if (NetworkLobbyManager.Instance != null)
        {
            NetworkLobbyManager.Instance.SetPlayerReadyServerRpc();
        }
    }
    
    public void OnClick_StartMultiplayerGame()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        
        if (NetworkLobbyManager.Instance != null)
        {
            NetworkLobbyManager.Instance.StartGameServerRpc();
        }
    }
}
