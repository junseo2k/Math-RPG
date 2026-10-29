using NUnit.Framework;
using UnityEngine;

public class RoomDecorTests
{
    static readonly (Vector2, Vector2)[] Channel = { (new Vector2(2f, 0f), new Vector2(1f, 12f)) };

    static int Mask(float x, float y) => RoomDecor.CornerMask(p => RoomDecor.Contains(Channel, p), new Vector2(x, y));

    [Test]
    public void OneWideChannelSplitsAcrossNeighbourCells()
    {
        // 물길 x 1.5~2.5: 꼭짓점 x=2만 안 → 왼쪽 칸은 오른쪽 두 꼭짓점, 오른쪽 칸은 왼쪽 두 꼭짓점
        Assert.AreEqual(4 | 1, Mask(1f, 0f));
        Assert.AreEqual(8 | 2, Mask(2f, 0f));
        Assert.AreEqual(0, Mask(4f, 0f));
    }

    [Test]
    public void ChannelEndsUseHalfTiles()
    {
        // 위쪽 끝 y=6: 칸 (1,6)은 아래 오른쪽 꼭짓점만, 칸 (1,5)는 오른쪽 위·아래 모두
        Assert.AreEqual(1, Mask(1f, 6f));
        Assert.AreEqual(4 | 1, Mask(1f, 5f));
    }

    [Test]
    public void LayoutStripsHaveSixteenTiles()
    {
        foreach (var name in new[] { "tile_water_ch1", "tile_dry_ch1" })
            Assert.AreEqual(16, AssetLibrary.LoadFrames(name)?.Length ?? 0, name);
    }
}
