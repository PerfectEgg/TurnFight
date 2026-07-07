using System.Collections.Generic;
using System;
using UnityEngine;
using Random = System.Random;

public class Attacker_CPU : MonoBehaviour
{
    public List<AttackKey> attackPattern { get; private set; }
    private Random rd;
    private bool changeEvent = false;
    public int noteIndex { get; private set; }

    public Attacker_CPU()
    {
        attackPattern = new List<AttackKey>();
        rd = new Random();
        noteIndex = 0;
    }

    public void Init()
    {
        attackPattern.Clear();
        noteIndex = 0;
    }
    
    public void setChangeEvent() => changeEvent = false;

    // 라이트 난이도의 CPU 공격 패턴 생성 (제한된 공격 키 등장)
    public void AddEazyRandomNote()
    {
        var key = rd.Next(0, 3) switch
        {
            0 => AttackKey.Left,
            1 => AttackKey.Right,
            _ => AttackKey.Space,
        };

        if (changeEvent)
        {
            key = AttackKey.None;
        }

        changeEvent = !changeEvent;

        attackPattern.Add(key);
        noteIndex++;

        AudioManager.Instance.PlaySFX(AudioManager.Instance.noteSound);
    }

    // 미들 난이도의 CPU 공격 패턴 생성 (모든 공격 키 등장)
    public void AddNormalRandomNote()
    {
        AttackKey key = rd.Next(100) < 5 ? AttackKey.None : (AttackKey)rd.Next(0, 5);

        attackPattern.Add(key);
        noteIndex++;

        AudioManager.Instance.PlaySFX(AudioManager.Instance.noteSound);
    }

    // 헤비 난이도의 CPU 공격 패턴 생성 (모든 공격 키 + 10% 확률로 None도 포함함)
    public void AddHardRandomNote()
    {

        AttackKey key = rd.Next(100) < 10 ? AttackKey.None : (AttackKey)rd.Next(0, 5);

        attackPattern.Add(key);
        noteIndex++;
        
        if (key != AttackKey.None)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.noteSound);
    }
}
