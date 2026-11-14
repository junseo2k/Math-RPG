using NUnit.Framework;
using UnityEngine;

// 3타 콤보(2026-11-08 A-2): 단계 넘김·초기화, 동쪽 3연격 그림, 수치 순서
public class ComboTests
{
    [Test]
    public void StepsAdvanceWithinWindowAndWrapAfterThird()
    {
        Assert.AreEqual(0, PlayerAttack.NextStep(-1, 1f, 0f, 3), "처음은 1타");
        Assert.AreEqual(1, PlayerAttack.NextStep(0, 1f, 1.2f, 3), "마감 안: 1타 → 2타");
        Assert.AreEqual(2, PlayerAttack.NextStep(1, 1f, 1f, 3), "마감 시각 그대로: 2타 → 3타");
        Assert.AreEqual(0, PlayerAttack.NextStep(2, 1f, 1.2f, 3), "3타 다음은 1타");
    }

    [Test]
    public void LateClickRestartsCombo()
    {
        Assert.AreEqual(0, PlayerAttack.NextStep(1, 1.5f, 1.2f, 3));
    }

    [Test]
    public void EastComboStripsExistWithFrameCounts()
    {
        int[] frames = { 8, 8, 10 };
        for (int k = 0; k < 3; k++)
        {
            var tex = AssetLibrary.LoadTexture($"chr_player_attack{k + 1}_e");
            Assert.IsNotNull(tex, $"attack{k + 1}_e");
            Assert.AreEqual(144, tex.height, $"attack{k + 1}_e 칸 높이");
            Assert.AreEqual(frames[k], tex.width / tex.height, $"attack{k + 1}_e 프레임 수");
        }
    }

    [Test]
    public void NorthComboStripsExistWithFrameCounts()
    {
        int[] frames = { 8, 6, 12 };
        for (int k = 0; k < 3; k++)
        {
            var tex = AssetLibrary.LoadTexture($"chr_player_attack{k + 1}_n");
            Assert.IsNotNull(tex, $"attack{k + 1}_n");
            Assert.AreEqual(frames[k], tex.width / tex.height, $"attack{k + 1}_n 프레임 수");
        }
    }

    // 공격하며 움직여도 공격 그림이 다른 방향(그림 없으면 대기)으로 바뀌지 않고, 다음 재생부터 새 방향
    [Test]
    public void DirectionChangeWaitsUntilOneShotEnds()
    {
        var go = new GameObject("anim", typeof(SpriteRenderer));
        try
        {
            var anim = go.AddComponent<SpriteAnimator>();
            anim.BaseName = "chr_player";
            anim.SetDirection("e");
            anim.PlayOnce("attack1");
            anim.SetDirection("w");
            Assert.IsTrue(anim.Has("attack1"), "재생 중엔 동쪽 유지 → 동쪽 공격 그림 있음");
            anim.PlayOnce("attack2");
            Assert.IsFalse(anim.Has("attack1"), "다음 재생은 서쪽(공격 그림 없음)");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ComboTimingIsOrdered()
    {
        var sword = new CombatBalance.SwordStats();
        Assert.AreEqual(3, sword.combo.Length);
        foreach (var hit in sword.combo)
        {
            Assert.Less(hit.hitDelay, hit.interval, "판정이 다음 입력보다 먼저");
            Assert.Greater(hit.damageMultiplier, 0f);
            Assert.GreaterOrEqual(hit.hitStop, 0f);
        }
    }
}
