using UnityEngine;

public class VFXController : MonoBehaviour
{
    public static VFXController Instance { get; private set; }

    [Header("Effect Prefab")]
    public GameObject hitEffectPrefab;
    public GameObject guardEffectPrefab;
    public GameObject chargingEffectPrefab;
    public GameObject swingP1UpEffectPrefab;
    public GameObject swingP1DownEffectPrefab;
    public GameObject swingP2UpEffectPrefab;
    public GameObject swingP2DownEffectPrefab;

    [Header("Effect Scale")]
    public Vector3 hitEffectScale = new Vector3(0.5f, 0.5f, 1f);
    public Vector3 guardEffectScale = new Vector3(0.25f, 0.25f, 1f);
    public Vector3 chargingEffectScale = new Vector3(0.25f, 0.25f, 1f);
    public Vector3 swingEffectScale = new Vector3(0.15f, 0.15f, 1f);

    [Header("Position Random")]
    [Tooltip("P1 Hit Effect X")]
    public Vector2 p1HitXRange = new Vector2(-2.25f, -1.25f);
    [Tooltip("P1 Hit Effect Y")]
    public Vector2 p1HitYRange = new Vector2(-2.25f, -0.75f);
    
    [Tooltip("P2 Hit Effect X")]
    public Vector2 p2HitXRange = new Vector2(1.25f, 2.25f);
    [Tooltip("P2 Hit Effect Y")]
    public Vector2 p2HitYRange = new Vector2(-2.25f, -0.75f);

    [Tooltip("P1 Guard Effect X")]
    public Vector2 p1GuardXRange = new Vector2(-1.25f, -0.75f);
    [Tooltip("P1 Guard Effect Y")]
    public Vector2 p1GuardYRange = new Vector2(-1.5f, -0.5f);
    
    [Tooltip("P2 Guard Effect X")]
    public Vector2 p2GuardXRange = new Vector2(0.75f, 1.25f);
    [Tooltip("P2 Guard Effect Y")]
    public Vector2 p2GuardYRange = new Vector2(-1.5f, -0.5f);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 피격 이펙트 재생
    public void PlayHitEffect(int playerNumber)
    {
        if (hitEffectPrefab != null)
        {
            Vector3 position = GetHitPosition(playerNumber);
            GameObject effectInstance = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            effectInstance.transform.localScale = hitEffectScale;
        }
    }

    // 가드 성공 이펙트 재생
    public void PlayGuardEffect(int playerNumber)
    {
        if (guardEffectPrefab != null)
        {
            Vector3 position = GetGuardPosition(playerNumber);
            GameObject effectInstance = Instantiate(guardEffectPrefab, position, Quaternion.identity);
            effectInstance.transform.localScale = guardEffectScale;
        }
    }

    // 공격 차징 이펙트 재생 (위치 등은 추후 수정 필요)
    public void PlayChargingEffect()
    {
        if (chargingEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(chargingEffectPrefab, new Vector3(-2.25f, -2.15f, 1), Quaternion.identity);
            effectInstance.transform.localScale = chargingEffectScale;
        }
    }

    // 공격 이펙트 재생
    public void PlaySwingEffect(int playerNumber, AttackKey attackKey)
    {
        GameObject prefabToSpawn = null;

        // 플레이어 번호와 공격 키에 따라 올바른 프리팹을 선택
        if (playerNumber == 1)
        {
            switch (attackKey)
            {
                case AttackKey.Left:
                    prefabToSpawn = swingP1UpEffectPrefab; break;
                case AttackKey.Right:
                case AttackKey.Up:
                case AttackKey.Down:
                case AttackKey.Space:
                case AttackKey.Special:
                    prefabToSpawn = swingP1DownEffectPrefab; break;
            }
        }
        else // playerNumber == 2
        {
            switch (attackKey)
            {
                case AttackKey.Right:
                    prefabToSpawn = swingP2UpEffectPrefab; break;
                case AttackKey.Left:
                case AttackKey.Up:
                case AttackKey.Down:
                case AttackKey.Space:
                    prefabToSpawn = swingP2DownEffectPrefab; break;
            }
        }

        if (prefabToSpawn != null)
        {
            Vector3 position = GetSwingPosition(playerNumber, attackKey);
            GameObject effectInstance = Instantiate(prefabToSpawn, position, Quaternion.identity);
            effectInstance.transform.localScale = swingEffectScale;
        }
    }

    // 히트 위치 설정
    public Vector3 GetHitPosition(int playerNumber)
    {
        float x, y;
        if (playerNumber == 1)
        {
            x = Random.Range(p1HitXRange.x, p1HitXRange.y);
            y = Random.Range(p1HitYRange.x, p1HitYRange.y);
        }
        else // playerNumber == 2
        {
            x = Random.Range(p2HitXRange.x, p2HitXRange.y);
            y = Random.Range(p2HitYRange.x, p2HitYRange.y);
        }
        return new Vector3(x, y, 0);
    }

    // 가드 성공 위치 설정
    public Vector3 GetGuardPosition(int playerNumber)
    {
        float x, y;
        if (playerNumber == 1)
        {
            x = Random.Range(p1GuardXRange.x, p1GuardXRange.y);
            y = Random.Range(p1GuardYRange.x, p1GuardYRange.y);
        }
        else // playerNumber == 2
        {
            x = Random.Range(p2GuardXRange.x, p2GuardXRange.y);
            y = Random.Range(p2GuardYRange.x, p2GuardYRange.y);
        }
        return new Vector3(x, y, 0);
    }

    public Vector3 GetSwingPosition(int playerNumber, AttackKey position)
    {
        float x = 0f, y = 0f;
        if (playerNumber == 1)
        {
            switch (position)
            {
                case AttackKey.Left: x = -0.5f; y = -1f; break;
                case AttackKey.Right: x = -0.5f; y = -2f; break;
                case AttackKey.Space: x = -0.5f; y = -2f; break;
                case AttackKey.Up: x = -0.5f; y = -2.5f; break;
                case AttackKey.Down: x = -0.5f; y = -2.5f; break;
                case AttackKey.Special: x = -0.5f; y = -2f; break;
            }
        }
        else // playerNumber == 2
        {
            switch (position)
            {
                case AttackKey.Left: x = 0.5f; y = -2f; break;
                case AttackKey.Right: x = 0.5f; y = -1f; break;
                case AttackKey.Space: x = 0.5f; y = -2f; break;
                case AttackKey.Up: x = -0.5f; y = -2.5f; break;
                case AttackKey.Down: x = -0.5f; y = -2.5f; break;
            }
        }
        return new Vector3(x, y, 0);
    }
}
