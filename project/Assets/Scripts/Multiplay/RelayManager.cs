using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class RelayManager : NetworkBehaviour
{
    public static RelayManager Instance { get; private set; }

    [Header("Network Prefabs")]
    [SerializeField] private GameObject networkLobbyManagerPrefab;

    // 호스트가 방을 나거가나 강제 종료할 경우 발생할 이벤트
    public event Action OnClientDisconnected;
    // 방 참가에 실패했을 때 발생하는 이벤트
    public event Action OnJoinFailed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"<color=yellow>UGS 로그인 성공! Player ID: {AuthenticationService.Instance.PlayerId}</color>");
        }

        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        }
    }

    private void HandleServerStarted()
    {
        // 이 이벤트는 호스트에서만 발생, 그리고 멀티 플레일 경우만 발생
        if (networkLobbyManagerPrefab != null && GameManager.Instance != null && GameManager.Instance.currentGameState.Value == GameState.Multi)
        {
            Debug.Log("서버 시작됨. NetworkLobbyManager 스폰 시도...");
            GameObject managerInstance = Instantiate(networkLobbyManagerPrefab);
            managerInstance.GetComponent<NetworkObject>().Spawn();
        }
    }

    public override void OnNetworkSpawn()
    {
        // NetworkManager의 OnClientDisconnectCallback 이벤트를 구독하여 연결 끊김 감시
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        // 만약 내가 클라이언트이고, 스스로 나간 것이 아닌 호스트가 방을 닫았다면
        if (!IsServer && clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("호스트와의 연결이 끊겼습니다. OnClientDisconnected 이벤트를 호출합니다.");
            // OnClientDisconnected 이벤트를 발생시켜 LobbyManager에게 알림
            OnClientDisconnected?.Invoke();
        }
    }

    public async Task<string> CreateRelay()
    {
        // NetworkManager가 실행 중인 경우, 아무것도 하지 않음
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            return null;
        }

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"릴레이 방 생성 실패: {e.Message}");
            return null;
        }
    }

    public async Task JoinRelay(string joinCode)
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            Debug.Log("NetworkManager가 이미 실행 중입니다.");
            return;
        }

        try
        {
            Debug.Log("JoinRelay로 진입");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"릴레이 방 참가 실패: {e.Message}");
            // 실패 이벤트를 발생시켜 LobbyManager에게 알림
            OnJoinFailed?.Invoke();
        }
    }
    
    public void ShutdownRelay()
    {

        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient))
        {
            Debug.Log("네트워크 세션을 종료합니다...");
            NetworkManager.Singleton.Shutdown();
        }
    }
}

