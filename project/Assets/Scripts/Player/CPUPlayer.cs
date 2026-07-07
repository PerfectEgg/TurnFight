using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class CpuPlayer : Participant
{
    private Attacker_CPU cpuAttacker; // CPU 로직을 담을 객체
    private PolicyManager policy;
    private GameDifficulty gameDifficulty;

    public void Setup(PolicyManager policy, GameDifficulty gameDifficulty)
    {
        this.policy = policy;
        this.gameDifficulty = gameDifficulty;
        this.cpuAttacker = new Attacker_CPU();
    }

    public override IEnumerator TakeAttackTurn(Action<List<AttackKey>> onPatternGenerated, RhythmUIController uiController)
    {
        cpuAttacker.Init();

        float turnStartTime = Time.time;
        long totalTurnTimeMs = policy.GetTurnEndTime();
        float totalTurnDuration = totalTurnTimeMs / 1000f;
        float turnEndTime = turnStartTime + totalTurnDuration;
        cpuAttacker.setChangeEvent();

        while (Time.time < turnEndTime)
        {
            float elapsedTime = Time.time - turnStartTime;
            long currentMs = (long)(elapsedTime * 1000); // ms가 필요할 때만 변환해서 사용

            uiController.UpdateTimer(elapsedTime / totalTurnDuration);


            if (currentMs >= policy.StartOffset + cpuAttacker.noteIndex * policy.NoteSpacing &&
                cpuAttacker.noteIndex < policy.PatternLength)
            {
                switch (gameDifficulty)
                {
                    case GameDifficulty.Eazy:
                        cpuAttacker.AddEazyRandomNote();
                        break;
                    case GameDifficulty.Normal:
                        cpuAttacker.AddNormalRandomNote();
                        break;
                    case GameDifficulty.Hard:
                        cpuAttacker.AddHardRandomNote();
                        break;
                }
                uiController.UpdateDynamicPattern(cpuAttacker.attackPattern, policy);
            }
            yield return null;
        }

        uiController.HighlightCurrentNote(-1);


        Debug.Log("CPU 패턴 생성 완료! 패턴 : " + string.Join(", ", cpuAttacker.attackPattern));

        onPatternGenerated?.Invoke(cpuAttacker.attackPattern);
    }

    public override IEnumerator TakeDefendTurn(List<AttackKey> pattern, RhythmUIController rhythmUIController)
    {
        // CPU는 방어 로직이 없기에 오버라이딩만 진행
        yield break;
    }

    public override IEnumerator TakeSpecialAttackTurn()
    {
        // CPU는 스페셜 공격 로직이 없기에 오버라이딩만 진행
        yield break;
    }
}
