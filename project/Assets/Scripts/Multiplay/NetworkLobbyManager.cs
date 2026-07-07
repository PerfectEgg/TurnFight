using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;

public class NetworkLobbyManager : NetworkBehaviour
{
    // 싱글톤
    public static NetworkLobbyManager Instance { get; private set; }

    // 인스턴스가 네트워크에 스폰되었음을 다른 스크립트(LobbyManager)에 방송
    public static event Action OnInstanceSpawned;

    public NetworkList<NetworkPlayerState> PlayerStates;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            PlayerStates = new NetworkList<NetworkPlayerState>();

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 서버(호스트)에서만 실행, 새로운 클라이언트가 연결될 때 호출
    public override void OnNetworkSpawn()
    {
        // 방송을 실행하여 LobbyManager에게 준비되었음을 알림
        OnInstanceSpawned?.Invoke();

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // 호스트 자신을 플레이어 리스트에 추가합니다.
            PlayerStates.Add(new NetworkPlayerState
            {
                ClientId = NetworkManager.Singleton.LocalClientId,
                IsReady = false,
                Wins = 0,
                Losses = 0
            });
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // 호스트 자신을 호출하는 문제를 해결하기 위한 리턴
        if (clientId == NetworkManager.Singleton.LocalClientId) return;

        // 새로운 클라이언트를 플레이어 리스트에 추가합니다.
        PlayerStates.Add(new NetworkPlayerState
        {
            ClientId = clientId,
            IsReady = false,
            Wins = 0,
            Losses = 0
        });
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // 연결이 끊긴 클라이언트를 리스트에서 찾아서 제거합니다.
        for (int i = 0; i < PlayerStates.Count; i++)
        {
            if (PlayerStates[i].ClientId == clientId)
            {
                PlayerStates.RemoveAt(i);
                break;
            }
        }
    }

    // NetworkBehaviour가 파괴되거나 네트워크에서 사라질 때 호출
    public override void OnNetworkDespawn()
    {
        // 서버일 때만 구독했으므로 서버일 때만 구독 해제
        if (IsServer)
        {
            // NetworkManager가 아직 존재할 때만 안전하게 접근
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        base.OnNetworkDespawn();
    }

    // 클라이언트가 서버에게 보내는 원격 함수 호출

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        // 준비 버튼을 누른 클라이언트를 찾아서 IsReady 상태를 변경
        for (int i = 0; i < PlayerStates.Count; i++)
        {
            if (PlayerStates[i].ClientId == rpcParams.Receive.SenderClientId)
            {
                var state = PlayerStates[i];
                state.IsReady = !state.IsReady; // 준비 <-> 준비 취소 토글
                PlayerStates[i] = state;
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        // 모든 플레이어가 준비되었는지 확인합니다. (호스트만 호출 가능)
        if (!AreAllPlayersReady()) return;

        // 모든 클라이언트에게 게임 씬으로 이동하라고 명령합니다.
        NetworkManager.Singleton.SceneManager.LoadScene("Ingame Scene", LoadSceneMode.Single);
    }
    
    // 두 플레이어의 레디 여부 확인
    public bool AreAllPlayersReady()
    {
        if (PlayerStates.Count < 2) return false; // 최소 2명 필요

        foreach (var state in PlayerStates)
        {
            if (!state.IsReady) return false;
        }
        return true;
    }
}
