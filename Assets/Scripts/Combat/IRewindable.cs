using System.Collections.Generic;

// 시간 역행 대상. 생성 시 Rewindables.All에 등록, 파괴 시 해제. 규칙: Docs/CombatDecisions.md 3장
// 절대 시각 타이머(Time.time 기준)는 "남은 시간"으로 저장하고 복원 시 다시 절대 시각으로 바꾼다
public interface IRewindable
{
    object Capture();
    void Restore(object state);
}

public static class Rewindables
{
    public static readonly List<IRewindable> All = new();
}
