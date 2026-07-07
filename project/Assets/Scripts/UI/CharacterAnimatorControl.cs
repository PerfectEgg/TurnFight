using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class CharacterAnimatorControl : MonoBehaviour
{
    public static CharacterAnimatorControl Instance { get; private set; }

    private Dictionary<int, Animator> characterAnimators = new Dictionary<int, Animator>();

    private readonly int Idel = Animator.StringToHash("Idel");
    private readonly int GuardHash = Animator.StringToHash("Guard");
    private readonly int ChargingHash = Animator.StringToHash("Charging");

    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int attackTypeHash = Animator.StringToHash("AttackType");

    private readonly int hitHash = Animator.StringToHash("Hit");
    private readonly int hitTypeHash = Animator.StringToHash("HitType");

    private readonly int WinHash = Animator.StringToHash("Win");
    private readonly int LoseHash = Animator.StringToHash("Lose");
    private readonly int KOLoseHash = Animator.StringToHash("KOLose");

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPlayerAnimator(Animator playerAnimator, int playerNumber)
    {
        if (characterAnimators.ContainsKey(playerNumber))
        {
            // 이미 해당 번호의 캐릭터가 등록되어 있으면 정보를 갱신
            characterAnimators[playerNumber] = playerAnimator;
            Debug.Log($"플레이어 {playerNumber}의 Animator를 갱신했습니다.");
        }
        else
        {
            // 새로 등록하는 경우 목록에 추가
            characterAnimators.Add(playerNumber, playerAnimator);
            Debug.Log($"플레이어 {playerNumber}의 Animator를 새로 등록했습니다.");
        }
    }

    public void TriggerGuard(int playerNumber)
    {
        if (characterAnimators.TryGetValue(playerNumber, out Animator targetAnimator))
        {
            targetAnimator.SetTrigger(GuardHash);
        }
    }

    public void TriggerCharging(int playerNumber)
    {
        if (characterAnimators.TryGetValue(playerNumber, out Animator targetAnimator))
        {
            targetAnimator.SetTrigger(ChargingHash);
        }
    }

    public void TriggerAttack(int playerNumber, AttackKey type)
    {
        if (characterAnimators.TryGetValue(playerNumber, out Animator targetAnimator))
        {
            targetAnimator.SetInteger(attackTypeHash, (int)type);
            targetAnimator.SetTrigger(attackHash);
        }
    }

    public void TriggerHit(int playerNumber, int type)
    {
        if (characterAnimators.TryGetValue(playerNumber, out Animator targetAnimator))
        {
            targetAnimator.SetInteger(hitTypeHash, type);
            targetAnimator.SetTrigger(hitHash);
        }
    }
    
    public void TriggerWin(int playerNumber)
    {
        if (characterAnimators.TryGetValue(playerNumber, out Animator targetAnimator))
        {
            targetAnimator.SetTrigger(WinHash);
        }
    }

    public void TriggerLose(int playerNumber)
    {
        if (characterAnimators.TryGetValue(playerNumber, out Animator targetAnimator))
        {
            targetAnimator.SetTrigger(LoseHash);
        }
    }

    public void TriggerKOLose(int playerNumber)
    {
        if (characterAnimators.TryGetValue(playerNumber, out Animator targetAnimator))
        {
            targetAnimator.SetTrigger(KOLoseHash);
        }
    }
}
