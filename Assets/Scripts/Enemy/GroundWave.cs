using System.Collections;
using UnityEngine;

// 역행 보스 지면 파동: 전진 → 벽에서 멈춤(반시계 나선) → 보스 쪽으로 역재생. 규칙: Docs/Bosses/03_Reversal.md
public class GroundWave : MonoBehaviour
{
    static readonly Color SpiralColor = new(0.3f, 0.9f, 0.9f, 0.6f);

    int dir;
    float speed;
    float returnSpeed;
    float wallPause;
    int damage;
    Transform owner;
    float originX;
    bool returning;
    bool paused;
    bool hitThisPass;

    public void Init(int dir, float speed, float returnSpeed, float wallPause, int damage, Transform owner)
    {
        this.dir = dir;
        this.speed = speed;
        this.returnSpeed = returnSpeed;
        this.wallPause = wallPause;
        this.damage = damage;
        this.owner = owner;
        originX = transform.position.x;
    }

    void Update()
    {
        if (paused) return;
        CheckHit();

        if (!returning)
        {
            float step = speed * Time.deltaTime;
            Vector2 front = new(transform.position.x + dir * transform.localScale.x * 0.5f, transform.position.y);
            if (PatrolEnemy.HitsGround(front, Vector2.right * dir, step + 0.02f))
            {
                StartCoroutine(Reverse());
                return;
            }
            transform.position += Vector3.right * (dir * step);
            return;
        }

        float target = owner ? owner.position.x : originX;
        float x = Mathf.MoveTowards(transform.position.x, target, returnSpeed * Time.deltaTime);
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
        if (Mathf.Approximately(x, target)) Destroy(gameObject);
    }

    IEnumerator Reverse()
    {
        paused = true;
        SoundPlayer.Play("sfx_rev_reverse");
        yield return Spiral(transform.position, wallPause, transform.parent);
        returning = true;
        hitThisPass = false;
        paused = false;
    }

    // 가는 길·오는 길 각각 1회. 대시 무적 중이면 맞지 않음
    void CheckHit()
    {
        if (hitThisPass) return;
        foreach (var hit in Physics2D.OverlapBoxAll(transform.position, transform.localScale, 0f))
        {
            if (!hit.TryGetComponent(out PlayerController player) || player.Health.Invincible) continue;
            player.Health.TakeDamage(damage);
            hitThisPass = true;
            return;
        }
    }

    // 역행 예고: 반시계로 도는 표식 (최종 아트 전 임시)
    public static IEnumerator Spiral(Vector3 pos, float duration, Transform parent)
    {
        var sr = new GameObject("Spiral").AddComponent<SpriteRenderer>();
        sr.transform.SetParent(parent);
        sr.transform.position = pos;
        sr.transform.localScale = Vector3.one * 1.2f;
        sr.sprite = AssetLibrary.SpriteUnitsOr("fx_rev_spiral", 1f, RoomManager.Square);
        sr.color = SpiralColor;
        sr.sortingOrder = 3;

        float end = Time.time + duration;
        while (Time.time < end && sr)
        {
            sr.transform.Rotate(0f, 0f, 360f * Time.deltaTime);
            yield return null;
        }
        if (sr) Destroy(sr.gameObject);
    }
}
