using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 사망 → 재탄생 연출. 총 길이 = 재탄생 대기(2초). 규칙: Docs/ArtSpecification.md 5-2
// 되감기가 아니라 '다음 날'임을 보여준다(사망 ≠ 시간 역행)
public class RebirthOverlay : MonoBehaviour
{
    [SerializeField] float lightOut = 0.4f;  // 주인공 빛이 꺼짐
    [SerializeField] float blackout = 0.6f;  // 암전
    [SerializeField] float dayCard = 0.6f;   // "N일째 18:00"
    [SerializeField] float sunrise = 0.4f;   // 태양이 떠오름

    GameFlow flow;
    Image black;
    TextMeshProUGUI dayText;
    Image sun;
    GameState lastState;

    void Start()
    {
        flow = FindAnyObjectByType<GameFlow>();

        black = GameUI.MakeImage(transform, "Black", new Color(0f, 0f, 0f, 0f));
        GameUI.Fill(black.rectTransform);
        black.raycastTarget = false;

        dayText = GameUI.MakeText(transform, "", 64f, GameUI.Gold);
        GameUI.Place(dayText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 100f));

        sun = GameUI.MakeImage(transform, "Sun", GameUI.Gold, AccumulationBoss.CircleSprite());
        GameUI.Place(sun.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(240f, 240f));
        sun.raycastTarget = false;

        dayText.gameObject.SetActive(false);
        sun.gameObject.SetActive(false);
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

        yield return Tween(lightOut, t => SetAlpha(view, 1f - t));
        yield return Tween(blackout, t => black.color = new Color(0f, 0f, 0f, t));

        // 재탄생 시 일차가 1 늘어난다
        dayText.text = $"{flow.Persistent.day + 1}일째   18:00";
        dayText.gameObject.SetActive(true);
        yield return new WaitForSeconds(dayCard);
        dayText.gameObject.SetActive(false);

        sun.gameObject.SetActive(true);
        SetAlpha(view, 1f);
        yield return Tween(sunrise, t =>
        {
            sun.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-240f, 120f, t));
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
