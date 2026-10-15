using System.Collections.Generic;
using UnityEngine;

// 이름으로 자산을 찾는다. 없으면 null → 호출한 쪽은 임시 도형·무음을 유지한다.
// 위치·규칙: Docs/AssetProductionSheet.md 1장 (Resources/Art, Resources/Audio)
public static class AssetLibrary
{
    const float PixelsPerUnit = 32f;

    static readonly Dictionary<string, Texture2D> textures = new();
    static readonly Dictionary<string, AudioClip> clips = new();
    static readonly Dictionary<(string, float), Sprite> sprites = new();

    public static Texture2D LoadTexture(string name)
    {
        if (!textures.TryGetValue(name, out var tex)) textures[name] = tex = Resources.Load<Texture2D>("Art/" + name);
        return tex;
    }

    public static AudioClip LoadClip(string name)
    {
        if (!clips.TryGetValue(name, out var clip)) clips[name] = clip = Resources.Load<AudioClip>("Audio/" + name);
        return clip;
    }

    // 가로 스트립의 첫 프레임(정사각 프레임). ppu: 1유닛당 픽셀
    static Sprite FirstFrame(string name, float ppu)
    {
        var key = (name, ppu);
        if (sprites.TryGetValue(key, out var sprite)) return sprite;
        var tex = LoadTexture(name);
        if (tex)
        {
            float frame = Mathf.Min(tex.width, tex.height);
            sprite = Sprite.Create(tex, new Rect(0f, 0f, frame, tex.height), new Vector2(0.5f, 0.5f), ppu,
                0, SpriteMeshType.FullRect);
        }
        sprites[key] = sprite;
        return sprite;
    }

    static readonly Dictionary<(string, float), Sprite[]> frameSets = new();

    // 가로 스트립 전체 프레임. scale: 오브젝트 확대 배율 상쇄. 없으면 null
    public static Sprite[] LoadFrames(string name, float scale = 1f)
    {
        var key = (name, scale);
        if (frameSets.TryGetValue(key, out var frames)) return frames;
        var tex = LoadTexture(name);
        if (tex)
        {
            int size = Mathf.Min(tex.width, tex.height);
            frames = new Sprite[Mathf.Max(1, tex.width / size)];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = Sprite.Create(tex, new Rect(i * size, 0f, size, tex.height), new Vector2(0.5f, 0.5f),
                    PixelsPerUnit * Mathf.Max(scale, 0.0001f), 0, SpriteMeshType.FullRect);
        }
        frameSets[key] = frames;
        return frames;
    }

    // PPU 32 기준 실제 픽셀 크기. UI나 크기 조절 없는 오브젝트용
    public static Sprite LoadSprite(string name) => FirstFrame(name, PixelsPerUnit);

    // 프레임 폭이 unitsWide 유닛이 되게(크기를 scale로 정하는 이펙트용)
    public static Sprite LoadSpriteUnits(string name, float unitsWide)
    {
        var tex = LoadTexture(name);
        return tex ? FirstFrame(name, Mathf.Min(tex.width, tex.height) / unitsWide) : null;
    }

    public static Sprite SpriteOr(string name, Sprite fallback) => LoadSprite(name) ?? fallback;

    public static Sprite SpriteUnitsOr(string name, float unitsWide, Sprite fallback) => LoadSpriteUnits(name, unitsWide) ?? fallback;

    // 크기를 조절한 캐릭터·보스: 확대 배율을 상쇄해 실제 픽셀 크기로 표시. 적용되면 색을 흰색으로(임시 색 제거)
    public static bool ApplyCharacter(SpriteRenderer view, string name)
    {
        float scale = Mathf.Max(Mathf.Abs(view.transform.lossyScale.x), 0.0001f);
        var sprite = FirstFrame(name, PixelsPerUnit * scale);
        if (!sprite) return false;
        view.sprite = sprite;
        view.color = Color.white;
        return true;
    }
}
