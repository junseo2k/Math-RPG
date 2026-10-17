using System;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityKind { Active, Passive }

[Serializable]
public class StartAbilityDef
{
    public string id;
    public string displayName;
    public AbilityKind kind;
    [TextArea] public string description;
    // 시험용 임시 능력(UI에 [임시] 배지)
    public bool temporary;
    [Min(0f)] public float weight = 1f;
}

public class StartAbilityPool : MonoBehaviour
{
    // 시험판은 임시 3개. 본편에서 정식 10개로 교체
    [SerializeField] List<StartAbilityDef> candidates = new()
    {
        new() { id = "temp_a", displayName = "순간 정지장", kind = AbilityKind.Active, temporary = true,
                description = "S키: 주변 일반 적을 1.5초 동안 멈춘다.\n재사용 대기 8초. 보스에게는 통하지 않는다." },
        new() { id = "temp_b", displayName = "잔광", kind = AbilityKind.Passive, temporary = true,
                description = "대시 직후 1초 안에 휘두르는\n다음 검 공격 피해 +1." },
        new() { id = "temp_c", displayName = "가벼운 발걸음", kind = AbilityKind.Passive, temporary = true,
                description = "이동 속도 +20%.\n대시 재사용 대기 -30%." },
    };

    // 가중치 기반, 중복 없이 count개 추첨
    public List<StartAbilityDef> Draw(int count)
    {
        var pool = new List<StartAbilityDef>(candidates);
        var picked = new List<StartAbilityDef>();
        while (picked.Count < count && pool.Count > 0)
        {
            float total = 0f;
            foreach (var c in pool) total += c.weight;

            float roll = UnityEngine.Random.value * total;
            int i = 0;
            for (; i < pool.Count - 1; i++)
            {
                roll -= pool[i].weight;
                if (roll < 0f) break;
            }

            picked.Add(pool[i]);
            pool.RemoveAt(i);
        }
        return picked;
    }
}
