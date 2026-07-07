using UnityEngine;

public class EndGameUIController : MonoBehaviour
{
    public void OnRestartButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다");
        }
    }

    public void OnLobbyButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BackToLobby();
        }
        else
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다");
        }
    }
}
