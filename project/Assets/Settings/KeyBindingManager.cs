using System.Collections.Generic;
using UnityEngine;

public class KeyBindingManager : MonoBehaviour {
    public static KeyBindingManager Instance;

    // P1, P2의 모든 키 설정을 저장하는 '사전(Dictionary)'
    private Dictionary<string, KeyCode> player1Keys;
    private Dictionary<string, KeyCode> player2Keys;

    private Dictionary<int, Dictionary<AttackKey, KeyCode>> playerBindings = new Dictionary<int, Dictionary<AttackKey, KeyCode>>();

    private void Awake() {
        // 싱글톤 세팅
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 넘어가도 유지됨

        InitDefaults();
    }

    private void InitDefaults() {
        // 기본 키 세팅 (원하는 대로 변경 가능)
        playerBindings[1] = new Dictionary<AttackKey, KeyCode>() {
            { AttackKey.Left, KeyCode.LeftArrow },
            { AttackKey.Right, KeyCode.RightArrow },
            { AttackKey.Up, KeyCode.UpArrow },
            { AttackKey.Down, KeyCode.DownArrow },
            { AttackKey.Space, KeyCode.Space }
        };

        playerBindings[2] = new Dictionary<AttackKey, KeyCode>() {
            { AttackKey.Left, KeyCode.Keypad4 },
            { AttackKey.Right, KeyCode.Keypad6 },
            { AttackKey.Up, KeyCode.Keypad8 },
            { AttackKey.Down, KeyCode.Keypad5 },
            { AttackKey.Space, KeyCode.A }
        };
    }

    // 특정 키 가져오기
    public KeyCode GetKey(int playerId, AttackKey action) {
        if (playerBindings.ContainsKey(playerId) && playerBindings[playerId].ContainsKey(action)) {
            return playerBindings[playerId][action];
        }
        return KeyCode.None;
    }

    // 키 변경 (설정 메뉴 등에서 호출)
    public void RebindKey(int playerId, AttackKey action, KeyCode newKey) {
        if (!playerBindings.ContainsKey(playerId)) {
            playerBindings[playerId] = new Dictionary<AttackKey, KeyCode>();
        }
        playerBindings[playerId][action] = newKey;
    }

    // 입력 체크
    public bool GetKeyDown(int playerId, AttackKey action) {
        return Input.GetKeyDown(GetKey(playerId, action));
    }
}
