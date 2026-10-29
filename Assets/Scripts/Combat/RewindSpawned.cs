using System.Collections.Generic;
using UnityEngine;

// 소멸한 공격도 역행 기록에서 벗어날 때까지 보관한다.
public class RewindSpawned : MonoBehaviour, IRewindable
{
    static readonly List<RewindSpawned> all = new();

    float spawnedAt;
    float retiredAt = float.PositiveInfinity;
    GameFlow flow;

    public bool CanTick => !TimeControl.IsStopped && (!flow || flow.State == GameState.Playing);

    void Awake()
    {
        spawnedAt = Time.time;
        flow = FindAnyObjectByType<GameFlow>();
        all.Add(this);
        Rewindables.All.Add(this);
    }

    void OnDestroy()
    {
        all.Remove(this);
        Rewindables.All.Remove(this);
    }

    public object Capture() => gameObject.activeSelf;

    public void Restore(object state)
    {
        bool active = (bool)state;
        retiredAt = active ? float.PositiveInfinity : Time.time;
        gameObject.SetActive(active);
    }

    public void Despawn()
    {
        retiredAt = Time.time;
        gameObject.SetActive(false);
    }

    public static void ForgetBefore(float time)
    {
        foreach (var s in all)
            if (s.retiredAt < time) Destroy(s.gameObject);
    }

    public static void ClearAll()
    {
        foreach (var s in all)
        {
            s.gameObject.SetActive(false);
            Destroy(s.gameObject);
        }
    }

    public static void DestroyAfter(float time)
    {
        foreach (var s in all)
            if (s.spawnedAt > time)
            {
                s.gameObject.SetActive(false);
                Destroy(s.gameObject);
            }
    }
}
