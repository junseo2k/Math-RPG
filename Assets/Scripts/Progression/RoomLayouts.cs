using System;
using UnityEngine;

public class RoomLayout
{
    public string Name;
    public float Length;
    public float Height;
    public (Vector2 center, Vector2 size)[] Blocks = Array.Empty<(Vector2, Vector2)>();
    public Vector2[] Spawns = Array.Empty<Vector2>();
    // 19시 수로 장식(RoomDecor). 물길은 벽처럼 막히고, 메마른 패치는 그림만, 기둥은 2×2 막힘
    public (Vector2 center, Vector2 size)[] Water = Array.Empty<(Vector2, Vector2)>();
    public (Vector2 center, Vector2 size)[] Dry = Array.Empty<(Vector2, Vector2)>();
    public Vector2[] Pillars = Array.Empty<Vector2>();

    public Vector2 Entrance => new(-Length / 2f + 2f, 0f);
    public Vector2 ExitPos => new(Length / 2f - 1.5f, 0f);
}

// 좌표 기준: 방 중심 = (0, 0), 탑다운. 규칙: Docs/SliceA/NormalRoom.md 1-3
public static class RoomLayouts
{
    // 시작 지점·보스 방. 보스 이동 범위(±8.5, ±5)와 맞춤
    public static readonly RoomLayout Arena = new() { Name = "Arena", Length = 20f, Height = 12f };

    public static readonly RoomLayout[] Normal =
    {
        new()
        {
            Name = "A_Empty",
            Length = 32f,
            Height = 20f,
            Spawns = new Vector2[] { new(0f, 0f), new(4f, 5f), new(4f, -5f), new(9f, 2f), new(12f, -6f), new(12f, 6f) },
            Dry = new (Vector2, Vector2)[] { (new Vector2(-7f, -4f), new Vector2(5f, 4f)), (new Vector2(8f, 6f), new Vector2(4f, 3f)) },
        },
        new()
        {
            Name = "B_Pillars",
            Length = 36f,
            Height = 22f,
            Pillars = new Vector2[] { new(-4f, 4f), new(-4f, -4f), new(6f, 4f), new(6f, -4f) },
            Dry = new (Vector2, Vector2)[] { (new Vector2(1f, 0f), new Vector2(4f, 3f)) },
            Spawns = new Vector2[] { new(-1f, 0f), new(1f, 7f), new(1f, -7f), new(6f, 0f), new(11f, 5f), new(11f, -5f), new(14f, 0f), new(14f, 8f) },
        },
        new()
        {
            Name = "C_CenterWall",
            Length = 34f,
            Height = 22f,
            Water = new (Vector2, Vector2)[] { (new Vector2(2f, 0f), new Vector2(1f, 12f)) },
            Dry = new (Vector2, Vector2)[] { (new Vector2(-8f, 5f), new Vector2(4f, 4f)), (new Vector2(9f, -5f), new Vector2(5f, 3f)) },
            Spawns = new Vector2[] { new(-1f, 8f), new(-1f, -8f), new(6f, 0f), new(9f, 6f), new(9f, -6f), new(13f, 0f) },
        },
    };
}
