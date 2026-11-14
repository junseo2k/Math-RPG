using System;
using System.Collections.Generic;
using UnityEngine;

// 전투 밸런스 수치의 유일한 정의. Assets/Resources/CombatBalance.asset (메뉴 Tools/전투 밸런스 열기)
// 각 스크립트가 쓸 때마다 읽으므로 플레이 중 수정도 바로 반영된다. 최대 HP·개수처럼 생성 시 정해지는 값은 다음 등장부터
public class CombatBalance : ScriptableObject
{
    public const string ResourcePath = "CombatBalance";

    [Serializable]
    public class PlayerStats
    {
        [Tooltip("기본 최대 HP (특성·유물·성장 보너스 전). 다음 판부터 반영")] public int maxHp = 100;
        [Tooltip("피격 후 무적(초). 다음 판부터 반영")] public float hitInvincibleTime = 0.8f;
        [Tooltip("이동 속도(m/초)")] public float moveSpeed = 6f;
        [Header("회피")]
        [Tooltip("회피 이동 거리(m)")] public float dashDistance = 3f;
        [Tooltip("회피 지속(초)")] public float dashTime = 0.25f;
        [Tooltip("회피 시작부터 무적(초)")] public float dashInvincibleTime = 0.2f;
        [Tooltip("회피 쿨타임(초)")] public float dashCooldown = 0.8f;
        [Header("피격")]
        [Tooltip("피격 경직(초)")] public float hitStun = 0.3f;
        [Tooltip("넉백 거리(m)")] public float knockbackDistance = 1f;
    }

    // 3타 콤보의 한 타. 시간은 클릭 순간부터(초). [임시] 2026-11-08
    [Serializable]
    public class ComboHit
    {
        [Tooltip("판정 시점(초). 그림의 타격 프레임 = 프레임 번호 / comboFps")] public float hitDelay;
        [Tooltip("다음 클릭을 받는 시점(초). 이보다 이르면 입력을 잠깐 모아 둔다")] public float interval;
        [Tooltip("피해 배율(공격력에 곱함)")] public float damageMultiplier = 1f;
        [Tooltip("적중 시 히트스톱(초): 플레이어·맞은 적 애니메이션만 멈춘다(게임 시간은 그대로)")] public float hitStop;
    }

    [Serializable]
    public class SwordStats
    {
        [Tooltip("공격력")] public int damage = 10;
        [Tooltip("판정 범위(앞쪽 길이, 폭)")] public Vector2 range = new(1.5f, 1f);
        [Tooltip("치명타 확률. 0.05 = 5%"), Range(0f, 1f)] public float critChance = 0.05f;
        [Tooltip("치명타 배율")] public float critMultiplier = 1.5f;
        [Header("3타 콤보 (2026-11-08 A-2)")]
        [Tooltip("1·2·3타. 타격 프레임: 1타 f4, 2타 f3, 3타 f2 (16fps)")]
        public ComboHit[] combo =
        {
            new() { hitDelay = 3f / 16f, interval = 5f / 16f, damageMultiplier = 1f, hitStop = 0.05f },
            new() { hitDelay = 2f / 16f, interval = 5f / 16f, damageMultiplier = 1f, hitStop = 0.05f },
            new() { hitDelay = 1f / 16f, interval = 9f / 16f, damageMultiplier = 1.5f, hitStop = 0.09f },
        };
        [Tooltip("다음 타를 받는 시간(초). 다음 클릭 가능 시점부터 이 안에 누르면 다음 타, 지나면 1타부터")] public float comboWindow = 0.35f;
        [Tooltip("다음 클릭 가능 시점보다 이만큼(초) 먼저 누른 입력은 모아 두었다가 실행")] public float inputBuffer = 0.15f;
        [Tooltip("콤보 그림(attack1~3) 재생 속도(초당 프레임)")] public float comboFps = 16f;
    }

