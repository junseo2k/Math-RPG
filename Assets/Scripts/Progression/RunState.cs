using System;
using System.Collections.Generic;

// 판 단위 임시 상태. 재탄생 시 새 인스턴스로 교체(= 초기화)
[Serializable]
public class RunState
{
    public int stage;
    public int elapsedHours;
    public List<StartAbilityDef> choices = new();
    public StartAbilityDef selectedAbility;
}

// 재탄생해도 유지되는 상태. 시험판은 메모리에만 보관
[Serializable]
public class PersistentState
{
    public int day = 1;
    public int deathCount;
    public List<int> bossEncounters = new();
}
