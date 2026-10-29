using System;
using UnityEngine;

// 19시 수로 장식: 물길·메마른 패치, 기둥, 등불. 규칙: Docs/Art/Ch1_19_SacredAqueduct.md
// 물길·메마른 패치는 칸 꼭짓점 4개가 영역 안인지 보고 경계 그림 16종 중 하나를 고른다.
// 그림이 없으면 충돌만 만들고 그림은 건너뛴다.
public static class RoomDecor
{
    const int RegionOrder = -9;     // 바닥(-10) 바로 위
    const int BaseOrder = 0;        // 캐릭터(1) 아래
    const int TopOrder = 2;         // 캐릭터 위: 기둥 윗부분·등불은 뒤에 선 캐릭터를 가린다

    public static void Build(RoomLayout layout, Transform room)
    {
        foreach (var (center, size) in layout.Water) Collider("Water", center, size, room);
        Region(layout.Water, "tile_water_ch1", room);
        Region(layout.Dry, "tile_dry_ch1", room);
        foreach (var center in layout.Pillars) Pillar(center, room);
        Lamps(layout, room);
    }

    // 경계 그림 번호 = NW<<3 | NE<<2 | SW<<1 | SE (영역 안 꼭짓점 = 1). tile_*_ch1 스트립의 칸 순서
    public static int CornerMask(Func<Vector2, bool> inside, Vector2 cellMin)
    {
        int mask = 0;
        if (inside(cellMin + Vector2.up)) mask |= 8;
        if (inside(cellMin + Vector2.one)) mask |= 4;
        if (inside(cellMin)) mask |= 2;
        if (inside(cellMin + Vector2.right)) mask |= 1;
        return mask;
    }

    public static bool Contains((Vector2 center, Vector2 size)[] rects, Vector2 p)
    {
        foreach (var (c, s) in rects)
            if (Mathf.Abs(p.x - c.x) <= s.x / 2f + 0.01f && Mathf.Abs(p.y - c.y) <= s.y / 2f + 0.01f) return true;
        return false;
    }

    static void Region((Vector2 center, Vector2 size)[] rects, string art, Transform room)
    {
        if (rects.Length == 0) return;
        var frames = AssetLibrary.LoadFrames(art);
        if (frames == null || frames.Length < 16) return;

        Vector2 min = rects[0].center, max = min;
        foreach (var (c, s) in rects)
        {
            min = Vector2.Min(min, c - s / 2f);
            max = Vector2.Max(max, c + s / 2f);
        }
        for (int x = Mathf.FloorToInt(min.x) - 1; x <= Mathf.CeilToInt(max.x); x++)
        for (int y = Mathf.FloorToInt(min.y) - 1; y <= Mathf.CeilToInt(max.y); y++)
        {
            int mask = CornerMask(p => Contains(rects, p), new Vector2(x, y));
            if (mask == 0) continue;
            Place(art, new Vector2(x + 0.5f, y + 0.5f), frames[mask], RegionOrder, room);
        }
    }

    // 2×2 밑동(충돌) + 위로 1칸 더 솟은 윗부분. 그림 64×96px
    static void Pillar(Vector2 center, Transform room)
    {
        Collider("Pillar", center, new Vector2(2f, 2f), room);
        var tex = AssetLibrary.LoadTexture("obj_pillar_ch1");
        if (!tex) return;
        var half = new Vector2(0.5f, 0.5f);
        Place("PillarBase", center, UnityEngine.Sprite.Create(tex, new Rect(0, 0, 64, 64), half, 32f), BaseOrder, room);
        Place("PillarTop", center + new Vector2(0f, 1.5f), UnityEngine.Sprite.Create(tex, new Rect(0, 64, 64, 32), half, 32f), TopOrder, room);
    }

    // 윗벽 앞(밑동이 방 안쪽 0.5칸), 방 너비의 1/4·3/4 지점. 충돌 없음
    static void Lamps(RoomLayout layout, Transform room)
    {
        if (AssetLibrary.LoadFrames("obj_lamp_ch1_idle") == null) return;
        float y = layout.Height / 2f + 0.5f;
        foreach (float x in new[] { -layout.Length / 4f, layout.Length / 4f })
        {
            var go = Place("Lamp", new Vector2(x, y), null, TopOrder, room);
            var anim = go.AddComponent<SpriteAnimator>();
            anim.BaseName = "obj_lamp_ch1";
            anim.Fps = 6f;
        }
    }

    static GameObject Place(string name, Vector2 pos, Sprite sprite, int order, Transform room)
    {
        var go = new GameObject(name);
        go.transform.SetParent(room);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    static void Collider(string name, Vector2 center, Vector2 size, Transform room)
    {
        var go = new GameObject(name);
        go.transform.SetParent(room);
        go.transform.position = center;
        go.AddComponent<BoxCollider2D>().size = size;
    }
}
