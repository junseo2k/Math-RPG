using System;
using UnityEngine;

public class RoomLayout
{
    public string Name;
    public float Length;
    public (Vector2 center, Vector2 size)[] Blocks = Array.Empty<(Vector2, Vector2)>();
    public Vector2[] Spawns = Array.Empty<Vector2>();

    public float EntranceX => -Length / 2f + 2f;
    public float ExitX => Length / 2f - 1.5f;
}

// 좌표 기준: 방 중심 x = 0, 바닥 윗면 y = 0. 규칙: Docs/NormalRoom.md 1-3
public static class RoomLayouts
{
    // 시작 지점·보스 방
    public static readonly RoomLayout Arena = new() { Name = "Arena", Length = 20f };

    public static readonly RoomLayout[] Normal =
    {
        new()
        {
            Name = "A_Flat",
            Length = 48f,
            Spawns = new Vector2[] { new(0f, 0.5f), new(4f, 0.5f), new(8f, 0.5f), new(12f, 0.5f), new(16f, 0.5f), new(20f, 0.5f) },
        },
        new()
        {
            Name = "B_TwoTier",
            Length = 56f,
            Blocks = new (Vector2, Vector2)[]
            {
                (new Vector2(-2f, 1.75f), new Vector2(6f, 0.5f)),
                (new Vector2(6f, 3.75f), new Vector2(6f, 0.5f)),
                (new Vector2(14f, 1.75f), new Vector2(6f, 0.5f)),
            },
            Spawns = new Vector2[] { new(-4f, 0.5f), new(-2f, 2.5f), new(4f, 0.5f), new(6f, 4.5f), new(10f, 0.5f), new(14f, 2.5f), new(20f, 0.5f), new(24f, 0.5f) },
        },
        new()
        {
            Name = "C_Hill",
            Length = 44f,
            Blocks = new (Vector2, Vector2)[]
            {
                (new Vector2(6f, 0.5f), new Vector2(12f, 1f)),
                (new Vector2(6f, 1f), new Vector2(6f, 2f)),
            },
            Spawns = new Vector2[] { new(2f, 1.5f), new(6f, 2.5f), new(10f, 1.5f), new(14f, 0.5f), new(17f, 0.5f), new(19f, 0.5f) },
        },
    };
}
