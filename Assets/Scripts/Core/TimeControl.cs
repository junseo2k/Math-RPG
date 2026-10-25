using System.Collections.Generic;
using UnityEngine;

// 시간 정지 요청을 모은다. 요청이 하나라도 남아 있으면 멈춘 상태. timeScale은 여기서만 쓴다
public static class TimeControl
{
    static readonly HashSet<object> holders = new();

    public static bool IsStopped => holders.Count > 0;

    public static void Stop(object holder)
    {
        holders.Add(holder);
        Time.timeScale = 0f;
    }

    public static void Release(object holder)
    {
        holders.Remove(holder);
        if (holders.Count == 0) Time.timeScale = 1f;
    }

    // 씬을 떠날 때 정리되지 않은 요청이 남아도 다음 씬은 정상 속도로 시작
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ResetOnSceneLoad() => UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) =>
    {
        holders.Clear();
        Time.timeScale = 1f;
    };
}
