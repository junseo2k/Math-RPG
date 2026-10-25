using System.Collections;
using UnityEngine;

// 역행 보스 고리 파동: 보스 중심으로 퍼짐 → 최대 반경에서 멈춤(반시계 나선) → 보스 쪽으로 오므라듦. 규칙: Docs/Bosses/03_Reversal.md
[RequireComponent(typeof(LineRenderer))]
public class GroundWave : MonoBehaviour
{
    static readonly Color SpiralColor = new(0.3f, 0.9f, 0.9f, 0.6f);
    const int Segments = 48;

    float speed;
    float returnSpeed;
    float wallPause;
    int damage;
    float maxRadius;
    float thickness;
    Transform owner;
    Vector3 origin;
    LineRenderer line;
    float radius;
    bool returning;
    bool paused;
    bool hitThisPass;

    Vector3 Center => owner ? owner.position : origin;

    public void Init(float startRadius, float maxRadius, float thickness, float speed, float returnSpeed, float wallPause, int damage, Transform owner, Color color)
    {
        radius = startRadius;
        this.maxRadius = maxRadius;
        this.thickness = thickness;
        this.speed = speed;
        this.returnSpeed = returnSpeed;
        this.wallPause = wallPause;
        this.damage = damage;
        this.owner = owner;
        origin = owner.position;

        line = GetComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = Segments;
        line.widthMultiplier = thickness;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = color;
        line.sortingOrder = 2;
        Draw();
    }

    void Update()
    {
        if (paused) return;
        CheckHit();

        if (!returning)
        {
            radius += speed * Time.deltaTime;
            if (radius >= maxRadius)
            {
                radius = maxRadius;
                StartCoroutine(Reverse());
            }
        }
        else
        {
            radius -= returnSpeed * Time.deltaTime;
            if (radius <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }
        Draw();
    }

    void Draw()
    {
        Vector3 c = Center;
        for (int i = 0; i < Segments; i++)
        {
            float a = i * Mathf.PI * 2f / Segments;
            line.SetPosition(i, c + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }
    }

    IEnumerator Reverse()
    {
        paused = true;
        SoundPlayer.Play("sfx_rev_reverse");
        yield return Spiral(Center, wallPause, transform.parent);
        returning = true;
        hitThisPass = false;
        paused = false;
    }

    // 퍼질 때·오므라들 때 각각 1회. 대시 무적 중에 겹치면 회피 성공으로 그 회차 판정 끝
    void CheckHit()
    {
        if (hitThisPass) return;
        var player = FindAnyObjectByType<PlayerController>();
        if (!player || player.Health.IsDead) return;
        float slack = thickness * 0.5f + player.GetComponent<Collider2D>().bounds.extents.x;
        float d = Vector2.Distance(player.transform.position, Center);
        if (Mathf.Abs(d - radius) > slack) return;
        player.Health.TakeHit(damage, Center);
        hitThisPass = true;
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
