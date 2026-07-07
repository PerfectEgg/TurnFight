using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TutorialController : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image tutorialImage; // 튜토리얼 이미지를 표시할 Image 컴포넌트
    [SerializeField] private TMP_Text tutorialText; // 튜토리얼 대사를 표시할 TextMeshPro 컴포넌트
    [SerializeField] private Button previousButton; // "이전" 버튼
    [SerializeField] private Button nextButton; // "다음" 버튼
    

    [Header("튜토리얼 내용")]
    [Tooltip("여기에 튜토리얼 이미지들을 순서대로 등록하세요.")]
    [SerializeField] private List<Sprite> tutorialImages;

    [Tooltip("여기에 각 이미지에 맞는 튜토리얼 대사를 순서대로 등록하세요.")]
    [TextArea(3, 10)] // 인스펙터에서 여러 줄로 편하게 입력하도록 설정
    [SerializeField] private List<string> tutorialTexts;

    // 현재 보고 있는 페이지 인덱스
    private int currentPage = 0;

    // 이 패널이 활성화될 때 호출
    private void OnEnable()
    {
        // 튜토리얼을 항상 첫 페이지부터 보여줍니다.
        currentPage = 0;
        UpdatePageUI();
    }
    private void Start()
    {
        // 버튼에 클릭 리스너 연결
        nextButton.onClick.AddListener(OnClick_NextPage);
        previousButton.onClick.AddListener(OnClick_PreviousPage);
    }

    // 다음 버튼을 클릭했을 때 호출될 함수
    public void OnClick_NextPage()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);

        // 다음 페이지가 있는지 확인 (배열/리스트 범위 초과 방지)
        if (currentPage + 1 < tutorialImages.Count)
        {
            currentPage++;
            UpdatePageUI();
        }
    }

    // "이전" 버튼을 클릭했을 때 호출될 함수
    public void OnClick_PreviousPage()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);

        // 이전 페이지가 있는지 확인 (0 미만 방지)
        if (currentPage - 1 >= 0)
        {
            currentPage--;
            UpdatePageUI();
        }
    }

    // 실제 UI(이미지, 텍스트)를 업데이트하고 버튼 활성화/비활성화를 관리하는 함수
    private void UpdatePageUI()
    {
        // 이미지와 텍스트 교체
        if (tutorialImages.Count > currentPage && tutorialImages[currentPage] != null)
        {
            tutorialImage.sprite = tutorialImages[currentPage];
        }
        if (tutorialTexts.Count > currentPage)
        {
            tutorialText.text = tutorialTexts[currentPage];
        }

        // 버튼 활성화/비활성화
        // 첫 번째 페이지라면 이전 버튼 비활성화
        previousButton.gameObject.SetActive(currentPage > 0);
        
        // 마지막 페이지라면 다음 버튼 비활성화
        nextButton.gameObject.SetActive(currentPage < tutorialImages.Count - 1);
    }
}
