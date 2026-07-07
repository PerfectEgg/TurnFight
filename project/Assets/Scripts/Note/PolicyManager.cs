using UnityEngine;

public class PolicyManager
{
    // 난이도에 따른 노트 간 간격, 노트 수 분류

    public enum Difficulty { Normal, Hard }

    public long NoteSpacing { get; private set; }       // 노트 간격 (ms)
    public long TimingWindow { get; private set; }      // 판정 허용 오차 (ms)
    public long PatternLength { get; private set; }     // 총 노트 수
    public long StartOffset { get; private set; }       // 첫 노트 입력 전까지 대기 시간 (ms)
    public long EndOffset { get; private set; }         // 한 패턴이 끝나고 마지막 지연 시간 (ms)

    public void Configure(Difficulty level)
    {
        switch (level)
        {
            case Difficulty.Normal:     // 보통 (8개 노트 처리, BPM 160)
                NoteSpacing = 250;
                TimingWindow = NoteSpacing / 2 + 50;
                PatternLength = 8;
                StartOffset = NoteSpacing * 2;
                EndOffset = NoteSpacing;
                break;
            case Difficulty.Hard:       // 어려움 (16개 노트 처리, BPM 180)
                NoteSpacing = 175;
                TimingWindow = NoteSpacing / 2 + 50;
                PatternLength = 12;
                StartOffset = NoteSpacing * 2;
                EndOffset = NoteSpacing;
                break;
        }
    }

    public long GetTurnEndTime() => StartOffset + (PatternLength - 1) * NoteSpacing + EndOffset;


}