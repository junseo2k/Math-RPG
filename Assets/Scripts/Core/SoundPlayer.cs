using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// BGM 전환과 효과음 재생. 파일이 없으면 무음. 규칙: Docs/SoundSpecification.md 2·3장
public class SoundPlayer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance) return;
        new GameObject("SoundPlayer").AddComponent<SoundPlayer>();
    }

    [SerializeField, Range(0f, 1f)] float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] float bgmVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 0.8f;
    [SerializeField] float crossfade = 1f;
    [SerializeField] int maxSameSfx = 2;
    [SerializeField] float pitchJitter = 0.05f;
    [SerializeField, Range(0f, 1f)] float duckVolume = 0.4f;

    static readonly string[] BossBgm = { "bgm_boss_accumulation", "bgm_boss_deferral", "bgm_boss_reversal" };

    // 위험 예고(우선순위 1): 울리는 동안 아래 낮은 우선순위 소리를 줄인다(사양 3-2)
    static readonly string[] Warnings =
        { "sfx_acc_warning", "sfx_acc_windup", "sfx_def_trail_warn", "sfx_rev_windup", "sfx_rev_rewind_warn", "sfx_enemy_windup" };
    static readonly string[] LowPriorityPrefixes = { "sfx_sword", "sfx_jump", "sfx_land", "sfx_dash", "sfx_ui" };

    static SoundPlayer instance;
    float duckUntil;

    AudioSource bgmA;
    AudioSource bgmB;
    bool aIsCurrent;
    string currentBgm = "";
    Coroutine fading;
    readonly Dictionary<string, int> playing = new();
    GameFlow flow;

    void Awake()
    {
        instance = this;
        bgmA = NewBgmSource();
        bgmB = NewBgmSource();
    }

    AudioSource NewBgmSource()
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.volume = 0f;
        return src;
    }

    // maxConcurrent: 같은 소리 동시 재생 한도(0이면 기본값)
    public static void Play(string name, int maxConcurrent = 0)
    {
        if (!instance) return;
        var clip = AssetLibrary.LoadClip(name);
        if (!clip) return;
        int limit = maxConcurrent > 0 ? maxConcurrent : instance.maxSameSfx;
        instance.playing.TryGetValue(name, out int count);
        if (count >= limit) return;
        instance.StartCoroutine(instance.PlayOne(name, clip));
    }

    IEnumerator PlayOne(string name, AudioClip clip)
    {
        playing.TryGetValue(name, out int count);
        playing[name] = count + 1;
        var src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        if (System.Array.IndexOf(Warnings, name) >= 0) duckUntil = Mathf.Max(duckUntil, Time.time + clip.length);
        bool ducked = Time.time < duckUntil && IsLowPriority(name);
        src.volume = masterVolume * sfxVolume * (ducked ? duckVolume : 1f);
        // 반복음 피로를 줄이려고 음높이를 조금씩 바꿈
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.Play();
        yield return new WaitForSeconds(clip.length / src.pitch);
        Destroy(src);
        playing[name]--;
    }

    static bool IsLowPriority(string name)
    {
        foreach (var prefix in LowPriorityPrefixes)
            if (name.StartsWith(prefix)) return true;
        return false;
    }

    void Update()
    {
        if (!flow) flow = FindAnyObjectByType<GameFlow>();
        if (!flow) return;

        string want = flow.State switch
        {
            GameState.AbilitySelect => "bgm_start",
            GameState.Playing => flow.IsBossStage ? BossTrack(flow.BossIndex) : "bgm_ch1",
            GameState.Cleared => "bgm_clear",
            _ => null, // 사망: 페이드 아웃 후 재탄생 시 bgm_start
        };
        if (want != currentBgm) SwitchBgm(want);
    }

    // 보스별 곡이 없으면 축적 보스 곡으로 대체
    static string BossTrack(int index)
    {
        string name = BossBgm[Mathf.Clamp(index, 0, BossBgm.Length - 1)];
        return AssetLibrary.LoadClip(name) ? name : BossBgm[0];
    }

    void SwitchBgm(string name)
    {
        currentBgm = name;
        var from = aIsCurrent ? bgmA : bgmB;
        var to = aIsCurrent ? bgmB : bgmA;
        aIsCurrent = !aIsCurrent;

        var clip = name != null ? AssetLibrary.LoadClip(name) : null;
        to.clip = clip;
        to.loop = name != "bgm_clear";
        if (clip) to.Play();

        if (fading != null) StopCoroutine(fading);
        fading = StartCoroutine(Crossfade(from, clip ? to : null));
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to)
    {
        float target = masterVolume * bgmVolume;
        float startFrom = from.volume;
        for (float t = 0f; t < crossfade; t += Time.deltaTime)
        {
            float k = t / crossfade;
            from.volume = Mathf.Lerp(startFrom, 0f, k);
            if (to) to.volume = Mathf.Lerp(0f, target, k);
            yield return null;
        }
        from.volume = 0f;
        from.Stop();
        if (to) to.volume = target;
    }
}
