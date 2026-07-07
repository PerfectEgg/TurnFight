using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;

public class RhythmUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject patternPanel;
    [SerializeField] private Slider timerSlider;

    [Header("Judgement UI")]
    [SerializeField] private GameObject judgementPrefab; // 텍스트와 애니메이션이 포함된 프리팹
    [SerializeField] private TMP_ColorGradient rainbowGradientPreset; // 무지개 색
    [SerializeField] private TMP_ColorGradient goldenGradientPreset; // 골드 색

    private readonly Color colorDarkGray = new Color(0.2f, 0.2f, 0.2f);
    private readonly Color colorWhiteOutline = Color.white;
    private readonly Color colorSilver = new Color(0.75f, 0.75f, 0.75f);

    [Header("Prefabs & Sprites")]
    [SerializeField] private GameObject noteIconPrefab;
    [SerializeField] private Sprite leftKeySprite, rightKeySprite, upKeySprite, downKeySprite, spaceKeySprite;

    [Header("Round UI")]
    [SerializeField] private TextMeshProUGUI roundText;

    [Header("Announcer UI")]
    [SerializeField] private TextMeshProUGUI announcerText;

    [Header("Markers")]
    [SerializeField] private RectTransform markerParent;
    [SerializeField] private GameObject noteMarkerPrefab;

    // 배열 사용하여 슬롯 기반 아이콘 관리
    private GameObject[] spawnedNoteIcons;
    private List<GameObject> spawnedMarkers = new List<GameObject>();
    private List<GameObject> judgementPool = new List<GameObject>();

    // 각 판정 오브젝트의 반납 코루틴을 저장할 리스트
    private List<Coroutine> judgementCoroutines = new List<Coroutine>();

    // 멀티 플레이용 코루틴 제어 변수
    private Coroutine _currentAnnouncementCoroutine;

    // 턴마다 노트 아이콘만 지우는 함수
    public void ClearTurnUI()
    {
        if (spawnedNoteIcons != null)
        {
            foreach (var icon in spawnedNoteIcons)
            {
                if (icon != null) Destroy(icon);
            }
        }
        spawnedNoteIcons = null; // 다음 턴을 위해 배열을 비웁니다.
    }

    // 마커만 따로 지우는 함수
    public void ClearMarkers()
    {
        foreach (var marker in spawnedMarkers) { Destroy(marker); }
        spawnedMarkers.Clear();
    }

    // 판정 UI 띄우기 함수
    public void ShowJudgement(int noteIndex, JudgementResult result)
    {
        if (this == null || this.gameObject == null) return;

        Judgement judgement = result.judgement;
        TimingError timingError = result.timingError;

        if (judgement == Judgement.None) return;

        // 풀에서 noteIndex에 해당하는 판정 오브젝트 찾기
        if (judgementPool == null || noteIndex < 0 || noteIndex >= judgementPool.Count)
        {
            Debug.LogError($"Judgement Pool이 준비되지 않았거나, noteIndex({noteIndex})가 범위를 벗어났습니다.");
            return;
        }

        GameObject judgementObject = judgementPool[noteIndex];

        Debug.Log($"ShowJudgement: {noteIndex}");

        // 텍스트 설정
        Transform judgementTransform = judgementObject.transform.Find("judgementText");

        TMP_Text judgementComponent = null;

        if (judgementTransform != null)
        {
            judgementComponent = judgementTransform.GetComponent<TMP_Text>(); // 프리팹 자식에서 텍스트 찾기

            RectTransform judgementRect = judgementTransform.GetComponent<RectTransform>();
            if (judgementRect != null)
            {
                judgementRect.anchoredPosition = new Vector2(0, 80f);
            }
        }
            
        if (judgementComponent != null)
        {
            switch (judgement)
            {
                case Judgement.Perfect:
                    judgementComponent.text = "Perfect";
                    judgementComponent.enableVertexGradient = true; // 그라데이션 적용
                    judgementComponent.colorGradientPreset = rainbowGradientPreset;
                    judgementComponent.outlineWidth = 0; // 그라데이션은 테두리 없음
                    break;
                case Judgement.Good:
                    judgementComponent.text = "Good";
                    judgementComponent.enableVertexGradient = true; // 그라데이션 적용
                    judgementComponent.colorGradientPreset = goldenGradientPreset;

                    judgementComponent.outlineWidth = 0; // 그라데이션은 테두리 없음
                    break;
                case Judgement.Miss:
                    judgementComponent.text = "Miss";
                    judgementComponent.enableVertexGradient = false; // 그라데이션 비활성화
                    judgementComponent.color = colorDarkGray;

                    // 테두리 추가
                    judgementComponent.outlineWidth = 0.1f;
                    judgementComponent.outlineColor = colorWhiteOutline;
                    break;
                case Judgement.Pass:
                    judgementComponent.text = "Pass";
                    judgementComponent.enableVertexGradient = false; // 그라데이션 비활성화
                    judgementComponent.color = colorSilver;

                    // 테두리 추가
                    judgementComponent.outlineWidth = 0.1f;
                    judgementComponent.outlineColor = colorWhiteOutline;
                    break;
                case Judgement.NonePass:
                    judgementComponent.text = "Bad";
                    judgementComponent.enableVertexGradient = false; // 그라데이션 비활성화
                    judgementComponent.color = colorDarkGray;

                    // 테두리 추가
                    judgementComponent.outlineWidth = 0.1f;
                    judgementComponent.outlineColor = colorWhiteOutline;
                    break;
            }
        }

        Transform timingTransform = judgementObject.transform.Find("timingText");
        TMP_Text timingTextComponent = null;

        if (timingTransform != null)
        {
            timingTextComponent = timingTransform.GetComponent<TMP_Text>();

            RectTransform timingRect = timingTransform.GetComponent<RectTransform>();
            if (timingRect != null)
            {
                timingRect.anchoredPosition = new Vector2(0, -80f);
            }
        }

        // 설정에서 토글로 On/Off 가능
        bool showFastSlow = SettingsManager.Instance.ShowInputTiming;

        if (timingTextComponent != null && showFastSlow)
        {
            // Good 또는 Miss일 때만 Fast/Slow 표시
            if (judgement == Judgement.Good || judgement == Judgement.Miss)
            {
                if (timingError == TimingError.Fast)
                {
                    timingTextComponent.text = "Fast";
                    timingTextComponent.color = Color.softRed;
                    timingTextComponent.gameObject.SetActive(true);
                }
                else if (timingError == TimingError.Slow)
                {
                    timingTextComponent.text = "Slow";
                    timingTextComponent.color = Color.skyBlue; // (색상은 예시)
                    timingTextComponent.gameObject.SetActive(true);
                }
                else // TimingError.None
                {
                    timingTextComponent.gameObject.SetActive(false);
                }
            }
            else // Perfect, Pass 등
            {
                timingTextComponent.gameObject.SetActive(false);
            }
        }
        else if (timingTextComponent != null)
        {
            timingTextComponent.gameObject.SetActive(false); // (설정에서 끈 경우)
        }

        // 활성화 및 애니메이션 재생
        judgementObject.SetActive(true); // "켠다"
        Animator animator = judgementObject.GetComponent<Animator>();
        if (animator != null)
        {
            // 애니메이션을 처음부터 다시 재생
            animator.Play("Judgement_Show", -1, 0f);
        }

        // 아직 실행 중이었다면, 그 이전 코루틴을 즉시 중지
        if (judgementCoroutines[noteIndex] != null)
        {
            StopCoroutine(judgementCoroutines[noteIndex]);
        }

        // 일정 시간 후 풀로 자동 반납 후 코루틴 리스트에 저장
        judgementCoroutines[noteIndex] = StartCoroutine(ReturnToPoolAfterDelay(judgementObject, noteIndex, 0.5f)); // 0.5초 뒤 종료
    }
    
    private IEnumerator ReturnToPoolAfterDelay(GameObject obj, int noteIndex, float delay)
    {
        yield return new WaitForSeconds(delay); // 애니메이션 재생 시간

        obj.SetActive(false); // 풀에 반납하는 효과
        
        // 작업이 끝났으므로 코루틴 리스트에서 제거
        if (noteIndex >= 0 && noteIndex < judgementCoroutines.Count)
        {
            judgementCoroutines[noteIndex] = null;
        }
    }

    // 슬롯 기반으로 패턴 UI를 생성하는 함수
    public void DisplayPattern(List<AttackKey> pattern, PolicyManager policy)
    {
        ClearTurnUI(); // 함수 시작 시 이전 노드 클리어
        spawnedNoteIcons = new GameObject[(int)policy.PatternLength]; // 전체 패턴 길이에 맞는 빈 슬롯(배열) 생성

        long totalTurnTime = policy.GetTurnEndTime();
        if (totalTurnTime <= 0) return;

        for (int i = 0; i < pattern.Count; i++)
        {
            if (pattern[i] == AttackKey.None) continue;

            long targetTime = policy.StartOffset + i * policy.NoteSpacing;
            float normalizedPosition = (float)targetTime / totalTurnTime;

            GameObject newIcon = Instantiate(noteIconPrefab, patternPanel.transform);
            RectTransform iconRect = newIcon.GetComponent<RectTransform>(); // iconRect 정의 추가
            iconRect.GetComponent<Image>().sprite = GetSpriteForKey(pattern[i]);
            
            iconRect.anchorMin = new Vector2(normalizedPosition, 0.5f);
            iconRect.anchorMax = new Vector2(normalizedPosition, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;

            spawnedNoteIcons[i] = newIcon; // 'i'번째 슬롯에 생성된 아이콘을 저장
        }
    }

    // 공격 턴 동안 실시간으로 추가되는 노트만 그려주는 함수 (슬롯 기반으로 수정)
    public void UpdateDynamicPattern(List<AttackKey> currentPattern, PolicyManager policy)
    {
        // 아직 배열이 준비되지 않았다면 초기화
        if (spawnedNoteIcons == null)
        {
            spawnedNoteIcons = new GameObject[(int)policy.PatternLength];
        }

        int lastNoteIndex = currentPattern.Count - 1;
        if (lastNoteIndex < 0) return;

        // 마지막으로 추가된 노트 슬롯이 비어있을 때만 아이콘 생성
        if (spawnedNoteIcons[lastNoteIndex] == null)
        {
            AttackKey newKey = currentPattern[lastNoteIndex];
            if (newKey == AttackKey.None) return;

            long totalTurnTime = policy.GetTurnEndTime();
            if (totalTurnTime <= 0) return;

            long targetTime = policy.StartOffset + lastNoteIndex * policy.NoteSpacing;
            float normalizedPosition = (float)targetTime / totalTurnTime;

            GameObject newIcon = Instantiate(noteIconPrefab, patternPanel.transform);
            RectTransform iconRect = newIcon.GetComponent<RectTransform>();
            iconRect.GetComponent<Image>().sprite = GetSpriteForKey(newKey);
            
            iconRect.anchorMin = new Vector2(normalizedPosition, 0.5f);
            iconRect.anchorMax = new Vector2(normalizedPosition, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            
            spawnedNoteIcons[lastNoteIndex] = newIcon; // 해당 슬롯에 아이콘 저장
        }
    }

    // 슬롯 기반으로 하이라이트를 처리하는 함수
    public void HighlightCurrentNote(int noteIndex)
    {
        if (spawnedNoteIcons == null) return;

        // 배열의 전체 길이를 기준으로 순회
        for (int i = 0; i < spawnedNoteIcons.Length; i++)
        {
            GameObject icon = spawnedNoteIcons[i];
            if (icon != null) // 해당 슬롯에 아이콘이 존재할 때만 색상 변경
            {
                icon.GetComponent<Image>().color = (i == noteIndex) ? Color.white : Color.gray;
            }
        }
    }

    // 타이머 UI 업데이트 함수
    public void UpdateTimer(float normalizedTime)
    {
        timerSlider.value = Mathf.Clamp01(normalizedTime);
    }
    
    // 마커 생성 함수
    public void DisplayNoteMarkers(PolicyManager policy)
    {
        ClearMarkers();

        if (judgementPool != null)
        {
            // 기본 판정 UI 풀이 존재할 경우 제거
            foreach (var judgement in judgementPool)
            {
                if (judgement != null)
                {
                    Debug.Log($"Destroy: {judgement.name}");
                    Destroy(judgement);
                }
            }
        }

        long totalTurnTime = policy.GetTurnEndTime();
        if (totalTurnTime <= 0) return;

        int noteCount = (int)policy.PatternLength;

        for (int i = 0; i < noteCount; i++)
        {
            long targetTime = policy.StartOffset + i * policy.NoteSpacing;
            float normalizedPosition = (float)targetTime / totalTurnTime;

            // 마커 UI 생성
            GameObject newMarker = Instantiate(noteMarkerPrefab, markerParent);
            RectTransform markerRect = newMarker.GetComponent<RectTransform>();

            markerRect.anchorMin = new Vector2(normalizedPosition, 0);
            markerRect.anchorMax = new Vector2(normalizedPosition, 1);
            markerRect.anchoredPosition = Vector2.zero;

            spawnedMarkers.Add(newMarker);
            
            // 판정 UI 생성
            GameObject newJudgement = Instantiate(judgementPrefab, markerParent);
            RectTransform judgementRect = newJudgement.GetComponent<RectTransform>();

            judgementRect.anchorMin = new Vector2(normalizedPosition, 0.5f);
            judgementRect.anchorMax = new Vector2(normalizedPosition, 0.5f);
            judgementRect.anchoredPosition = new Vector2(0, 0);

            // 비활성화
            newJudgement.SetActive(false);

            // 판정 UI 풀에 추가
            judgementPool.Add(newJudgement);

            judgementCoroutines.Add(null);
        }
    }

    // 게임 컷신 재생 함수
    public IEnumerator ShowAnnouncement(string message, float displayDuration)
    {
        if (announcerText == null) yield break;

        announcerText.text = message;

        Color originalColor = announcerText.color;
        // 알파 값을 1로 설정하여 완전히 불투명하게 만들기
        announcerText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1);
        announcerText.gameObject.SetActive(true);

        float fadeOutTime = 0.1f; // 사라지는 데 걸리는 시간
        // 전체 표시 시간에서 페이드 아웃 시간을 뺀 만큼 기다립니다.
        float holdTime = displayDuration - fadeOutTime;
        if (holdTime > 0)
        {
            yield return new WaitForSeconds(holdTime);
        }

        float timer = 0f;
        while (timer < fadeOutTime)
        {
            // 시간에 따라 알파(투명도) 값을 1에서 0으로 점차 감소시킵니다.
            float alpha = Mathf.Lerp(1, 0, timer / fadeOutTime);
            announcerText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        announcerText.gameObject.SetActive(false);
        announcerText.color = originalColor;
    }

    public void TriggerAnnouncement(string message, bool show)
    {
        // 1. 이전에 실행 중이던 코루틴이 있다면 즉시 중지
        if (_currentAnnouncementCoroutine != null)
        {
            StopCoroutine(_currentAnnouncementCoroutine);
            _currentAnnouncementCoroutine = null;
        }

        // 2. 새 명령에 따라 새 코루틴 시작
        if (show)
        {
            ShowAnnouncementOn(message);
        }
        else
        {
            _currentAnnouncementCoroutine = StartCoroutine(ShowAnnouncementOff());
        }
    }

    // 멀티 플레이용 On/Off 게임 컷신 재생 함수 (On)
    public void ShowAnnouncementOn(string message)
    {
        if (announcerText == null) return;

        announcerText.text = message;

        Color originalColor = announcerText.color;
        // 알파 값을 1로 설정하여 완전히 불투명하게 만들기
        announcerText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1);
        announcerText.gameObject.SetActive(true);
    }

    // 멀티 플레이용 On/Off 게임 컷신 재생 함수 (Off)
    public IEnumerator ShowAnnouncementOff()
    {
        if (announcerText == null) yield break;

        Color originalColor = announcerText.color;

        float fadeOutTime = 0.1f; // 사라지는 데 걸리는 시간
        float timer = 0f;

        while (timer < fadeOutTime)
        {
            // 시간에 따라 알파(투명도) 값을 1에서 0으로 점차 감소시킵니다.
            float alpha = Mathf.Lerp(1, 0, timer / fadeOutTime);
            announcerText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

        announcerText.gameObject.SetActive(false);
        announcerText.color = originalColor;

        // 코루틴이 끝났으니 제어 변수 비우기
        _currentAnnouncementCoroutine = null;
    }

    public void UpdateRoundText(string message)
    {
        if (roundText != null) roundText.text = message;
    }

    private Sprite GetSpriteForKey(AttackKey key)
    {
        switch (key)
        {
            case AttackKey.Left: return leftKeySprite;
            case AttackKey.Right: return rightKeySprite;
            case AttackKey.Up: return upKeySprite;
            case AttackKey.Down: return downKeySprite;
            case AttackKey.Space: return spaceKeySprite;
            default: return null;
        }
    }
}

