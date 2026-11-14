using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// 보스 = 다음 시각의 관문: 보스 방 X:59, 처치하면 정각 (WorldLevelStructure 5절)
public class ClockTests
{
    [Test]
    public void BossRoomHoldsClockAtFiftyNine()
    {
        var go = new GameObject("ClockTestFlow");
        try
        {
            var flow = go.AddComponent<GameFlow>();
            var enter = typeof(GameFlow).GetMethod("EnterStage", BindingFlags.Instance | BindingFlags.NonPublic);

            enter.Invoke(flow, new object[] { 2 });
            Assert.AreEqual("18:00", flow.TimeText);

            enter.Invoke(flow, new object[] { 3 });
            Assert.AreEqual("18:59", flow.TimeText);
            Assert.AreEqual(18f + 59f / 60f, flow.DialHour, 1e-4f);

            // 처치 = ClearStage의 시계 부분
            flow.Run.clockHeld = false;
            flow.Run.elapsedHours = 1;
            Assert.AreEqual("19:00", flow.TimeText);

            enter.Invoke(flow, new object[] { 6 });
            Assert.AreEqual("19:59", flow.TimeText);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
