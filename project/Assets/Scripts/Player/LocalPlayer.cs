using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Random = System.Random;
using NUnit.Framework;

public class LocalPlayer : Participant
{
    [Header("Player Settings")]
    public int playerId; // Inspector에서 p1, p2로 설정

    private int oppPlayerId;

    private Attacker attacker; // 공격 로직을 담을 객체
    private Defender defender; // 수비 로직을 담을 객체
    private PolicyManager policy;
    private GameDifficulty gameDifficulty;
    private JudgementDifficulty judgementDifficulty;

    // 멀티플레이 용 내 턴인지 확인하는 플래그 변수
    bool isMyAttackTurn = false;
    bool isMyDefendTurn = false;

    private Random random;

    void Update()
    {
        // 내 차례일 때 코드를 실행
        if (!IsOwner) return;

        // 멀티 플레이가 아니면 코드를 실행하지 않음
        if (GameManager.Instance.currentGameState.Value != GameState.Multi) return;

        // 공격 턴일 때
        if (isMyAttackTurn)
        {
            attacker.UpdateNoneNote();
        }
        // 수비 턴일 때
        else if (isMyDefendTurn)
        {
            long elapsedMs = (long)((Time.time - defender.turnStartTime) * 1000);
            JudgementResult autoMissResult = defender.UpdateMissCheck(elapsedMs); 
        }
    }

    public void Setup(PolicyManager policy, GameDifficulty gameDifficulty, JudgementDifficulty judgementDifficulty)
    {
        this.policy = policy;
        this.gameDifficulty = gameDifficulty;
        this.judgementDifficulty = judgementDifficulty;
        this.attacker = new Attacker(policy);
        this.defender = new Defender(policy, this, judgementDifficulty);

        random = new Random();
    }

    public override void Init()
    {
        base.Init();
        PerfectRound = true;
    }

    public override void ApplyDamage(int damage)
    {
        PerfectRound = false;
        base.ApplyDamage(damage);
    }


    public override void ApplyNonePassDamage()
    {
        // 서버가 아니면 실행하지 않음
        if (!IsServer) return;

        PerfectRound = false;
        base.ApplyNonePassDamage();
    }

    public override void ApplyPerfectRoundBonus()
    {
        // 서버가 아니면 실행하지 않음
        if (!IsServer) return;

        hp.Value += 10;
        if (hp.Value > 100)
            hp.Value = 100;

        if (hpBar != null)
        {
            hpBar.UpdateHp(hp.Value);
        }
    }

    public override void ResetPerfectRoundStatus()
    {
        PerfectRound = true;
    }

    private void HandleAttackInput(int receivedPlayerId, AttackKey key)
    {
        // 키 입력시 플레이어 ID에 저장된 AttackKey와 일치한 경우 RegisterDefense 작동

        // 멀티 모드일 경우
        if (GameManager.Instance.currentGameState.Value == GameState.Multi)
        {
            ReportAttackInputServerRpc(key);
        }
        // 싱글 혹은 로컬 모드인 경우
        else
        {
            // ID와 다르면 무시
            if (receivedPlayerId != this.playerId) return;

            // 내 ID와 일치하면 Attacker 로직 실행 
            attacker.AddNote(key);
        }
    }

    private void HandleDefendInput(int receivedPlayerId, AttackKey key)
    {
        // 키 입력시 플레이어 ID에 저장된 AttackKey와 일치한 경우 RegisterDefense 작동

        // 멀티 모드일 경우
        if (GameManager.Instance.currentGameState.Value == GameState.Multi)
        {
            ReportDefendInputServerRpc(key);
        }
        // 싱글 혹은 로컬 모드인 경우
        else
        {
            // ID와 다르면 무시
            if (receivedPlayerId != this.playerId) return;

            // 내 ID와 일치하면 Defender 로직 실행
            int lastIndex = defender.noteIndex;
            JudgementResult result = defender.RegisterDefense(key);
            if (result.judgement != Judgement.None)
                StartCoroutine(PlayHitAnimationSequence(key, result, lastIndex));
        }
    }


    // 공격을 위한 코루틴 함수
    public override IEnumerator TakeAttackTurn(Action<List<AttackKey>> onPatternGenerated, RhythmUIController uiController)
    {
        // 멀티 모드일 경우 별개의 코드를 사용하기에 생략
        if (GameManager.Instance.currentGameState.Value == GameState.Multi)
        {
            yield break;
        }

        Debug.Log($"Player {playerId}의 공격 턴 시작!");

        PlayerInputManager.Instance.OnPlayerInput += HandleAttackInput;

        attacker.Init();

        // Attacker 턴 시작 시간 알림.
        attacker.StartTurnTimer();

        float turnStartTime = Time.time;
        long totalTurnTimeMs = policy.GetTurnEndTime();
        float totalTurnDuration = totalTurnTimeMs / 1000f;
        float turnEndTime = turnStartTime + totalTurnDuration;

        while (Time.time < turnEndTime)
        {
            attacker.UpdateNoneNote();

            // 전체 턴 시간 기준으로 타이머를 업데이트
            float elapsedTime = Time.time - turnStartTime;
            uiController.UpdateTimer(elapsedTime / totalTurnDuration);

            // 실시간으로 입력되는 패턴을 화면에 표기
            uiController.UpdateDynamicPattern(attacker.attackPattern, policy);

            yield return null;
        }

        PlayerInputManager.Instance.OnPlayerInput -= HandleAttackInput;

        uiController.HighlightCurrentNote(-1);
        attacker.FillRemainingNotesAsNone();

        onPatternGenerated?.Invoke(attacker.attackPattern);
        Debug.Log($"Player {playerId}의 공격 턴 종료!");
    }