    [Serializable]
    public class SentinelStats
    {
        [Tooltip("최대 HP. 다음 등장부터 반영")] public int maxHp = 40;
        [Tooltip("공격 피해")] public int slashDamage = 8;
        [Tooltip("순찰 속도(m/초)")] public float patrolSpeed = 2f;
        [Tooltip("추격 속도(m/초)")] public float chaseSpeed = 3.5f;
        [Tooltip("플레이어 감지 거리(m)")] public float detectRange = 6f;
        [Tooltip("공격 시작 거리·베기 길이(m)")] public float attackRange = 1.2f;
        [Tooltip("예비 동작(초)")] public float windup = 0.5f;
        [Tooltip("공격 후 회복(초)")] public float recover = 1f;
        [Tooltip("피격 경직(초)")] public float hitStun = 0.2f;
    }

    [Serializable]
    public class AccumulationBossStats
    {
        [Tooltip("최대 HP. 다음 등장부터 반영")] public int maxHp = 300;
        [Header("축적·방출")]
        [Tooltip("축적 1단계 간격(초)")] public float stackInterval = 1.5f;
        [Tooltip("방출까지 필요한 축적 단계")] public int maxStacks = 5;
        [Tooltip("방출 예고 깜빡임(초)")] public float releaseWarning = 1f;
        [Tooltip("충격파 반경(m)")] public float shockwaveRadius = 3.5f;
        [Tooltip("충격파 피해")] public int shockwaveDamage = 30;
        [Tooltip("방출 후 탈진(초)")] public float exhaustTime = 2f;
        [Header("돌진")]
        [Tooltip("돌진 간격(초)")] public float chargeInterval = 3f;
        [Tooltip("돌진 예비 동작(초)")] public float chargeWindup = 0.4f;
        [Tooltip("돌진 속도(m/초)")] public float chargeSpeed = 12f;
        [Tooltip("돌진 거리(m)")] public float chargeDistance = 6f;
        [Tooltip("돌진 피해")] public int chargeDamage = 15;
    }

    [Serializable]
    public class SliceBossStats
    {
        [Tooltip("최대 HP. 다음 등장부터 반영")] public int maxHp = 400;
        [Tooltip("패턴 사이 대기(초, 최소~최대)")] public Vector2 idleRange = new(0.8f, 1.5f);
        [Tooltip("직전 패턴은 다음 선택에서 제외")] public bool excludePrevious = true;
        [Tooltip("역행으로 취소된 패턴은 다음 선택에서 제외")] public bool excludeCancelled = true;
        [Tooltip("패턴별 가중치·예고·실행·회복 시간·피해. 플레이 중에 항목 수를 바꾸지 말 것")]
        public List<SliceBoss.PatternSettings> patterns = new()
        {
            new() { pattern = SliceBoss.Pattern.Slash, telegraph = 0.6f, active = 0.2f, damage = 15 },
            new() { pattern = SliceBoss.Pattern.Charge, telegraph = 0.8f, active = 1f, damage = 20 },
            new() { pattern = SliceBoss.Pattern.Barrage, telegraph = 0.6f, active = 1.8f, damage = 12 },
            new() { pattern = SliceBoss.Pattern.Fall, telegraph = 1.2f, active = 1.5f, damage = 25 },
        };
        [Header("내려베기")]
        [Tooltip("판정 범위(앞쪽 길이, 폭)")] public Vector2 slashSize = new(3f, 2f);
        [Header("돌진")]
        [Tooltip("돌진 속도(m/초)")] public float chargeSpeed = 18f;
        [Header("탄막")]
        [Tooltip("탄 개수. 다음 등장부터 반영")] public int shotCount = 3;
        [Tooltip("발사 간격(초)")] public float shotInterval = 0.6f;
        [Tooltip("탄 속도(m/초)")] public float shotSpeed = 8f;
        [Tooltip("탄 반지름(m)")] public float shotRadius = 0.3f;
        [Tooltip("탄 수명(초)")] public float shotLifetime = 3f;
        [Header("낙하 광역")]
        [Tooltip("낙하 지점 수. 다음 등장부터 반영")] public int fallCount = 3;
        [Tooltip("낙하 범위 한 변(m)")] public float fallWidth = 2f;
        [Tooltip("착지 판정 구간: 실행 마지막 N초")] public float fallImpact = 0.2f;
    }

