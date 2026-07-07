using UnityEngine;

// 이 스크립트는 Animator 컴포넌트가 있는 오브젝트에만 추가할 수 있도록 강제
[RequireComponent(typeof(Animator))]
public class PlayerAnimatorRegistrator : MonoBehaviour
{
    [Tooltip("이 캐릭터의 고유 번호 (P1: 1, P2 또는 CPU: 2)")]
    public int playerNumber;
    
    // 이 오브젝트가 씬에 생성되어 활성화될 때 자동 호출
    void Start()
    {
        // 1. 자기 자신에게 붙어있는 Animator 컴포넌트 찾기
        Animator myAnimator = GetComponent<Animator>();

        // 2. 중앙 관제탑(CharacterAnimatorControl)이 존재하는지 확인
        if (CharacterAnimatorControl.Instance != null)
        {
            // 3. 관제탑에 플레이어와 애니메이터 정보 제공 후 등록 요청 송신
            CharacterAnimatorControl.Instance.RegisterPlayerAnimator(myAnimator, playerNumber);
        }
        else
        {
            // 관제탑이 없는 경우, 에러 메시지 출력
            Debug.LogError("씬에 CharacterAnimatorControl 스크립트가 존재하지 않습니다! GameManager 오브젝트를 확인해주세요.");
        }
    }
}

