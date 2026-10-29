using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 사망 → 대기 → 재탄생 연출. 규칙: Docs/Art/ArtSpecification.md 5-2
// 되감기가 아니라 '다음 날'임을 보여준다(사망 ≠ 시간 역행)
public class RebirthOverlay : MonoBehaviour
{
    [SerializeField, Min(0f)] float deathPause = 1f;
    public float DeathPause => deathPause;
    [SerializeField] float lightOut = 0.4f;  // 주인공 빛이 꺼짐
    [SerializeField] float blackout = 0.6f;  // 암전
    [SerializeField] float dayCard = 0.6f;   // "N일째 18:00"
    [SerializeField] float sunrise = 0.4f;   // 태양이 떠오름
    [SerializeField] float sunFromY = -240f; // 태양 시작·끝 높이(화면 아래 기준)
    [SerializeField] float sunToY = 120f;

    [Header("화면 요소 (생성 시 자동 연결)")]
    [SerializeField] Image black;
    [SerializeField] TextMeshProUGUI dayText;
    [SerializeField] Image sun;

    GameFlow flow;
    GameState lastState;

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();
        EnsureBuilt();
        if (!sun.sprite) sun.sprite = AccumulationBoss.CircleSprite();
        black.color = new Color(0f, 0f, 0f, 0f);
        dayText.gameObject.SetActive(false);
        sun.gameObject.SetActive(false);
    }

    public void EnsureBuilt()
    {
        if (black) return;
        black = GameUI.MakeImage(transform, "Black", new Color(0f, 0f, 0f, 0f));
        GameUI.Fill(black.rectTransform);
        black.raycastTarget = false;

        dayText = GameUI.MakeText(transform, "N일째   18:00", 64f, GameUI.Gold);
        dayText.name = "DayText";
        GameUI.Place(dayText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 100f));

        sun = GameUI.MakeImage(transform, "Sun", GameUI.Gold);
        GameUI.Place(sun.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(240f, 240f));
        sun.raycastTarget = false;
    }

    void Update()
    {
        if (!flow) return;
        if (flow.State == GameState.Dead && lastState != GameState.Dead) StartCoroutine(Play());
        lastState = flow.State;
    }

    IEnumerator Play()
    {
        var player = FindAnyObjectByType<PlayerController>();
        var view = player ? player.GetComponent<SpriteRenderer>() : null;

        yield return new WaitForSeconds(deathPause);
        yield return Tween(lightOut, t => SetAlpha(view, 1f - t));
        yield return Tween(blackout, t => black.color = new Color(0f, 0f, 0f, t));

        // 재탄생 시 일차가 1 늘어난다
        dayText.text = $"{flow.Persistent.day + 1}일째   18:00";
        dayText.gameObject.SetActive(true);
        yield return new WaitForSeconds(dayCard);
        dayText.gameObject.SetActive(false);

        sun.gameObject.SetActive(true);
        SoundPlayer.Play("sfx_rebirth");
        SetAlpha(view, 1f);
        yield return Tween(sunrise, t =>
        {
            sun.rectTransform.anchoredPosition = new Vector2(sun.rectTransform.anchoredPosition.x, Mathf.Lerp(sunFromY, sunToY, t));
            black.color = new Color(0f, 0f, 0f, 1f - t);
        });
        sun.gameObject.SetActive(false);
        black.color = new Color(0f, 0f, 0f, 0f);
    }

    static IEnumerator Tween(float duration, Action<float> apply)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            apply(t / duration);
            yield return null;
        }
        apply(1f);
    }

    static void SetAlpha(SpriteRenderer view, float alpha)
    {
        if (!view) return;
        var c = view.color;
        view.color = new Color(c.r, c.g, c.b, alpha);
    }
}