    [Serializable]
    public class DeferralBossStats
    {
        [Tooltip("최대 HP. 다음 등장부터 반영")] public int maxHp = 350;
        [Tooltip("좌우 이동 속도(m/초)")] public float moveSpeed = 1.5f;
        [Header("투사체")]
        [Tooltip("발사 간격(초)")] public float fireInterval = 2f;
        [Tooltip("투사체 속도(m/초)")] public float projectileSpeed = 8f;
        [Tooltip("직격 피해 (즉시)")] public int directDamage = 15;
        [Tooltip("투사체 크기(m)")] public float projectileSize = 0.4f;
        [Tooltip("최대 비행 거리(m)")] public float maxDistance = 40f;
        [Header("궤적 예약 · 피해 유예")]
        [Tooltip("궤적: 발동까지 대기·예고 깜빡임·폭·발동 지속(초/m)과 유예 피해")]
        public TrailSettings trail = new() { delay = 2f, warning = 0.3f, width = 0.6f, active = 0.3f, damage = 15 };
        [Tooltip("보스를 한 번 때릴 때 줄어드는 유예 피해")] public int reducePerHit = 15;
    }

    [Serializable]
    public class ReversalBossStats
    {
        [Tooltip("최대 HP. 다음 등장부터 반영")] public int maxHp = 400;
        [Tooltip("2단계 진입 HP 비율. 0.5 = 절반"), Range(0f, 1f)] public float phase2Ratio = 0.5f;
        [Tooltip("2단계 전환 연출(초)")] public float phaseTransition = 1f;
        [Tooltip("걷기 속도(m/초)")] public float walkSpeed = 1f;
        [Tooltip("플레이어와 유지하는 거리(m)")] public float keepDistance = 2f;
        [Header("고리 파동")]
        [Tooltip("발동 간격(초)")] public float waveInterval = 3.5f;
        [Tooltip("예비 동작(초)")] public float waveWindup = 0.6f;
        [Tooltip("퍼지는 속도(m/초)")] public float waveSpeed = 7f;
        [Tooltip("고리 두께(m)")] public float waveThickness = 1f;
        [Tooltip("최대 반경(m)")] public float waveMaxRadius = 12f;
        [Tooltip("피해 (퍼질 때·오므라들 때 각 1회)")] public int waveDamage = 20;
        [Header("역재생")]
        [Tooltip("최대 반경에서 멈추는 시간(초)")] public float wallPause = 0.5f;
        [Tooltip("오므라드는 속도(m/초)")] public float returnSpeed = 7f;
        [Header("위치 되감기 (2단계)")]
        [Tooltip("발동 간격(초, 최소~최대). 매번 이 범위에서 무작위")] public Vector2 rewindIntervalRange = new(6f, 10f);
        [Tooltip("잔상 예고(초)")] public float rewindWarning = 1f;
        [Tooltip("몇 초 전 위치로 되돌리는지")] public float rewindSeconds = 3f;
    }

    [Header("플레이어")]
    public PlayerStats player = new();
    [Header("플레이어 — 검")]
    public SwordStats sword = new();
    [Header("일반 적 — 밤의 파수꾼")]
    public SentinelStats sentinel = new();
    [Header("보스 1 — 시간 축적")]
    public AccumulationBossStats accumulationBoss = new();
    [Header("시험판 보스 (지금은 등장하지 않음)")]
    public SliceBossStats sliceBoss = new();
    [Header("보스 2 — 시간 유예")]
    public DeferralBossStats deferralBoss = new();
    [Header("보스 3 — 시간 역행")]
    public ReversalBossStats reversalBoss = new();

    static CombatBalance cached;

    public static CombatBalance Instance
    {
        get
        {
            if (cached) return cached;
            cached = Resources.Load<CombatBalance>(ResourcePath);
            if (!cached)
            {
                Debug.LogWarning($"Resources/{ResourcePath}.asset이 없어 기본값을 사용합니다. 메뉴 Tools/전투 밸런스 열기로 생성하세요.");
                cached = CreateInstance<CombatBalance>();
            }
            return cached;
        }
    }
}
