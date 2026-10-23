using System;
using UnityEngine;

// 태양핵: 기본은 고정 설치형, 유랑하는 태양이면 follow를 따라 이동. 공격하지 않음
// 역행 중에는 유지시간만 멈추고 되감지 않는다
public class SunCore : MonoBehaviour
{
    static readonly Color FieldColor = new(1f, 0.75f, 0.25f, 0.25f);
    static readonly Color AfterglowColor = new(1f, 0.5f, 0.3f, 0.15f);

    public float Radius { get; private set; }
    // 부서진 해시계의 잔광 영역 (공격·방어 보너스만 일부 유지)
    public bool IsAfterglow { get; private set; }
    // 태양의 성좌: 직전 프레임 플레이어 위치 안 여부, 이 태양핵에서의 발동 횟수
    public bool PlayerInside { get; set; }
    public int ConstellationCount { get; set; }
    // 유지시간이 끝나 소멸할 때만 호출 (방 이동·리셋으로 제거될 때는 호출하지 않음)
    public event Action<SunCore> Expired;

    float lifeLeft;
    Transform follow;

    public static SunCore Spawn(Vector2 position, float radius, float lifetime, Transform follow = null, bool afterglow = false)
    {
        var core = new GameObject(afterglow ? "SunAfterglow" : "SunCore").AddComponent<SunCore>();
        core.transform.position = position;
        core.Radius = radius;
        core.lifeLeft = lifetime;
        core.follow = follow;
        core.IsAfterglow = afterglow;

        var view = core.gameObject.AddComponent<SpriteRenderer>();
        view.sprite = AssetLibrary.SpriteUnitsOr(afterglow ? "fx_sun_afterglow" : "fx_sun_core", 2f, AccumulationBoss.CircleSprite());
        view.color = afterglow ? AfterglowColor : FieldColor;
        view.sortingOrder = -1;
        core.transform.localScale = Vector3.one * radius;
        return core;
    }

    public bool Contains(Vector2 point) => (point - (Vector2)transform.position).sqrMagnitude <= Radius * Radius;

    void Update()
    {
        if (follow) transform.position = follow.position;
        if (RewindSystem.IsFrozen) return;
        lifeLeft -= Time.deltaTime;
        if (lifeLeft > 0f) return;
        Expired?.Invoke(this);
        Destroy(gameObject);
    }
}