    // 수비를 위한 코루틴 함수
    public override IEnumerator TakeDefendTurn(List<AttackKey> pattern, RhythmUIController uiController)
    {
        // 멀티 모드일 경우 별개의 코드를 사용하기에 생략
        if (GameManager.Instance.currentGameState.Value == GameState.Multi)
        {
            yield break;
        }

        Debug.Log($"Player {playerId}의 수비 턴 시작!");

        PlayerInputManager.Instance.OnPlayerInput += HandleDefendInput;

        defender.Init(pattern);

        // Defender에게 턴 시작 시간 알림.
        defender.StartTurnTimer();

        oppPlayerId = playerId == 1 ? 2 : 1;

        float turnStartTime = Time.time;
        long totalTurnTimeMs = policy.GetTurnEndTime();
        float totalTurnDuration = totalTurnTimeMs / 1000f;
        float turnEndTime = turnStartTime + totalTurnDuration;

        while (Time.time < turnEndTime && !defender.IsFinished())
        {
            int lastIndex = defender.noteIndex;

            long elapsedMs = (long)((Time.time - turnStartTime) * 1000);
            JudgementResult missResult = defender.UpdateMissCheck(elapsedMs);

            if (missResult.judgement != Judgement.None)
               StartCoroutine(PlayHitAnimationSequence(pattern[lastIndex], missResult, lastIndex));

            // 전체 턴 시간 기준으로 타이머를 업데이트
            float elapsedTime = Time.time - turnStartTime;
            uiController.UpdateTimer(elapsedTime / totalTurnDuration);

            // 노트 하이라이트
            uiController.HighlightCurrentNote(defender.noteIndex);

            yield return null;
        }

        PlayerInputManager.Instance.OnPlayerInput -= HandleDefendInput;

        yield break;
    }

    public override IEnumerator TakeSpecialAttackTurn()
    {
        Debug.Log($"Player {playerId}의 스페셜 어택 턴 시작!");

        AudioManager.Instance.PlayCharging();

        const float timeLimit = 7.5f;       // 제한 시간 7.5초 (7500ms)
        const int successThreshold = 100;    // 성공 기준 100회 연타
        int hitCount = 0;                    // 연타 카운트

        // 입력을 카운트하기 위한 간단한 이벤트 핸들러
        Action<int, AttackKey> attackHandler = (id, key) =>
        {
            if (id == this.playerId)
            {
                hitCount++;
            }
        };

        PlayerInputManager.Instance.OnPlayerInput += attackHandler;

        float startTime = Time.time;
        float endTime = startTime + timeLimit;
        
        VFXController.Instance.PlayChargingEffect();
        
        // 2. 제한 시간 동안 대기
        while (Time.time < endTime)
        {
            CharacterAnimatorControl.Instance.TriggerCharging(playerId);
            yield return null; // 다음 프레임까지 대기
        }

        PlayerInputManager.Instance.OnPlayerInput -= attackHandler;

        Debug.Log($"스페셜 어택 종료! 총 {hitCount}회 입력.");

        AudioManager.Instance.StopCharging();

        CharacterAnimatorControl.Instance.TriggerAttack(playerId, AttackKey.Special);

        // 최종 성공 여부를 bool 값으로 반환
        yield return hitCount >= successThreshold;
    }

    private IEnumerator PlayHitAnimationSequence(AttackKey attackKey, JudgementResult result, int noteIndex)
    {
        // (GameManager.Instance.rhythmUI가 RhythmUIController라고 가정)
        if (GameManager.Instance.rhythmUI != null)
        {
            // 1단계에서 수정한 ShowJudgement 함수 호출
            GameManager.Instance.rhythmUI.ShowJudgement(noteIndex, result);
        }

        switch (result.judgement)
        {
            case Judgement.Perfect:
                AudioManager.Instance.PlaySFX(AudioManager.Instance.guardSound);

                CharacterAnimatorControl.Instance.TriggerAttack(oppPlayerId, attackKey);
                VFXController.Instance.PlaySwingEffect(oppPlayerId, attackKey);
                CharacterAnimatorControl.Instance.TriggerGuard(playerId);
                VFXController.Instance.PlayGuardEffect(playerId);
                break;
            case Judgement.Pass:
                break;
            case Judgement.NonePass:
                AudioManager.Instance.PlaySFX(AudioManager.Instance.nonePassSound);

                CharacterAnimatorControl.Instance.TriggerGuard(playerId);
                break;
            case Judgement.Good:
            case Judgement.Miss:
                AudioManager.Instance.PlaySFX(AudioManager.Instance.hitSound);

                CharacterAnimatorControl.Instance.TriggerAttack(oppPlayerId, attackKey);
                VFXController.Instance.PlaySwingEffect(oppPlayerId, attackKey);
                CharacterAnimatorControl.Instance.TriggerHit(playerId, random.Next(1, 3));
                VFXController.Instance.PlayHitEffect(playerId);
                break;
        }

        yield return new WaitForSeconds(0.2f);
    }

