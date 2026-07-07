using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = System.Random;
using Unity.VisualScripting;


public class Defender
{
    public List<AttackKey> pattern = new List<AttackKey>();
    private PolicyManager policy;
    private readonly JudgementHandler judge;
    private readonly Participant player;
    public int noteIndex { get; private set; }
    public float turnStartTime { get; private set; }    // 턴이 사작된 시간

    public Judgement lastJudgementResult { get; private set; }

    public Defender(PolicyManager p, Participant pl, JudgementDifficulty level)
    {
        policy = p;
        player = pl;
        noteIndex = 0;

        judge = new JudgementHandler();
        judge.Configure(level);
    }

    // 초기화 함수
    public void Init(List<AttackKey> attackPattern)
    {
        pattern.Clear();
        pattern = new List<AttackKey>(attackPattern);
        noteIndex = 0;
    }

    // 턴 시작 시간 전달 함수
    public void StartTurnTimer()
    {
        turnStartTime = Time.time;
    }

    // 방어 기록 함수
    public JudgementResult RegisterDefense(AttackKey inputKey)
    {
        if (pattern == null || noteIndex >= pattern.Count || player.IsDead()) return JudgementResult.None;

        long elapsedMs = (long)((Time.time - turnStartTime) * 1000);
        long target = policy.StartOffset + noteIndex * policy.NoteSpacing;
        long timeDiff = elapsedMs - target; // 시간 차이 계산

        if (Math.Abs(elapsedMs - target) <= policy.TimingWindow)
        {
            Judgement judgement;

            // 패턴이 None일 때 입력한 경우, NonePass 판정
            if (pattern[noteIndex] == AttackKey.None) judgement = Judgement.NonePass;
            // 패턴과 동일한 키를 일치한 경우, 시간에 따른 판정
            else if (inputKey == pattern[noteIndex]) judgement = judge.GetJudgement(timeDiff);
            // 패턴과 다른 키를 입력한 경우, Miss 판정
            else judgement = Judgement.Miss;

            TimingError error = TimingError.None;
            if (judgement == Judgement.Good || judgement == Judgement.Miss)
            {
                // Good 또는 Miss일 때만 Fast/Slow 판정
                error = (timeDiff < 0) ? TimingError.Fast : TimingError.Slow;
            }

            JudgementResult result = new JudgementResult(judgement, error);

            if (player != null) 
            {
                player.UpdateHp(result.judgement);
            }
            lastJudgementResult = result.judgement;
            noteIndex++;
            Debug.Log($"판정: {result} / 다음 노트: {noteIndex}");

            return result;
        }

        return JudgementResult.None;
    }

    // 플레이어가 아무것도 하지 않을 때의 Miss에 대한 판정 함수
    public JudgementResult UpdateMissCheck(long elapsedMs)
    {
        if (pattern == null || IsFinished()) return JudgementResult.None;

        long targetTime = policy.StartOffset + noteIndex * policy.NoteSpacing;

        // 만약 노트 패턴이 None일 때 안 눌렀을 경우, Pass 판정
        if (pattern[noteIndex] == AttackKey.None)
        {
            if (elapsedMs > targetTime)
            {
                
                if (player != null) 
                {
                    player.UpdateHp(Judgement.Pass);
                }
                lastJudgementResult = Judgement.Pass;
                noteIndex++;
                Debug.Log($"PASS! 다음 노트로 넘어갑니다: {noteIndex} 시간 경과: {elapsedMs}");
                return JudgementResult.Pass;
            }
        }

        // 목표 시간보다 너무 많이 지났다면 Miss 처리
        if (elapsedMs > targetTime + policy.TimingWindow)
        {
            if (player != null) 
            {
                player.UpdateHp(Judgement.Miss);
            }
            lastJudgementResult = Judgement.Miss;
            noteIndex++;
            Debug.Log($"시간 초과 MISS! 다음 노트로 넘어갑니다: {noteIndex} 시간 경과: {elapsedMs}");
            return new JudgementResult(Judgement.Miss, TimingError.Slow);
        }

        return JudgementResult.None;
    }



    // 라운드나 게임이 끝났는 지에 대한 여부
    public bool IsFinished()
    {
        // 패턴이 전달되지 않은 경우를 방지하기 위해 null을 받을 시 무조건 false
        if (pattern == null)
        {
            return false;
        }

        // 패턴이 끝났는 지 확인
        bool patternDone = noteIndex >= pattern.Count;

        // 서버 로직(player == null)이면, 패턴 완료 여부만 반환
        if (player == null)
        {
            return patternDone;
        }
        
        // 클라이언트 로직(player != null)이면, 패턴 또는 사망 여부 반환
        return patternDone || player.IsDead();
    }
}
