using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;

public abstract class Participant : NetworkBehaviour
{
    // 네트워크릴 위현 변수
    public NetworkVariable<FixedString32Bytes> participantName = 
        new NetworkVariable<FixedString32Bytes>(
            default, // 기본값
            NetworkVariableReadPermission.Everyone, // 모두 읽기 가능
            NetworkVariableWritePermission.Server // 서버(호스트)만 쓰기 가능
        );

    public NetworkVariable<int> hp = new NetworkVariable<int>(
            default, // 초기값 (Init에서 설정하는 것이 더 좋습니다)
            NetworkVariableReadPermission.Everyone, // 모두 읽기 가능
            NetworkVariableWritePermission.Server // 서버(호스트)만 쓰기 가능
        );

    protected bool isDeath;

    public HPBarController hpBar;
    public GameObject characterVisual;

    public const int NonePassDamage = 5;
    public const int GoodDamage = 5;
    public const int MissDamage = 15;

    public bool PerfectRound { get; protected set; }


    public virtual void Init()
    {
        if (IsHost)
            hp.Value = 100;

        hpBar.UpdateHp(hp.Value);
        isDeath = false;
    }
    
    public virtual void setCharacterVisual(GameObject characterVisual)
    {
        this.characterVisual = characterVisual;
    }

    public virtual void UpdateHp(Judgement jg)
    {
        switch (jg)
        {
            case Judgement.Perfect: break;
            case Judgement.Pass: break;
            case Judgement.Good:
                ApplyDamage(GoodDamage);
                break;
            case Judgement.Miss:
                ApplyDamage(MissDamage);
                break;
            case Judgement.NonePass:
                ApplyNonePassDamage();
                break;
        }
    }

    public virtual void UpdateHpByAttack(int SpecialAttackDamage)
    {
        ApplyDamage(SpecialAttackDamage);
    }

    public virtual void ApplyDamage(int damage)
    {
        if (!IsServer) return;

        hp.Value -= damage;
        if (hp.Value <= 0)
        {
            hp.Value = 0;
            isDeath = true;
        }

        if (hpBar != null)
        {
            hpBar.UpdateHp(hp.Value);
        }
    }

    public virtual void ApplyNonePassDamage()
    {
        if (!IsServer) return;

        hp.Value -= NonePassDamage;
        if (hp.Value <= 0)
        {
            hp.Value = 1;
        }

        if (hpBar != null)
        {
            hpBar.UpdateHp(hp.Value);
        }
    }

    [ClientRpc]
    protected void UpdateHpBarClientRpc(int hp)
    {
        if (hpBar != null)
        {
            hpBar.UpdateHp(hp);
        }
    }

    public bool IsDead() => isDeath;

    public virtual void ApplyPerfectRoundBonus() { }
    public virtual void ResetPerfectRoundStatus() { }

    public abstract IEnumerator TakeAttackTurn(Action<List<AttackKey>> onPatternGenerated, RhythmUIController uiController);

    public abstract IEnumerator TakeDefendTurn(List<AttackKey> pattern, RhythmUIController rhythmUIController);
    public abstract IEnumerator TakeSpecialAttackTurn();

    public virtual IEnumerator PlayKnockoutAnimation(Vector2 direction)
    {
        float duration = 3.0f;      // 날아가는 데 걸리는 시간
        float rotationSpeed = 5f; // 회전 속도
        float knockbackSpeed = 3.5f; // 날아가는 속도

        float elapsedTime = 0f;


        while (elapsedTime < duration)
        {

            // 지정된 방향으로 캐릭터를 이동
            characterVisual.transform.position += (Vector3)direction.normalized * knockbackSpeed * Time.deltaTime;

            // 캐릭터에 회전을 추가해 확실히 떨어지는 느낌을 줌
            characterVisual.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime * -direction.x);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