    #region 멀티 플레이용 네트워크 콜백 (클라이언트 통신)

    // 서버가 클라이언트에게 "Setup을 실행하라"고 명령하는 RPC
    [ClientRpc]
    public void SetupClientRpc(GameDifficulty gd, JudgementDifficulty jd)
    {
        // 이 RPC는 모든 클라이언트에게 가지만, "주인"만 실행
        if (!IsOwner) return; 

        Debug.Log($"[ClientRpc] SetupClientRpc 수신. IsOwner = {IsOwner}");
        
        // GameManager의 동기화된 난이도 값(NetworkDifficulty)을 가져옴
        GameDifficulty syncedDifficulty = GameManager.Instance.NetworkDifficulty.Value;
        
        // 로컬 클라이언트에서 정책(Policy)을 "재생성"
        PolicyManager localPolicy = new PolicyManager();
        if (syncedDifficulty == GameDifficulty.Hard)
        {
            localPolicy.Configure(PolicyManager.Difficulty.Hard);
        }
        else
        {
            localPolicy.Configure(PolicyManager.Difficulty.Normal);
        }
        
        Setup(localPolicy, syncedDifficulty, jd);
    }

    [ClientRpc]
    // 공격 턴 네트워크 콜백
    public void StartAttackTurnClientRpc()
    {
        // 내 턴이 아니면 아무것도 안 함
        if (!IsOwner) return;

        Debug.Log($"[ClientRpc] 공격 턴 시작 신호 수신. 입력을 구독합니다.");

        isMyAttackTurn = true;

        attacker.Init(); // 공격 턴 시작 시 Attacker 초기화
        attacker.StartTurnTimer();

        PlayerInputManager.Instance.OnPlayerInput += HandleAttackInput;
    }

    // 수비 턴 네트워크 콜백
    [ClientRpc]
    public void StartDefendTurnClientRpc(AttackKey[] pattern)
    {
        // 내 턴이 아니면 아무것도 안 함
        if (!IsOwner) return;

        isMyDefendTurn = true;

        Debug.Log($"[ClientRpc] 수비 턴 시작 신호 수신. 입력을 구독합니다.");

        // 서버로부터 받은 패턴으로 Defender 초기화
        defender.Init(new List<AttackKey>(pattern));
        defender.StartTurnTimer();

        // TODO: UI에 패턴 표시 (GameManager가 별도 Rpc로 처리하거나 여기서 해도 됨)
        // rhythmUI.DisplayPattern(defender.attackPattern, policy);

        PlayerInputManager.Instance.OnPlayerInput += HandleDefendInput;

    }

    // 턴 종료 네트워크 콜백
    [ClientRpc]
    public void EndTurnClientRpc()
    {
        // 내 턴이 아니면 아무것도 안 함
        if (!IsOwner) return;

        if (isMyAttackTurn)
        {
            isMyAttackTurn = false;

            attacker.FillRemainingNotesAsNone();

            SubmitAttackPatternServerRpc(attacker.attackPattern.ToArray());

            PlayerInputManager.Instance.OnPlayerInput -= HandleAttackInput;
        }
        else if (isMyDefendTurn)
        {
            isMyDefendTurn = false;

            PlayerInputManager.Instance.OnPlayerInput -= HandleDefendInput;
        }

        Debug.Log($"[ClientRpc] 턴 종료 신호 수신. 입력을 구독 해제합니다.");
    }
    
        
    #endregion
    
    #region 멀티 플레이용 네트워크 콜백 (서버 통신)

    [ServerRpc]
    private void ReportAttackInputServerRpc(AttackKey key, ServerRpcParams rpcParams = default)
    {
        // 이 코드는 호스트에서만 실행됨
        // GameManager에게 (입력한 사람, 입력 키)를 전달
        GameManager.Instance.ProcessAttackInput(rpcParams.Receive.SenderClientId, key);
    }

    [ServerRpc]
    private void ReportDefendInputServerRpc(AttackKey key, ServerRpcParams rpcParams = default)
    {
        // 이 코드는 호스트에서만 실행됨
        // GameManager에게 (입력한 사람, 입력 키)를 전달
        GameManager.Instance.ProcessDefendInput(rpcParams.Receive.SenderClientId, key);
    }

    [ServerRpc]
    private void SubmitAttackPatternServerRpc(AttackKey[] pattern)
    {
        // 이 코드는 호스트에서만 실행됨
        // 서버에 있는 GameManager를 찾아 수신한 패턴을 전달하고 다음 턴(수비 턴)을 시작하라고 지시
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReceiveAttackPattern(pattern);
        }
    }

    #endregion
}
