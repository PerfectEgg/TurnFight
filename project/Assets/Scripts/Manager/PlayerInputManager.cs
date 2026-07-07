using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerInputManager : MonoBehaviour {

    // 싱글톤 패턴으로 게임 내 어디서든 인스턴스 쉽게 접근 가능
    public static PlayerInputManager Instance { get; private set; }

    // AttackKey를 사용할 이벤트 선언
    public event Action<int, AttackKey> OnPlayerInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // 등록된 플레이어 ID 목록을 순회
        
        // 로컬 2인 플레이 환경에서는 이 방식이 더 간단하고 확실합니다.
        for (int playerId = 1; playerId <= 2; playerId++)
        {
            // 모든 AttackKey 종류를 순회합니다.
            foreach (AttackKey attackKey in Enum.GetValues(typeof(AttackKey)))
            {
                if (attackKey == AttackKey.None) continue;

                // KeyBindingManager에게 "playerId번 플레이어의 attackKey에 해당하는 키가 '지금' 눌렸니?" 라고 물어봅니다.
                if (KeyBindingManager.Instance.GetKeyDown(playerId, attackKey))
                {
                    // 눌렸다면, 플레이어 ID와 AttackKey를 방송합니다.
                    OnPlayerInput?.Invoke(playerId, attackKey);
                }
            }
        }
    }
}