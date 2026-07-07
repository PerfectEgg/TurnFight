using System;

public enum AttackKey { Left, Right, Up, Down, Space, None, Special = -1 }
public enum Judgement { Perfect, Good, Miss, Pass, NonePass, None }
public enum JudgementDifficulty { EasyJudgement, NormalJudgement, HardJudgement }

// 판정 결과를 담은 구조체
public struct JudgementResult
{
    public Judgement judgement;
    public TimingError timingError;

    // 생성자
    public JudgementResult(Judgement judge, TimingError error = TimingError.None)
    {
        this.judgement = judge;
        this.timingError = error;
    }

    // .None으로 편하게 쓰기 위한 static 변수
    public static JudgementResult None = new JudgementResult(Judgement.None, TimingError.None);
    public static JudgementResult NonePass = new JudgementResult(Judgement.NonePass, TimingError.None);
    public static JudgementResult Pass = new JudgementResult(Judgement.Pass, TimingError.None);
}

// 타이밍 나타내는 열거형
public enum TimingError
{
    None, // Perfect 또는 Fast/Slow 정보 없음
    Fast, // 빠르게 침
    Slow  // 느리게 침
}

class JudgementHandler
{
    private float perfectWindow { get; set; }       // 퍼펙트 판정
    private float goodWindow { get; set; }          // 굿 판정

    public void Configure(JudgementDifficulty level)
    {
        switch (level)
        {
            // 기본 판정
            case JudgementDifficulty.NormalJudgement:
                perfectWindow = 88;
                goodWindow = 121;
                break;
            // 하드 판정 (좀 더 어려운 판정으로 특수 상황 때 적용)
            case JudgementDifficulty.HardJudgement:
                perfectWindow = 66;
                goodWindow = 88;
                break;
        }
    }

    public Judgement GetJudgement(float offset)
    {
        offset = Math.Abs(offset);
        if (offset <= perfectWindow) return Judgement.Perfect;
        if (offset <= goodWindow) return Judgement.Good;
        return Judgement.Miss;
    }
}