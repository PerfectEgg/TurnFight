using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Attacker {
    public List<AttackKey> attackPattern { get; private set; }
    private PolicyManager policy;
    public int noteIndex { get; private set; }
    public float turnStartTime { get; private set; }    // 턴이 사작된 시간

    public Attacker(PolicyManager policy)
    {
        this.policy = policy;
        attackPattern = new List<AttackKey>();
        noteIndex = 0;
    }

    public void Init() {
        attackPattern.Clear();
        noteIndex = 0;
    }

    // 턴 시작 시간 전달 함수
    public void StartTurnTimer()
    {
        turnStartTime = Time.time;
    }

    // 공격 키를 입력
    public void AddNote(AttackKey key)
    {
        if (noteIndex >= policy.PatternLength) return;

        long elapsedMs = (long)((Time.time - turnStartTime) * 1000);
        long target = policy.StartOffset + noteIndex * policy.NoteSpacing;

        if (Math.Abs(elapsedMs - target) <= policy.NoteSpacing / 2)
        {
            if (attackPattern.Count <= noteIndex)
            {
                attackPattern.Add(key);
                Debug.Log($"공격 패턴 [{key}]을 입력.");

                AudioManager.Instance.PlaySFX(AudioManager.Instance.noteSound);
            }
        }
    }

    // 제한 시간 내에 입력하지 않은 경우 None으로 채움.
    public void UpdateNoneNote()
    {
        if (noteIndex >= policy.PatternLength) return;

        long elapsedMs = (long)((Time.time - turnStartTime) * 1000);
        long timeLimit = policy.StartOffset + noteIndex * policy.NoteSpacing + (policy.NoteSpacing / 2);

        if (elapsedMs > timeLimit)
        {
            // 현재 노트가 아직 기록되지 않았다면 'None'으로 채움
            if (attackPattern.Count <= noteIndex)
            {
                attackPattern.Add(AttackKey.None);
                Debug.Log($"공격 패턴 [{noteIndex}]을 None으로 처리.");
            }
            noteIndex++; // 다음 노트
        }
    }

    // 비어 있는 모든 패턴을 None으로 채우는 안전장치 함수
    public void FillRemainingNotesAsNone()
    {
        while (attackPattern.Count < policy.PatternLength)
        {
            attackPattern.Add(AttackKey.None);
        }
    }

}
