using UnityEngine;

// 유예 보스 투사체. 직격은 즉시 피해, 멈춘 뒤 지나간 자리에 궤적 예약(TrailStrike)을 남긴다
public class DeferredProjectile : MonoBehaviour
{
    Vector2 direction;
    float speed;
    int damage;
    float maxDistance;
    TrailSettings trail;
    Vector2 start;
    float traveled;
    bool done;

    public void Init(Vector2 dir, float speed, int damage, float maxDistance, TrailSettings trail)
    {
        direction = dir;
        this.speed = speed;
        this.damage = damage;
        this.maxDistance = maxDistance;
        this.trail = trail;
        start = transform.position;
    }

    void Update()
    {
        if (done) return;
        Vector2 pos = transform.position;
        float step = speed * Time.deltaTime;
        float radius = transform.localScale.x * 0.5f;

        foreach (var hit in Physics2D.CircleCastAll(pos, radius, direction, step))
        {
            if (hit.collider.TryGetComponent(out PlayerController player))
            {
                // 대시 무적 중이면 통과
                if (player.Health.Invincible) continue;
                player.Health.TakeDamage(damage);
                Finish(hit.centroid);
                return;
            }
            // 지형(리지드바디 없는 고체 콜라이더)에서 멈춤
            if (!hit.collider.isTrigger && !hit.rigidbody)
            {
                Finish(hit.centroid);
                return;
            }
        }

        transform.position = pos + direction * step;
        traveled += step;
        if (traveled >= maxDistance) Finish(transform.position);
    }

    void Finish(Vector2 end)
    {
        done = true;
        if ((end - start).sqrMagnitude > 0.01f)
        {
            var go = new GameObject("TrailStrike");
            go.transform.SetParent(transform.parent);
            go.AddComponent<TrailStrike>().Init(start, end, trail);
        }
        Destroy(gameObject);
    }
}
