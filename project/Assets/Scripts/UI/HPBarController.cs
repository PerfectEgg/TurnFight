using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HPBarController : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI nameText;

    [SerializeField] private GameObject fillGameObject;

    [SerializeField] private TextMeshProUGUI RoleText;
    
    private Coroutine hpUpdateCoroutine; // 현재 실행 중인 애니메이션 코루틴

    public void Init(int maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = maxHp;
        }
        if (fillGameObject != null)
        {
            fillGameObject.SetActive(true);
        }
    }

    public void UpdateHp(int currentHp)
    {
        // 이전에 실행 중인 HP 업데이트 애니메이션이 있다면 중지
        if (hpUpdateCoroutine != null)
        {
            StopCoroutine(hpUpdateCoroutine);
        }
        
        // 새로운 HP 값으로 부드럽게 변경하는 애니메이션 코루틴 시작
        hpUpdateCoroutine = StartCoroutine(AnimateHpChange(currentHp));
    }

    private IEnumerator AnimateHpChange(int targetHp)
    {
        // HP가 0 이하로 떨어지면 Fill 이미지를 즉시 숨김.
        if (fillGameObject != null)
        {
            fillGameObject.SetActive(targetHp > 0);
        }

        float currentSliderValue = hpSlider.value;
        float timer = 0f;
        float duration = 0.5f; // 0.5초 동안 애니메이션

        // 정해진 시간 동안 현재 슬라이더 값에서 목표 HP까지 부드럽게 이동
        while (timer < duration)
        {
            timer += Time.deltaTime;
            // Lerp 함수를 사용하여 두 값 사이를 자연스럽게 간격 줄이기
            hpSlider.value = Mathf.Lerp(currentSliderValue, targetHp, timer / duration);
            yield return null; // 다음 프레임까지 대기
        }

        // 애니메이션이 끝난 후, 값을 목표 HP로 정확하게 맞춤
        hpSlider.value = targetHp;
    }

    public void UpdateName(string name)
    {
        if (nameText != null) nameText.text = name;
    }

    public void UpdateRole(string message)
    {
        if (RoleText != null) RoleText.text = message;
    }
}
