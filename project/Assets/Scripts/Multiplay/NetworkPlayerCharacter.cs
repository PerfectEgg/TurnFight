using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerCharacter : NetworkBehaviour
{
    [Header("Visuals")]
    [Tooltip("플레이어 1 캐릭터 자식 오브젝트")]
    [SerializeField] private GameObject visualP1;
    [Tooltip("플레이어 2 캐릭터 자식 오브젝트")]
    [SerializeField] private GameObject visualP2;

    // 이 오브젝트가 네트워크에 '스폰'될 때 모든 클라이언트에서 호출됩니다.
    public override void OnNetworkSpawn()
    {
        GameObject activeVisual = null;

        // --- 1. P1/P2 모습 결정 ---
        if (OwnerClientId == 0) // 이 캐릭터의 주인이 호스트(P1)인가?
        {
            gameObject.name = "PlayerCharacter_Host";
            visualP1.SetActive(true);
            visualP2.SetActive(false);
            activeVisual = visualP1;
        }
        else
        {
            gameObject.name = $"PlayerCharacter_Client_{OwnerClientId}";
            visualP1.SetActive(false);
            visualP2.SetActive(true);
            activeVisual = visualP2;
        }

        // --- 2. 애니메이터 자동 등록 ---
        // PlayerAnimatorRegistrator의 역할을 이 스크립트가 직접 수행합니다.
        if (activeVisual != null)
        {
            Animator animator = activeVisual.GetComponent<Animator>();
            if (animator != null && CharacterAnimatorControl.Instance != null)
            {
                // 자신의 ClientId를 사용하여 중앙 관제탑에 애니메이터를 등록합니다.
                CharacterAnimatorControl.Instance.RegisterPlayerAnimator(animator, (int)OwnerClientId);
            }
        }

        // --- 3. '내 캐릭터'일 때만 입력 받기 ---
        if (IsOwner)
        {
            Debug.Log($"<color=cyan>내 캐릭터({gameObject.name}) 스폰 완료!</color>");
            // TODO: 여기서 PlayerInputManager의 입력을 받도록 이벤트를 구독해야 합니다.
            // PlayerInputManager.Instance.OnPlayerInput += HandleLocalInput;
        }
    }
}
