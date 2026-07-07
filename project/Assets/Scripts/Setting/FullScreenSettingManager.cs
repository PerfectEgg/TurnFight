using UnityEngine;

public class FullScreenSettingManager : MonoBehaviour
{
    void Update()
    {
        // F11 키를 누르면 전체 화면
        if (Input.GetKeyDown(KeyCode.F11))
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
    }

    void Awake()
    {
        DontDestroyOnLoad(this);
    }
}