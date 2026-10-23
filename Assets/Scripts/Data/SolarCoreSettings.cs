using System;
using UnityEngine;

// 태양핵 밸런스 수치의 유일한 정의. Assets/Resources/SolarCoreSettings.asset (메뉴 Tools/태양핵 설정 열기)
// 퍼센트는 비율로 저장한다: 0.2 = 20%. 규칙: Docs/Design/SolarCoreSpec.md
public class SolarCoreSettings : ScriptableObject
{
    public const string ResourcePath = "SolarCoreSettings";

    [Serializable]
    public class Basic
    {
        [Tooltip("바라보는 방향으로 소환하는 거리(m)")] public float spawnDistance = 2f;
        [Tooltip("효과 반경(m)")] public float radius = 3f;
        [Tooltip("유지시간(초)")] public float lifetime = 6f;
        [Tooltip("소멸 후 쿨타임(초)")] public float cooldown = 6f;
        [Tooltip("동시에 설치할 수 있는 최대 개수")] public int maxCount = 1;
        [Tooltip("이동 속도 증가. 0.2 = +20%"), Range(0f, 1f)] public float moveSpeedBonus = 0.2f;
        [Tooltip("공격력 증가. 0.15 = +15%"), Range(0f, 1f)] public float attackBonus = 0.15f;
        [Tooltip("받는 피해 감소. 0.1 = −10%"), Range(0f, 1f)] public float damageReduction = 0.1f;
    }

    [Serializable]
    public class SolarWind
    {
        [Tooltip("이동 속도 보너스에 더하는 값. 0.1 = +10%p"), Range(0f, 1f)] public float extraMoveSpeedBonus = 0.1f;
    }

    [Serializable]
    public class WideExpansion
    {
        [Tooltip("반경 증가율. 0.4 = +40%"), Range(0f, 2f)] public float radiusBonus = 0.4f;
    }

    [Serializable]
    public class LingeringSun
    {
        [Tooltip("추가 유지시간(초)")] public float extraLifetime = 3f;
    }

    [Serializable]
    public class WanderingSun
    {
        [Tooltip("동반형 반경(m). 광역 확장이 곱해진다")] public float radius = 2f;
        [Tooltip("이동 속도 증가. 0.15 = +15%. 태양풍이 더해진다"), Range(0f, 1f)] public float moveSpeedBonus = 0.15f;
        [Tooltip("공격력 증가. 0.1 = +10%"), Range(0f, 1f)] public float attackBonus = 0.1f;
        [Tooltip("받는 피해 감소. 0.07 = −7%"), Range(0f, 1f)] public float damageReduction = 0.07f;
    }

    [Serializable]
    public class DelayedRelease
    {
        [Tooltip("감속 지속시간(초)")] public float duration = 2f;
        [Tooltip("적 이동 속도 감소. 0.25 = −25%"), Range(0f, 1f)] public float slowRatio = 0.25f;
    }

    [Serializable]
    public class BrokenSundial
    {
        [Tooltip("잔광 영역 유지시간(초)")] public float duration = 3f;
        [Tooltip("공격력·받는 피해 감소 보너스 효율. 0.5 = 50%. 이동 속도 보너스는 적용하지 않음"), Range(0f, 1f)] public float efficiency = 0.5f;
    }

    [Serializable]
    public class SolarConstellation
    {
        [Tooltip("일출 충전 이벤트 값. 0.05 = 5%"), Range(0f, 1f)] public float chargeAmount = 0.05f;
        [Tooltip("태양핵 하나당 최대 발동 횟수")] public int maxPerCore = 2;
        [Tooltip("발동 사이 최소 간격(초)")] public float minInterval = 2f;
    }

    [Header("A. 태양핵 기본 능력")]
    public Basic basic = new();

    [Header("B. 특성 — 태양풍 (일반)")]
    public SolarWind solarWind = new();
    [Header("B. 특성 — 광역 확장 (일반)")]
    public WideExpansion wideExpansion = new();
    [Header("B. 특성 — 잔존하는 태양 (일반)")]
    public LingeringSun lingeringSun = new();
    [Header("B. 특성 — 유랑하는 태양 (희귀)")]
    public WanderingSun wanderingSun = new();
    [Header("B. 특성 — 지연 방출 (희귀)")]
    public DelayedRelease delayedRelease = new();

    [Header("C. 유물 — 부서진 해시계 (희귀)")]
    public BrokenSundial brokenSundial = new();
    [Header("C. 유물 — 태양의 성좌 (희귀)")]
    public SolarConstellation solarConstellation = new();

    static SolarCoreSettings cached;

    public static SolarCoreSettings Instance
    {
        get
        {
            if (cached) return cached;
            cached = Resources.Load<SolarCoreSettings>(ResourcePath);
            if (!cached)
            {
                Debug.LogWarning($"Resources/{ResourcePath}.asset이 없어 기본값을 사용합니다. 메뉴 Tools/태양핵 설정 열기로 생성하세요.");
                cached = CreateInstance<SolarCoreSettings>();
            }
            return cached;
        }
    }
}
