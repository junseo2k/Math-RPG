using System.Collections.Generic;
using UnityEngine;

// 역행 복원 지점보다 나중에 생긴 오브젝트는 역행하면 사라진다 (보스 투사체·궤적·파동 등)
public class RewindSpawned : MonoBehaviour
{
    static readonly List<RewindSpawned> all = new();

    float spawnedAt;

    void Awake()
    {
        spawnedAt = Time.time;
        all.Add(this);
    }

    void OnDestroy() => all.Remove(this);

    public static void DestroyAfter(float time)
    {
        foreach (var s in all)
            if (s.spawnedAt > time) Destroy(s.gameObject);
    }
}
