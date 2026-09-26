using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MathRPG
{
    public sealed class RivalSecondUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        TextMeshProUGUI timer;
        RectTransform question;
        Vector2 questionOrigin;
        float shakeTime;
        public RivalSecondUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }

        public void Tick()
        {
            if (game.Mode != TrainingMode.RivalSecondChallenge) return;
            if (timer != null) timer.text = "남은 시간 " + game.RivalSecond.Remaining.ToString("0.0") + "초 · 압박 " + game.RivalSecond.Pressure + "/2";
            if (question == null) return;
            shakeTime += Time.unscaledDeltaTime;
            float amount = game.RivalSecond.Definition.maximumShake * game.RivalSecond.Pressure / 2f;
            question.anchoredPosition = questionOrigin + new Vector2(Mathf.Sin(shakeTime * 31), Mathf.Cos(shakeTime * 23)) * amount;
        }

        public bool Draw(Transform card, string feedback)
        {
            var encounter = game.RivalSecond;
            if (game.Mode == TrainingMode.RivalSecondChallenge)
            {
                var quiz = encounter.Quiz;
                if (quiz == null) return true;
                ui.ObserveQuiz(quiz);
                ui.Label(card, encounter.Definition.rivalName + " / 룬 대결", 30, 18, 700, 26, 16);
                timer = ui.Label(card, "", 30, 52, 700, 28, 17);
                ui.Label(card, quiz.Title + " · " + quiz.Step.phase, 30, 88, 700, 32, 23);
                question = ui.Label(card, quiz.Step.prompt, 30, 133, 700, 65, 21, alignment: TextAnchor.UpperLeft).rectTransform;
                questionOrigin = question.anchoredPosition;
                for (int i = 0; i < quiz.Step.choices.Length; i++)
                {
                    int index = i;
                    bool removed = quiz.IsEliminated(i);
                    ui.ChoiceButton(card, (i + 1) + "  " + (removed ? "제외한 보기" : quiz.Step.choices[i]), 30 + i % 2 * 357, 210 + i / 2 * 60, 338, 50, () => encounter.Submit(index), enabled: !removed);
                }
                string hint = quiz.HintVisible ? quiz.Step.hint : "정령: 네가 배운 식이야. 한 줄씩 보자.";
                ui.Label(card, (string.IsNullOrEmpty(feedback) ? "" : feedback + "\n") + hint, 30, 340, 700, 105, 16, alignment: TextAnchor.UpperLeft);
                ui.Button(card, "H  정령의 도움 (" + game.Hints + ")", 30, 480, 338, 42, encounter.UseHint, enabled: !quiz.HintVisible && game.Hints > 0);
                ui.Button(card, "ESC  잠시 쉼", 390, 480, 338, 42, game.Pause);
                var shade = new GameObject("Pressure shade", typeof(RectTransform), typeof(Image));
                shade.transform.SetParent(card, false);
                var rect = shade.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                var tint = shade.GetComponent<Image>();
                tint.color = new Color(0, 0, 0, encounter.Pressure * .07f); tint.raycastTarget = false;
                Tick();
                return true;
            }
            string title, body, action;
            UnityEngine.Events.UnityAction callback;
            if (game.Mode == TrainingMode.RivalSecondIntroduction)
            { title = "더 강한 자의 공적"; body = encounter.Definition.introduction; action = "돌려달라 — 결투한다"; callback = encounter.StartDuel; }
            else if (game.Mode == TrainingMode.RivalSecondBreakdown)
            { title = "움직이지 않는 손"; body = encounter.Definition.breakdown; action = "Q  룬을 발동하려 한다"; callback = encounter.FailRune; }
            else if (game.Mode == TrainingMode.RivalSecondDefeat)
            { title = "남겨진 사람"; body = encounter.Definition.defeat; action = "뛰쳐나간다"; callback = encounter.Depart; }
            else if (game.Mode == TrainingMode.HumanChapterComplete)
            { title = "행방불명"; body = encounter.Definition.departure; action = "챕터4로"; callback = game.BeginChapterFour; }
            else return false;
            ui.Label(card, "CHAPTER 03 / 인장과 답장", 32, 22, 696, 30, 15);
            ui.Label(card, title, 32, 68, 696, 48, 30);
            ui.Label(card, body, 32, 126, 696, 212, 19, alignment: TextAnchor.UpperLeft);
            ui.Label(card, feedback ?? "", 32, 341, 696, 28, 14);
            ui.Button(card, action, 32, 375, 696, 48, callback, true);
            return true;
        }
    }
}
