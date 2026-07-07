using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Random = System.Random;
using Debug = UnityEngine.Debug;
using UnityEngine.Experimental.AI;
using NUnit.Framework;
using Unity.VisualScripting;

public enum GameDifficulty { Eazy, Normal, Hard }
public enum GameState { Single, Local, Multi }

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] public NetworkVariable<GameState> currentGameState =
    new NetworkVariable<GameState>(GameState.Single, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private GameDifficulty currentGameDifficulty;

    // 플레이어 슬롯 프리팹
    [Header("Player Prefabs")]
    [SerializeField] private LocalPlayer localPlayerPrefab;    // 로컬 플레이어 슬롯 프리팹
    [SerializeField] private CpuPlayer cpuPlayerPrefab;        // CPU 플레이어 슬롯 프리팹

    // 플레이어 캐릭터 프리팹
    [Header("Character Prefabs")]
    [SerializeField] public GameObject player1CharacterPrefab; // 플레이어 1의 캐릭터 프리팹
    [SerializeField] public GameObject player2CharacterPrefab; // 플레이어 2의 캐릭터 프리팹
    [SerializeField] public GameObject cpuCharacterPrefab;     // CPU의 캐릭터 프리팹

    private Participant p1;
    private Participant p2;
    private PolicyManager policy;
    private Stopwatch sw;
    private GameDifficulty gd;
    private JudgementDifficulty jd;


    private Transform player1Slot;
    private Transform player2Slot;
    private HPBarController player1HpBar;
    private HPBarController player2HpBar;
    public RhythmUIController rhythmUI;
    private GameObject endGamePanel; // 게임 종료 시 활성화할 UI 패널
    private GameObject audienceParent;

    // 멀티 플레이를 위해 전달할 패턴
    private List<AttackKey> currentPattern;
    // 패턴을 받았는지 확인하는 플래그 (코루틴 대기용)
    private bool attackPatternReceived = false;
    // Ingame Scene References 저장 변수
    private IngameSceneReferences sceneReferencesCache = null;

    // 클라이언트에 난이도를 전달하기 위한 NetworkVariable 변수
    public NetworkVariable<GameDifficulty> NetworkDifficulty =
        new NetworkVariable<GameDifficulty>(GameDifficulty.Normal, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    private bool p1_turn;               // 플레이어 1의 턴인지 확인
    private int round;                  // 인게임 라운드
    private bool isGameEnded = false;   // 게임이 끝났는 가에 대한 여부

    // 멀티 플레이용 서버에서 사용할 객체들
    private Attacker p1_attacker;
    private Attacker p2_attacker;
    private Defender p1_defender;
    private Defender p2_defender;

    // 멀티 플레이용 클라이언트 ID 저장 (InitGame에서 설정)
    private ulong p1_ClientId;
    private ulong p2_ClientId;

    private void Update()
    {
        // 멀티 모드일 경우, 호스트의 게임 매니저만 사용하기에 리턴
        if (currentGameState.Value != GameState.Multi && !IsServer) return;

        if (p1 == null || p2 == null) return;

        if (!isGameEnded && (p1.IsDead() || p2.IsDead()))
        {
            isGameEnded = true; // 중복 실행 방지
            StartCoroutine(EndGame());
        }

        // Update에서는 이제 게임 종료 조건만 체크하거나, 
        // 실시간으로 변해야 하는 UI 업데이트 등을 처리할 수 있습니다.
    }

    private void Awake()
    {
        // 싱글톤 패턴 (게임 전역에서 접근 가능)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopAllCoroutines();
    }

    public override void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void StartGameLogic()
    {
        // 서버가 아니면 게임 로직을 시작하지 않음
        if (!IsServer) return;

        InitGame();

        // 게임 시작 시, 게임 종료 패널은 항상 숨겨둡니다.
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }

        // 관객 활성화 로직
        if (audienceParent != null)
        {
            audienceParent.SetActive(currentGameState.Value != GameState.Single);
        }

        Debug.Log("게임 매니저 시작됨 ✅");
        Debug.Log(currentGameState.Value.ToString());

        switch (currentGameState.Value)
        {
            case GameState.Single:
                Debug.Log("싱글 플레이 시작");
                StartCoroutine(SingleGame());
                break;
            case GameState.Local:
                Debug.Log("로컬 멀티 플레이 시작");
                StartCoroutine(LocalGame());
                break;
            case GameState.Multi:
                Debug.Log("온라인 멀티 플레이 시작");
                StartCoroutine(MultiGame());
                break;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Ingame Scene")
        {
            Debug.Log("<color=green>Ingame Scene 도착! 필요한 오브젝트들을 연결합니다...</color>");

            // 씬의 안내소 역할을 하는 GameSceneReferences를 찾음
            IngameSceneReferences refs = FindAnyObjectByType<IngameSceneReferences>();
            this.sceneReferencesCache = refs;
            if (refs != null)
            {
                // 안내소를 통해 모든 부품들을 GameManager에 연결
                this.player1Slot = refs.player1Slot;
                this.player2Slot = refs.player2Slot;
                this.player1HpBar = refs.player1HpBar;
                this.player2HpBar = refs.player2HpBar;
                this.rhythmUI = refs.rhythmUI;
                this.endGamePanel = refs.endGamePanel;
                this.audienceParent = refs.audienceParent;

                // 모든 연결이 끝났으니 실제 게임 로직 시작
                if (IsServer)
                    StartGameLogic();
            }
            else
            {
                Debug.LogError("Ingame Scene에서 GameSceneReferences 스크립트를 찾을 수 없습니다!");
            }
        }
    }

    public void SetGameState(GameState newGameState)
    {
        this.currentGameState.Value = newGameState;
        Debug.Log($"게임 모드가 <color=yellow>{newGameState}</color> (으)로 설정되었습니다.");
    }

    public void SetDifficultyAndPolicy(GameDifficulty newDifficulty, PolicyManager newPolicy)
    {
        this.currentGameDifficulty = newDifficulty;
        this.policy = newPolicy;
        Debug.Log($"게임 난이도가 <color=yellow>{newDifficulty}</color> (으)로 설정되었습니다.");

        // 만약 InitGame()이 policy가 null이라서 실패했다면 즉시 두뇌 생성
        if (IsServer && p1_attacker == null)
        {
            jd = JudgementDifficulty.NormalJudgement; // (InitGame에 있던 값)
            p1_attacker = new Attacker(policy);
            p1_defender = new Defender(policy, null, jd);
            p2_attacker = new Attacker(policy);
            p2_defender = new Defender(policy, null, jd);
            Debug.Log("서버 '두뇌'가 'policy'와 함께 초기화되었습니다.");
        }
    }

    #region 싱글 플레이
    // PVE(싱글 플레이)시 진행
    private IEnumerator SingleGame()
    {
        player1HpBar.UpdateName(p1.participantName.Value.ToString());
        player2HpBar.UpdateName(p2.participantName.Value.ToString());
        player1HpBar.UpdateRole("defender");
        player2HpBar.UpdateRole("attacker");

        rhythmUI.UpdateRoundText("R" + round);
        yield return null;

        rhythmUI.DisplayNoteMarkers(policy);     // 패턴 마커 표시

        while (!p1.IsDead() && !p2.IsDead())
        {
            if (round == 0)
            {
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("CPU Turn", 1.5f));
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Ready?", 1f));
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Fight!", 1.5f));
            }
            else
            {
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Next Turn", 1f));
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Ready?", 1f));
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Fight!", 1.5f));
            }
            p1.ResetPerfectRoundStatus();

            yield return new WaitForSeconds(0.5f);
            
            yield return null;

            for (int attackingOpportunit = 3; attackingOpportunit > 0; attackingOpportunit--)
            {
                List<AttackKey> currentPattern = null; // 현재 턴의 패턴을 저장할 변수

                yield return StartCoroutine(p2.TakeAttackTurn((generatedPattern) =>
                {
                    currentPattern = generatedPattern;
                }, rhythmUI));

                if (currentPattern == null)
                {
                    Debug.LogError("패턴이 생성되지 않았습니다!");
                    yield break; // 코루틴 중단
                }
                yield return new WaitForSeconds(0.5f);

                rhythmUI.DisplayPattern(currentPattern, policy); // 패턴 아이콘 표시

                yield return StartCoroutine(p1.TakeDefendTurn(currentPattern, rhythmUI));
                if (p1.IsDead() || p2.IsDead()) break;

                rhythmUI.ClearTurnUI();
                yield return new WaitForSeconds(1.0f);
            }
            if (p1.IsDead() || p2.IsDead()) break;

            yield return StartCoroutine(rhythmUI.ShowAnnouncement("Attack Chance!", 1.5f));
            bool specialAttackSuccess = false;

            var specialAttackCoroutine = p1.TakeSpecialAttackTurn();
            while (specialAttackCoroutine.MoveNext())
            {
                if (specialAttackCoroutine.Current is bool result)
                {
                    specialAttackSuccess = result;
                }
                yield return specialAttackCoroutine.Current;
            }

            CharacterAnimatorControl.Instance.TriggerAttack(2, AttackKey.Special);
            VFXController.Instance.PlaySwingEffect(2, AttackKey.Special);

            if (specialAttackSuccess)
            {
                Debug.Log("스페셜 어택 성공! CPU에게 데미지!");
                int SpecialAttackDamage = p1.PerfectRound ? 50 : 25;
                if (p1.PerfectRound)
                {
                    Debug.Log("CPU에게 크리티컬 데미지!");
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.hitSound);

                    CharacterAnimatorControl.Instance.TriggerHit(2, 1);
                    VFXController.Instance.PlayHitEffect(1);
                }
                else
                {
                    Debug.Log("CPU에게 데미지!");
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.hitSound);

                    CharacterAnimatorControl.Instance.TriggerHit(2, 0);
                    VFXController.Instance.PlayHitEffect(1);
                }
                p2.UpdateHpByAttack(SpecialAttackDamage); // CPU(p2)의 데미지 처리 함수 호출
            }
            else
            {
                Debug.Log("스페셜 어택 실패.");
                CharacterAnimatorControl.Instance.TriggerGuard(2);
            }

            if (p1.PerfectRound)
            {
                p1.ApplyPerfectRoundBonus();
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Perfect Defend!", 1.0f));
            }

            round++;
        }

        yield return StartCoroutine(EndGame());
    }
    #endregion

    #region 로컬 플레이
    // PVP(로컬 멀티 플레이)시 진행
    private IEnumerator LocalGame()
    {
        player1HpBar.UpdateName(p1.participantName.Value.ToString());
        player2HpBar.UpdateName(p2.participantName.Value.ToString());

        rhythmUI.DisplayNoteMarkers(policy);

        while (!p1.IsDead() && !p2.IsDead() && round <= 6)
        {
            Participant attacker;
            Participant defender;

            rhythmUI.UpdateRoundText("R" + round);

            if (p1_turn)
            {
                player1HpBar.UpdateRole("attacker");
                player2HpBar.UpdateRole("defender");
                attacker = p1;
                defender = p2;
                yield return null;
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Round " + round, 1.5f));
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Player 1 Turn", 2f));
            }
            else
            {
                player2HpBar.UpdateRole("attacker");
                player1HpBar.UpdateRole("defender");
                attacker = p2;
                defender = p1;
                yield return null;
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Round " + round, 1.5f));
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Player 2 Turn", 2f));
            }

            yield return StartCoroutine(rhythmUI.ShowAnnouncement("Ready?", 1f));
            yield return StartCoroutine(rhythmUI.ShowAnnouncement("Fight!", 1.5f));
            yield return new WaitForSeconds(0.5f);

            defender.ResetPerfectRoundStatus();

            for (int attackingOpportunit = 3; attackingOpportunit > 0; attackingOpportunit--)
            {
                yield return new WaitForSeconds(0.5f); // 기회 사이의 준비 시간

                List<AttackKey> currentPattern = null; // 현재 턴의 패턴을 저장할 변수

                yield return StartCoroutine(attacker.TakeAttackTurn((generatedPattern) =>
                {
                    currentPattern = generatedPattern;
                }, rhythmUI));

                if (currentPattern == null)
                {
                    Debug.LogError("패턴이 생성되지 않았습니다!");
                    yield break; // 코루틴 중단
                }
                yield return new WaitForSeconds(0.5f);

                rhythmUI.DisplayPattern(currentPattern, policy); // 패턴 아이콘 표시

                yield return StartCoroutine(defender.TakeDefendTurn(currentPattern, rhythmUI));
                if (attacker.IsDead() || defender.IsDead()) break;

                rhythmUI.ClearTurnUI();
                yield return new WaitForSeconds(1.0f);
            }
            if (attacker.IsDead() || defender.IsDead()) break;

            if (defender.PerfectRound)
            {
                defender.ApplyPerfectRoundBonus();
                yield return StartCoroutine(rhythmUI.ShowAnnouncement("Perfect Defend!", 1.0f));
            }

            p1_turn = p1_turn ? false : true;

            round++;
            yield return new WaitForSeconds(1.0f);
        }

        yield return StartCoroutine(EndGame());
    }
    #endregion

    #region 멀티 플레이
    // PVP(온라인 멀티 플레이)시 진행
    private IEnumerator MultiGame()
    {
        // 서버에서만 실행
        if (!IsServer) yield break;

        yield return new WaitForSeconds(1.0f);

        HpBarInitClientRpc(p1.participantName.Value.ToString(), p2.participantName.Value.ToString());
        DisplayMarkersClientRpc();

        while (!p1.IsDead() && !p2.IsDead() && round <= 6)
        {
            Participant attackerParticipant;
            Participant defenderParticipant;
            Attacker attackerLogic;
            Defender defenderLogic;
            ulong attackerClientId, defenderClientId;

            // 라운드 UI 업데이트 (모두에게 방송)
            UpdateRoundTextClientRpc(round);

            if (p1_turn)
            {
                UpdateRoleUIClientRpc("attacker", "defender");
                attackerParticipant = p1;
                defenderParticipant = p2;
                attackerLogic = p1_attacker;
                defenderLogic = p2_defender;
                attackerClientId = p1_ClientId;
                defenderClientId = p2_ClientId;

                ShowAnnouncementClientRpc("Round " + round, true);
                yield return new WaitForSeconds(1.4f);
                ShowAnnouncementClientRpc("Round " + round, false);
                ShowAnnouncementClientRpc("Player 1 Turn", true);
                yield return new WaitForSeconds(1.9f);
                ShowAnnouncementClientRpc("Player 1 Turn", false);
            }
            else
            {
                UpdateRoleUIClientRpc("defender", "attacker");
                attackerParticipant = p2;
                defenderParticipant = p1;
                attackerLogic = p2_attacker;
                defenderLogic = p1_defender;
                attackerClientId = p2_ClientId;
                defenderClientId = p1_ClientId;

                ShowAnnouncementClientRpc("Round " + round, true);
                yield return new WaitForSeconds(1.4f);
                ShowAnnouncementClientRpc("Round " + round, false);
                ShowAnnouncementClientRpc("Player 2 Turn", true);
                yield return new WaitForSeconds(1.9f);
                ShowAnnouncementClientRpc("Player 2 Turn", false);
            }

            ShowAnnouncementClientRpc("Ready?", true);
            yield return new WaitForSeconds(0.9f);
            ShowAnnouncementClientRpc("Ready?", false);
            ShowAnnouncementClientRpc("Fight!", true);
            yield return new WaitForSeconds(1.4f);
            ShowAnnouncementClientRpc("Fight!", false);
            yield return new WaitForSeconds(0.5f);

            defenderParticipant.ResetPerfectRoundStatus(); // 서버측 값 변경

            ClearTurnUIClientRpc();

            for (int attackingOpportunit = 3; attackingOpportunit > 0; attackingOpportunit--)
            {
                yield return new WaitForSeconds(0.5f);

                // --- 1. 공격 턴 (서버가 시간 재기) ---
                attackerLogic.Init();

                // 플래그 리셋 (패턴 수신 대기 시작)
                attackPatternReceived = false;

                attackerLogic.StartTurnTimer();

                // 공격자에게 턴 시작 '방송'
                (attackerParticipant as LocalPlayer).StartAttackTurnClientRpc();

                float turnStartTime = Time.time;
                long totalTurnTimeMs = policy.GetTurnEndTime();
                float totalTurnDuration = totalTurnTimeMs / 1000f;
                float turnEndTime = turnStartTime + totalTurnDuration;

                while (Time.time < turnEndTime)
                {
                    // 클라이언트가 패턴을 먼저 보냈다면 루프 탈출
                    if (attackPatternReceived)
                    {
                        Debug.Log("공격자가 턴을 조기 종료했습니다.");
                        break; 
                    }

                    // 서버가 타이머 UI 업데이트 '방송'
                    float elapsedTime = Time.time - turnStartTime;
                    UpdateTimerClientRpc(elapsedTime / totalTurnDuration);

                    // (ProcessAttackInput이 attackerLogic에 노트를 채워줌)
                    yield return null;
                }

                // 공격자에게 턴 종료 '방송'
                (attackerParticipant as LocalPlayer).EndTurnClientRpc();

                // 만약 턴이 시간 초과되었고 아직 패턴을 못 받았다면 클라이언트가 RPC를 보낼 때까지 대기
                if (!attackPatternReceived)
                {
                    Debug.Log("시간 초과. 공격자로부터 패턴 수신 대기...");

                    // (안전장치) 랙 등에 대비해 2초간 타임아웃 설정
                    float timeoutTime = Time.time + 1.0f;
                    yield return new WaitUntil(() => attackPatternReceived || Time.time > timeoutTime);
                }
                
                // 패턴을 받았는지 최종 확인
                if (!attackPatternReceived)
                {
                    // 타임아웃 발생
                    Debug.LogError("공격자로부터 패턴 수신 실패! (타임아웃). 빈 패턴으로 강제 진행.");
                    attackerLogic.FillRemainingNotesAsNone(); // 서버 로직으로 강제 None 채우기
                    this.currentPattern = attackerLogic.attackPattern; // GameManager의 'currentPattern'에 할당
                }

                yield return new WaitForSeconds(0.5f);

                DisplayPatternClientRpc(this.currentPattern.ToArray());

                // --- 2. 수비 턴 (서버가 시간 재기) ---
                defenderLogic.Init(this.currentPattern);
                defenderLogic.StartTurnTimer();

                // 수비자에게 턴 시작 '방송' (패턴과 함께)
                (defenderParticipant as LocalPlayer).StartDefendTurnClientRpc(this.currentPattern.ToArray());

                turnStartTime = Time.time;
                turnEndTime = turnStartTime + totalTurnDuration;

                while (Time.time < turnEndTime && !defenderLogic.IsFinished() && defenderLogic.noteIndex < policy.PatternLength)
                {
                    // 서버가 타이머 UI 업데이트 방송
                    float elapsedTime = Time.time - turnStartTime;
                    UpdateTimerClientRpc(elapsedTime / totalTurnDuration);

                    // ProcessDefendInput이 defenderLogic으로 판정/방송

                    // Miss 판정 (서버가 직접)
                    long elapsedMs = (long)((Time.time - turnStartTime) * 1000);
                    int lastIndex = defenderLogic.noteIndex; // UpdateMissCheck 전에 인덱스 저장
                    JudgementResult missResult = defenderLogic.UpdateMissCheck(elapsedMs);

                    if (missResult.judgement != Judgement.None)
                    {
                        switch (missResult.judgement)
                        {
                            case Judgement.Miss:
                                defenderParticipant.ApplyDamage(Participant.MissDamage);
                                break;
                            case Judgement.NonePass:
                                defenderParticipant.ApplyNonePassDamage();
                                break;
                        }

                        PlayHitAnimationSequenceClientRpc(defenderClientId, this.currentPattern[lastIndex], missResult.judgement);
                    }

                    yield return null;
                }

                // 수비자에게 턴 종료 '방송'
                (defenderParticipant as LocalPlayer).EndTurnClientRpc();

                if (attackerParticipant.IsDead() || defenderParticipant.IsDead()) break;

                ClearTurnUIClientRpc();

                yield return new WaitForSeconds(1.0f);
            }
            if (attackerParticipant.IsDead() || defenderParticipant.IsDead()) break;

            if (defenderParticipant.PerfectRound)
            {
                defenderParticipant.ApplyPerfectRoundBonus();

                ShowAnnouncementClientRpc("Perfect Defend!", true);
                yield return new WaitForSeconds(0.9f);
                ShowAnnouncementClientRpc("Perfect Defend!", false);
            }

            p1_turn = !p1_turn; // 턴 교대

            round++;
            yield return new WaitForSeconds(1.0f);
        }

        yield return new WaitForSeconds(1.0f);
    }
    #endregion

    // 캐릭터 생성
    private void SpawnCharacters(GameState gameState)
    {
        // P1 생성
        GameObject p1Object = Instantiate(player1CharacterPrefab, player1Slot);
        p1Object.transform.localPosition = new Vector3((float)-1.5, -6, 0);
        p1Object.transform.localScale = new Vector3(7, 7, 1);
        p1Object.GetComponent<PlayerAnimatorRegistrator>().playerNumber = 1;
        p1.setCharacterVisual(p1Object);
        Debug.Log("플레이어 1이 생성되었습니다.");

        if (gameState == GameState.Single)  // 싱글 플레이일 경우 CPU 생성
        {
            GameObject cpuObject = Instantiate(cpuCharacterPrefab, player2Slot);
            cpuObject.transform.localPosition = new Vector3((float)1.5, -6, 0);
            cpuObject.transform.localScale = new Vector3(7, 7, 1);
            cpuObject.GetComponent<PlayerAnimatorRegistrator>().playerNumber = 2;
            p2.setCharacterVisual(cpuObject);
            Debug.Log("싱글플레이 모드: CPU가 2번 플레이어로 생성되었습니다.");
        }
        else // 그 외의 모드일 경우 P2 생성
        {
            GameObject p2Object = Instantiate(player2CharacterPrefab, player2Slot);
            p2Object.transform.localPosition = new Vector3((float)1.5, -6, 0);
            p2Object.transform.localScale = new Vector3(7, 7, 1);
            p2Object.GetComponent<PlayerAnimatorRegistrator>().playerNumber = 2;
            p2.setCharacterVisual(p2Object);
            Debug.Log("멀티플레이 모드: 플레이어 2가 생성되었습니다.");
        }
    }

    // 게임 초기화 코드
    private void InitGame()
    {
        if (!IsServer) return;

        // 1. 이전 게임의 플레이어가 남아있다면 모두 삭제
        if (player1Slot.childCount > 0) Destroy(player1Slot.GetChild(0).gameObject);
        if (player2Slot.childCount > 0) Destroy(player2Slot.GetChild(0).gameObject);

        if (p1 != null)
        {
            NetworkObject netObjP1 = p1.GetComponent<NetworkObject>();
            if (netObjP1 != null && netObjP1.IsSpawned)
            {
                netObjP1.Despawn(true); // true = Despawn 후 즉시 Destroy
            }
            else if (p1.gameObject != null) // 스폰되지 않은 객체일 경우 대비
            {
                Destroy(p1.gameObject);
            }
            p1 = null;
        }
        if (p2 != null)
        {
            NetworkObject netObjP2 = p2.GetComponent<NetworkObject>();
            if (netObjP2 != null && netObjP2.IsSpawned)
            {
                netObjP2.Despawn(true);
            }
            else if (p2.gameObject != null)
            {
                Destroy(p2.gameObject);
            }
            p2 = null;
        }

        // 2. 난이도 및 정책 설정 (멀티용 '두뇌' 생성)
        gd = currentGameDifficulty;
        jd = JudgementDifficulty.NormalJudgement;

        // P1과 P2의 '두뇌'를 서버(GameManager)가 생성
        p1_attacker = new Attacker(policy);
        p1_defender = new Defender(policy, null, jd); // Participant가 필요 없이, 서버가 직접 제어
        p2_attacker = new Attacker(policy);
        p2_defender = new Defender(policy, null, jd);

        switch (currentGameState.Value)
        {
            case GameState.Single:
                p1 = Instantiate(localPlayerPrefab, player1Slot);
                p1.GetComponent<NetworkObject>().Spawn(); // 네트워크 객체로 스폰
                p2 = Instantiate(cpuPlayerPrefab, player2Slot);
                p2.GetComponent<NetworkObject>().Spawn(); // 네트워크 객체로 스폰

                p1_ClientId = NetworkManager.Singleton.LocalClientId;
                p2_ClientId = p2.GetComponent<NetworkObject>().OwnerClientId; // CPU는 서버 소유

                (p1 as LocalPlayer).Setup(policy, gd, jd);
                (p2 as CpuPlayer).Setup(policy, gd);
                break;
            case GameState.Local:
                p1 = Instantiate(localPlayerPrefab, player1Slot);
                p1.GetComponent<NetworkObject>().Spawn(); // 네트워크 객체로 스폰
                p2 = Instantiate(localPlayerPrefab, player2Slot);
                p2.GetComponent<NetworkObject>().Spawn(); // 네트워크 객체로 스폰

                p1_ClientId = NetworkManager.Singleton.LocalClientId;
                p2_ClientId = p2.GetComponent<NetworkObject>().OwnerClientId; // 로컬 P2도 서버 소유

                (p1 as LocalPlayer).Setup(policy, gd, jd);
                (p2 as LocalPlayer).Setup(policy, gd, jd);
                break;
            case GameState.Multi:
                if (NetworkLobbyManager.Instance == null)
                {
                    Debug.LogError("NetworkLobbyManager.Instance가 null입니다! NetworkLobbyManager.cs의 Awake()에 DontDestroyOnLoad()가 있는지 확인하세요.");
                    isGameEnded = true; // 게임 비정상 종료
                    return; // InitGame 중단
                }

                // 로비 매니저에서 클라이언트 ID 가져오기
                p1_ClientId = NetworkLobbyManager.Instance.PlayerStates[0].ClientId;
                p2_ClientId = NetworkLobbyManager.Instance.PlayerStates[1].ClientId;

                // ID를 기반으로 세팅
                p1 = Instantiate(localPlayerPrefab, player1Slot);
                p1.GetComponent<NetworkObject>().SpawnAsPlayerObject(p1_ClientId);

                // ID를 기반으로 세팅
                p2 = Instantiate(localPlayerPrefab, player2Slot);
                p2.GetComponent<NetworkObject>().SpawnAsPlayerObject(p2_ClientId);
                Debug.Log("스폰 완료");

                (p1 as LocalPlayer).SetupClientRpc(gd, jd);
                (p2 as LocalPlayer).SetupClientRpc(gd, jd);
                break;
        }

        SpawnCharacters(currentGameState.Value);

        // 5. ID 및 이름 설정 (NetworkVariable 값 설정)
        if (p1 is LocalPlayer) (p1 as LocalPlayer).playerId = 1;
        if (p2 is LocalPlayer) (p2 as LocalPlayer).playerId = 2;

        if (currentGameState.Value == GameState.Single)
        {
            p1.participantName.Value = "Player"; // .Value 사용
            p2.participantName.Value = "CPU";    // .Value 사용
            round = 0;
        }
        else
        {
            p1.participantName.Value = "Player 1"; // .Value 사용
            p2.participantName.Value = "Player 2"; // .Value 사용
            round = 1;
        }

        // 6. HP 및 UI 초기화
        p1.hpBar = player1HpBar;
        p2.hpBar = player2HpBar;

        // 초기화 코드
        p1.Init();
        p2.Init();

        // 체력 초기화 코드
        player1HpBar.Init(p1.hp.Value);
        player2HpBar.Init(p2.hp.Value);

        p1_turn = true;

        if (rhythmUI != null)
        {
            rhythmUI.ClearTurnUI();  // 화면에 보일 패턴 리스트 초기화
            rhythmUI.ClearMarkers(); // 화면에 보일 노트 마커 초기화
        }

        Debug.Log("게임 초기화 실행 (서버)");
    }
    
    [ClientRpc]
    private void InitClientRpc()
    {
        IngameSceneReferences refs = this.sceneReferencesCache;
        if (refs == null)
        {
            Debug.LogError($"[InitClientRpc Coroutine] IngameSceneReferences 객체를 2개 찾지 못했습니다!");
            return;
        }

        if (player1Slot.childCount > 0) Destroy(player1Slot.GetChild(0).gameObject);
        if (player2Slot.childCount > 0) Destroy(player2Slot.GetChild(0).gameObject);
        
        p1_ClientId = NetworkLobbyManager.Instance.PlayerStates[0].ClientId;
        p2_ClientId = NetworkLobbyManager.Instance.PlayerStates[1].ClientId;

        LocalPlayer player1 = null;
        LocalPlayer player2 = null;
        LocalPlayer[] allPlayers = FindObjectsByType<LocalPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allPlayers.Length < 2)
        {
            Debug.LogError($"[InitClientRpc Coroutine] LocalPlayer 객체를 2개 찾지 못했습니다! (찾은 개수: {allPlayers.Length})");
        }

        foreach (LocalPlayer player in allPlayers)
        {
            if (player.OwnerClientId == NetworkManager.ServerClientId)
            {
                player1 = player;
                player1.playerId = 1; // ID 할당
            }
            else
            {
                player2 = player;
                player2.playerId = 2; // ID 할당
            }
        }

        // --- 찾은 객체에 UI 연결 ---
        if (player1 != null && player1CharacterPrefab != null)
        {
            GameObject p1Object = Instantiate(player1CharacterPrefab, player1Slot);
            player1.setCharacterVisual(p1Object);
        } else { Debug.LogError("Player 1 또는 P1 CharacterPrefab 찾기/연결 실패!"); }
        
        if (player2 != null && player2CharacterPrefab != null)
        {
            GameObject p2Object = Instantiate(player2CharacterPrefab, player2Slot);
            player2.setCharacterVisual(p2Object);
        } else { Debug.LogError("Player 2 또는 P2 CharacterPrefab 찾기/연결 실패!"); }

        
        if (player1 != null && refs.player1HpBar != null)
        {
            player1.hpBar = refs.player1HpBar;
            Debug.Log("Player 1 HP Bar 연결됨.");
        } else { Debug.LogError("Player 1 또는 P1 HP Bar 찾기/연결 실패!"); }

        if (player2 != null && refs.player2HpBar != null)
        {
            player2.hpBar = refs.player2HpBar;
            Debug.Log("Player 2 HP Bar 연결됨.");
        } else { Debug.LogError("Player 2 또는 P2 HP Bar 찾기/연결 실패!"); }
    }

    private IEnumerator KOLose()
    {
        if (p1.IsDead())
        {
            CharacterAnimatorControl.Instance.TriggerKOLose(1);
            StartCoroutine(p1.PlayKnockoutAnimation(new Vector2(-12f, 0f)));
            yield return null;
        }
        else if (p2.IsDead())
        {
            CharacterAnimatorControl.Instance.TriggerKOLose(2);
            StartCoroutine(p2.PlayKnockoutAnimation(new Vector2(12f, 0f)));
            yield return null;
        }
        else
            yield break;
    }

    private IEnumerator EndGame()
    {
        // 서버만 이 코루틴을 실행
        if (!IsServer) yield break;

        isGameEnded = true;

        if (p1.IsDead() || p2.IsDead())
            AudioManager.Instance.PlaySFX(AudioManager.Instance.KOLoseSound);

        rhythmUI.ClearTurnUI(); // 화면에 보일 패턴 리스트 초기화
        rhythmUI.ClearMarkers(); // 화면에 보일 노트 마커 초기화

        string winner = null;
        StartCoroutine(KOLose());

        ShowAnnouncementClientRpc("Game Set", true);
        yield return new WaitForSeconds(2.4f);
        ShowAnnouncementClientRpc("Game Set", false);
        // yield return StartCoroutine(rhythmUI.ShowAnnouncement("Game Set", 2.5f));
        yield return new WaitForSeconds(0.5f);

        switch (currentGameState.Value)
        {
            case GameState.Single:
                if (p1.IsDead())
                {
                    CharacterAnimatorControl.Instance.TriggerWin(2);
                    winner = "CPU Win!";
                }
                else
                {
                    CharacterAnimatorControl.Instance.TriggerWin(1);
                    winner = "Player 1 Win!";
                }
                break;
            case GameState.Local:
            case GameState.Multi:
                if (p1.IsDead())
                {
                    CharacterAnimatorControl.Instance.TriggerWin(2);
                    winner = "Player 2 Win!";
                }
                else if (p2.IsDead())
                {
                    CharacterAnimatorControl.Instance.TriggerWin(1);
                    winner = "Player 1 Win!";
                }
                else if (p1.hp.Value < p2.hp.Value)
                {
                    CharacterAnimatorControl.Instance.TriggerWin(2);
                    CharacterAnimatorControl.Instance.TriggerLose(1);
                    winner = "Player 2 Win!";
                }
                else if (p1.hp.Value > p2.hp.Value)
                {
                    CharacterAnimatorControl.Instance.TriggerWin(1);
                    CharacterAnimatorControl.Instance.TriggerLose(2);
                    winner = "Player 1 Win!";
                }
                else
                {
                    CharacterAnimatorControl.Instance.TriggerLose(1);
                    CharacterAnimatorControl.Instance.TriggerLose(2);
                    winner = "Draw";
                }
                break;
        }

        ShowAnnouncementClientRpc(winner, true);
        yield return new WaitForSeconds(2.9f);
        ShowAnnouncementClientRpc(winner, false);
        // yield return StartCoroutine(rhythmUI.ShowAnnouncement(winner, 3f));

        ShowEndGamePanelClientRpc();
    }

    // 게임 재시작
    public void RestartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    // 로비로 돌아가기
    public void BackToLobby()
    {
        StartCoroutine(CleanupNetworkSession());
        Debug.Log("로비로 돌아갑니다...");
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby Scene", LoadSceneMode.Single);
    }

    IEnumerator CleanupNetworkSession()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(0.5f);
        }
    }

    #region 네트워크 로직 (세팅)
    // 클라이언트에서 난이도 값이 변경되었을 때 호출될 함수
    void OnDifficultyChanged(GameDifficulty oldDifficulty, GameDifficulty newDifficulty)
    {
        // 로컬 PolicyManager 설정
        PolicyManager policyManager = new PolicyManager();
        if (newDifficulty == GameDifficulty.Hard)
        {
            policyManager.Configure(PolicyManager.Difficulty.Hard);
        }
        else
        {
            policyManager.Configure(PolicyManager.Difficulty.Normal);
        }
        
        // 로컬 GameManager에 실제 적용
        SetDifficultyAndPolicy(newDifficulty, policyManager);
        Debug.Log($"[Client/Host] 난이도 동기화 완료: {newDifficulty}");
    }

    // NetworkBehaviour의 콜백 함수
    public override void OnNetworkSpawn()
    {
        // NetworkVariable의 값이 변경될 때마다 OnDifficultyChanged 함수를 호출하도록 구독
        NetworkDifficulty.OnValueChanged += OnDifficultyChanged;

        // 호스트가 아닌 클라이언트의 경우, 스폰될 때 현재 값으로 즉시 한 번 적용
        if (!IsHost)
        {
            OnDifficultyChanged(NetworkDifficulty.Value, NetworkDifficulty.Value);
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            Debug.Log("NetworkManager 찾음");
            NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
        }
        else
            Debug.Log("NetworkManager 없음");
    }

    public override void OnNetworkDespawn()
    {
        // 구독 해제
        NetworkDifficulty.OnValueChanged -= OnDifficultyChanged;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
        }
    }

    // OnSceneEvent 핸들러
    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        // "Ingame Scene" 로드가 "내 클라이언트"에서 "완료"되었을 때만 처리
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete &&
            sceneEvent.SceneName == "Ingame Scene" &&
            sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"<color=green>[HandleSceneEvent]</color> Ingame Scene LoadComplete event received! 캐릭터 스폰 코루틴 시작...");

            if (currentGameState.Value == GameState.Multi)
                InitClientRpc();
        }

        // 모든 클라이언트 로드 완료 감지
        if (IsServer && sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
        {
            // 모든 클라이언트가 씬 로드를 완료했을 때 실행할 로직
            Debug.Log("<color=yellow>[Server HandleSceneEvent]</color> 모든 클라이언트가 Ingame Scene 로드 완료!");
        }
    }

    #endregion

    #region 네트워크 로직 (입력)
    /// <summary>
    /// 로컬 플레이어에서 처리하는 걸 서버에서 처리하기 위해 게임 매니저로 이양
    /// </summary>

    // LocalPlayer에서 ReportAttackInputServerRpc 호출
    public void ProcessAttackInput(ulong clientId, AttackKey key)
    {
        if (!IsServer) return;
        List<AttackKey> patternToShow = null;

        // 지금 공격 턴인 플레이어의 '두뇌'에 입력을 전달
        if (p1_turn && clientId == p1_ClientId)
        {
            p1_attacker.AddNote(key);
            patternToShow = p1_attacker.attackPattern;
        }
        else if (!p1_turn && clientId == p2_ClientId)
        {
            p2_attacker.AddNote(key);
            patternToShow = p2_attacker.attackPattern;
        }

        // 입력이 성공적일 경우, 패턴 띄우기
        if (patternToShow != null)
        {
            UpdateDynamicPatternClientRpc(patternToShow.ToArray());
        }
    }

    // LocalPlayer에서 ReportDefendInputServerRpc 호출
    public void ProcessDefendInput(ulong clientId, AttackKey key)
    {
        if (!IsServer) return;

        JudgementResult result = JudgementResult.None;
        Participant defender = null;
        List<AttackKey> pattern = null;

        // 지금 수비 턴인 플레이어의 '두뇌'로 판정
        if (p1_turn && clientId == p2_ClientId) // P1 공격, P2 수비
        {
            result = p2_defender.RegisterDefense(key);
            defender = p2;
            pattern = p1_attacker.attackPattern;
        }
        else if (!p1_turn && clientId == p1_ClientId) // P2 공격, P1 수비
        {
            result = p1_defender.RegisterDefense(key);
            defender = p1;
            pattern = p2_attacker.attackPattern;
        }

        // 판정 결과가 나왔다면
        if (result.judgement != Judgement.None && defender != null)
        {
            // 1. 서버가 판정 결과에 따른 데미지 적용
            switch (result.judgement)
            {
                case Judgement.Good:
                    // 정책(policy)에 따른 데미지 계산은 서버(GM)가 하고,
                    // HP 적용 및 PerfectRound 처리는 Participant가 하도록 위임
                    defender.ApplyDamage(Participant.GoodDamage);
                    break;
                case Judgement.Miss:
                    defender.ApplyDamage(Participant.MissDamage);
                    break;
                case Judgement.NonePass:
                    defender.ApplyNonePassDamage(); // PerfectRound = false 처리
                    break;

                // Perfect, Pass는 데미지/패널티 없으므로 호출 안 함
                case Judgement.Perfect:
                case Judgement.Pass:
                    break;
            }

            // 2. 모든 클라이언트에게 연출 '방송'
            PlayHitAnimationSequenceClientRpc(
                p1_turn ? p2_ClientId : p1_ClientId,
                key,
                result.judgement
            );
        }
    }

    // --- 2. 'ReceiveAttackPattern' 함수 추가 ---
    // (LocalPlayer의 ServerRpc가 이 함수를 호출할 것입니다)
    public void ReceiveAttackPattern(AttackKey[] pattern)
    {
        // 이 로직은 서버에서만 실행되어야 합니다.
        if (!IsServer) return;

        // 1. 전달받은 패턴을 서버의 currentPattern 변수에 저장
        this.currentPattern = new List<AttackKey>(pattern);

        // 2. MultiGame 코루틴에게 "패턴 도착했음!"이라고 알려주는 플래그를 true로 설정
        this.attackPatternReceived = true; 
    }
    #endregion

    #region 네트워크 로직 (이펙트/애니메이션 브로드캐스팅)

    [ClientRpc]
    // 플레이어 히트 애니메이션 재생
    private void PlayHitAnimationSequenceClientRpc(ulong defenderClientId, AttackKey attackKey, Judgement judgement)
    {
        // 이 코드는 모든 클라이언트에서 실행
        // LocalPlayer에 있던 PlayHitAnimationSequence 로직을 그대로 가져옴

        int attackerPlayerNum = (defenderClientId == p1_ClientId) ? 2 : 1;
        int defenderPlayerNum = (defenderClientId == p1_ClientId) ? 1 : 2;

        // LocalPlayer의 random.Next(1, 3)을 위해 Random 객체 생성
        Random random = new Random();

        switch (judgement)
        {
            case Judgement.Perfect:
                CharacterAnimatorControl.Instance.TriggerAttack(attackerPlayerNum, attackKey);
                VFXController.Instance.PlaySwingEffect(attackerPlayerNum, attackKey);
                CharacterAnimatorControl.Instance.TriggerGuard(defenderPlayerNum);
                VFXController.Instance.PlayGuardEffect(defenderPlayerNum);
                break;
            case Judgement.Pass:
                break;
            case Judgement.NonePass:
                CharacterAnimatorControl.Instance.TriggerGuard(defenderPlayerNum);
                break;
            case Judgement.Good:
            case Judgement.Miss:
                CharacterAnimatorControl.Instance.TriggerAttack(attackerPlayerNum, attackKey);
                VFXController.Instance.PlaySwingEffect(attackerPlayerNum, attackKey);
                CharacterAnimatorControl.Instance.TriggerHit(defenderPlayerNum, random.Next(1, 3));
                VFXController.Instance.PlayHitEffect(defenderPlayerNum);
                break;
        }

    }

    [ClientRpc]
    // 마커 표시
    private void HpBarInitClientRpc(string p1ParticipantName, string p2ParticipantName)
    {
        player1HpBar.UpdateName(p1ParticipantName);
        player2HpBar.UpdateName(p2ParticipantName);
    }

    [ClientRpc]
    // 판정선에 따른 타이머 UI
    private void UpdateTimerClientRpc(float ratio)
    {
        rhythmUI.UpdateTimer(ratio);
    }

    [ClientRpc]
    // 라운드 UI
    private void UpdateRoundTextClientRpc(int round)
    {
        rhythmUI.UpdateRoundText("R" + round);
    }

    [ClientRpc]
    // 아니운서 UI
    private void ShowAnnouncementClientRpc(string message, bool show)
    {
        if (rhythmUI != null)
        {
            rhythmUI.TriggerAnnouncement(message, show);
        }
    }

    [ClientRpc]
    // 게임 오버 시 나오는 패널
    private void ShowEndGamePanelClientRpc()
    {
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
        }
    }

    [ClientRpc]
    // 패턴 패널
    private void DisplayPatternClientRpc(AttackKey[] pattern)
    {
        // 모든 클라이언트가 자신의 rhythmUI 참조를 사용
        if (rhythmUI != null)
        {
            // AttackKey[] 배열을 List<AttackKey>로 변환, ClientRpc에서 List<AttackKey>를 인식하지 못하기 때문
            rhythmUI.DisplayPattern(new List<AttackKey>(pattern), policy);
        }
    }

    [ClientRpc]
    // 플레이어의 역할 UI
    private void UpdateRoleUIClientRpc(string p1_role, string p2_role)
    {
        // 모든 클라이언트가 자신의 HP Bar 참조를 사용해 역할 텍스트 갱신
        if (player1HpBar != null) player1HpBar.UpdateRole(p1_role);
        if (player2HpBar != null) player2HpBar.UpdateRole(p2_role);
    }

    [ClientRpc]
    // 마커 표시
    private void DisplayMarkersClientRpc()
    {
        // 모든 클라이언트가 자신의 rhythmUI 참조를 사용해 마커 표시
        // RhythmUIController.DisplayNoteMarkers 함수는
        // 내부에서 ClearMarkers()를 먼저 호출하므로
        // 이 함수 하나만 호출하면 마커가 깨끗이 갱신됩니다.
        if (rhythmUI != null)
        {
            rhythmUI.DisplayNoteMarkers(policy);
        }
    }

    [ClientRpc]
    // 패턴 UI 클리어
    private void ClearTurnUIClientRpc()
    {
        if (rhythmUI != null)
        {
            rhythmUI.ClearTurnUI();
        }
    }

    [ClientRpc]
    // 동적으로 패턴 보이기
    private void UpdateDynamicPatternClientRpc(AttackKey[] currentPattern)
    {
        if (rhythmUI != null)
        {
            // rhythmUI.UpdateDynamicPattern은 List를 받으므로 변환
            rhythmUI.UpdateDynamicPattern(new List<AttackKey>(currentPattern), policy);
        }
    }

    #endregion
}
