using UnityEngine;
using System.Collections;

// 이 스크립트는 SpriteRenderer가 있어야만 작동합니다.
[RequireComponent(typeof(SpriteRenderer))]
public class VFXAnimator : MonoBehaviour
{
    // 인스펙터 창에서 이펙트의 애니메이션 방식을 선택할 수 있습니다.
    public enum AnimationType
    {
        ExpandAndDisappear, // 중앙에서 바깥으로 퍼지면서 나타났다가 한번에 사라짐 (히트, 가드용)
        ShrinkToCenter,     // 바깥에서 중앙으로 모이며 사라짐 (차징용)
        FadeInAndOut_LeftToRight, // 왼쪽에서 오른쪽으로 나타났다가 사라짐 (1P 스윙용)
        FadeInAndOut_RightToLeft  // 오른쪽에서 왼쪽으로 나타났다가 사라짐 (2P 스윙용)
    }

    [Header("애니메이션 설정")]
    public AnimationType animationType;
    [Tooltip("애니메이션이 재생될 총 시간(초)")]
    public float duration = 0.2f;
    public float Specialduration = 7.5f;
    [Tooltip("최종적으로 도달할 크기")]
    public Vector3 targetScale = Vector3.one;

    private SpriteRenderer spriteRenderer;
    private Color initialColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialColor = spriteRenderer.color;

        // 설정된 애니메이션 타입에 따라 적절한 코루틴 실행
        switch (animationType)
        {
            case AnimationType.ExpandAndDisappear:
                StartCoroutine(ExpandAndDisappearCoroutine());
                break;
            case AnimationType.ShrinkToCenter:
                StartCoroutine(ShrinkToCenterCoroutine());
                break;
            case AnimationType.FadeInAndOut_LeftToRight:
                // 1은 왼쪽 -> 오른쪽 방향
                StartCoroutine(FadeInAndOut_LeftToRight());
                break;
            case AnimationType.FadeInAndOut_RightToLeft:
                // -1은 오른쪽 -> 왼쪽 방향
                StartCoroutine(FadeInAndOut_RightToLeft());
                break;
        }
    }

    // 히트/가드용: 중앙에서 바깥으로 퍼지는 애니메이션
    private IEnumerator ExpandAndDisappearCoroutine()
    {
        transform.localScale = Vector3.zero; // 시작 크기는 0
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // 시간이 지남에 따라 크기를 0에서 targetScale까지 부드럽게 증가
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 애니메이션이 끝나면 즉시 사라집니다.
        Destroy(gameObject);
    }

    // 차징용: 바깥에서 중앙으로 모이는 애니메이션
    private IEnumerator ShrinkToCenterCoroutine()
    {
        float expandDuration = duration * 0.4f; // 전체 시간의 40%는 팽창
        float shrinkDuration = duration * 0.6f; // 전체 시간의 60%는 수축

        // --- 1단계: 주변에 에너지가 희미하게 팽창 ---
        float elapsedTime = 0f;
        transform.localScale = Vector3.zero;
        Color color = initialColor;

        while (elapsedTime < expandDuration)
        {
            float progress = elapsedTime / expandDuration;
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, progress);
            color.a = Mathf.Lerp(0f, 0.5f, progress); // 희미하게(50% 투명도) 나타남
            spriteRenderer.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // --- 2단계: 한 점으로 강하게 응축 ---
        elapsedTime = 0f;
        while (elapsedTime < shrinkDuration)
        {
            float progress = elapsedTime / shrinkDuration;
            // 크기는 작아지고
            transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, progress);
            // 투명도와 색상은 점점 밝고 선명해집니다 (하얀색으로).
            spriteRenderer.color = Color.Lerp(color, Color.white, progress);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // 스윙용: 수평 이동 페이드 인/아웃, 왼쪽에서 오른쪽
    private IEnumerator FadeInAndOut_LeftToRight()
    {
        if (spriteRenderer == null) { Destroy(gameObject); yield break; }

        float stepDuration = 0.1f; // 각 단계는 0.1초씩 진행
        Color color = spriteRenderer.color;

        // --- 1단계: 시작점에서 75% 지점까지 (0.1초, 페이드 인) ---
        float elapsedTime = 0f;
        while (elapsedTime < stepDuration)
        {
            float progress = elapsedTime / stepDuration;
            color.a = Mathf.Lerp(0, 1, progress); // 서서히 나타남
            spriteRenderer.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        color.a = 1;
        spriteRenderer.color = color;

        // --- 2단계: 75% 지점에서 끝점까지 (0.1초, 페이드 아웃) ---
        elapsedTime = 0f;
        while (elapsedTime < stepDuration)
        {
            float progress = elapsedTime / stepDuration;
            color.a = Mathf.Lerp(1, 0, progress); // 서서히 사라짐
            spriteRenderer.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
    
    // 스윙용: 수평 이동 페이드 인/아웃, 오른쪽에서 왼쪽
    private IEnumerator FadeInAndOut_RightToLeft()
    {
        if (spriteRenderer == null) { Destroy(gameObject); yield break; }
        
        float stepDuration = 0.1f;
        Color color = spriteRenderer.color;
        
        // 1단계: 페이드 인
        float elapsedTime = 0f;
        while (elapsedTime < stepDuration)
        {
            float progress = elapsedTime / stepDuration;
            color.a = Mathf.Lerp(0, 1, progress);
            spriteRenderer.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        color.a = 1;
        spriteRenderer.color = color;

        // 2단계: 페이드 아웃
        elapsedTime = 0f;
        while (elapsedTime < stepDuration)
        {
            float progress = elapsedTime / stepDuration;
            color.a = Mathf.Lerp(1, 0, progress);
            spriteRenderer.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}

