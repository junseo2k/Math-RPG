using UnityEngine;

namespace MathRPG
{
    public sealed class RivalSecondRematchUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        public RivalSecondRematchUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }
        public string Warning
        {
            get
            {
                var fight = game.SecondRematch;
                if (!fight.Active || game.Mode != TrainingMode.Playing) return "";
                if (fight.HazardActive) return (fight.LethalHazard ? "즉사 장판" : "룬 장판") + " · " + fight.HazardRemaining.ToString("0.0") + "초 — 표시 밖으로 이동!";
                if (fight.WindingUp) return fight.LowSweep ? "낮은 검격 — SPACE 점프" : "높은 검격 — S 숙이기";
                return "";
            }
        }

        public bool Draw(Transform card, string feedback)
        {
            if (!game.InSecondRematch) return false;
            var fight = game.SecondRematch;
            if (game.Mode == TrainingMode.SecondRematchChallenge) { DrawQuestion(card, feedback); return true; }
            string title, body, action;
            UnityEngine.Events.UnityAction callback;
            switch (game.Mode)
            {
                case TrainingMode.SecondRematchIntroduction:
                    title = "길을 막는 총사령관"; body = fight.Definition.introduction; action = "새로운 흐름을 짚는다"; callback = fight.Learn; break;
                case TrainingMode.SecondRematchLesson:
                    title = "지수와 로그"; body = fight.Definition.lesson; action = "내 의지로 맞선다"; callback = fight.StartDuel; break;
                case TrainingMode.SecondRematchPhase:
                    title = "물러설 수 없는 이유"; body = fight.Definition.phaseDialogue; action = "다시 검을 든다"; callback = fight.ContinuePhase; break;
                case TrainingMode.SecondRematchVictory:
                    title = "증명한 것은"; body = fight.Definition.victory; action = "열린 길로"; callback = fight.Depart; break;
                case TrainingMode.SecondRematchDeparture:
                    title = "재앙으로 향하는 길"; body = fight.Definition.departure; action = "재앙을 향해 간다"; callback = game.Calamity.Begin; break;
                case TrainingMode.GameOver when fight.Active:
                    title = "다시, 네가 선택한 자리로";
                    body = "정령: 서두르지 않아도 돼. 네가 잡았던 감각을 기억해.\n\n" + fight.DeathCause + "\n생명과 힌트를 회복하고 결투를 처음부터 다시 시작합니다.";
                    action = "결투 재도전"; callback = fight.StartDuel; break;
                case TrainingMode.Paused when fight.Active:
                    ui.Label(card, "잠시 숨 고르기", 32, 30, 696, 45, 29);
                    ui.Label(card, "A/D 이동 · SPACE 점프 · S 숙이기\nJ 검격 · K 잔광 · Q 룬 대결\n1~4 보기 · H 힌트\n\n문제와 장판, 검격이 모두 멈춰 있습니다.\n재도전하거나 기억을 불러오면 결투 진행은 초기화됩니다.", 32, 100, 696, 260, 21, alignment: TextAnchor.UpperLeft);
                    ui.Label(card, feedback ?? "", 32, 398, 696, 65, 15);
                    ui.Button(card, "계속", 32, 480, 220, 48, game.Resume, true);
                    ui.Button(card, "처음부터 재도전", 266, 480, 220, 48, fight.StartDuel);
                    ui.Button(card, "기억 불러오기", 500, 480, 228, 48, game.LoadCheckpoint);
                    return true;
                default: return false;
            }
            ui.Label(card, "CHAPTER 04 / 라이벌2 리매치", 32, 20, 696, 30, 15);
            ui.Label(card, title, 32, 65, 696, 50, 29);
            ui.Label(card, body, 32, 130, 696, 260, 20, alignment: TextAnchor.UpperLeft);
            ui.Label(card, feedback ?? "", 32, 402, 696, 64, 15, alignment: TextAnchor.UpperLeft);
            ui.Button(card, action, 32, 480, 696, 48, callback, true);
            return true;
        }

        void DrawQuestion(Transform card, string feedback)
        {
            var fight = game.SecondRematch;
            var quiz = fight.Quiz;
            if (quiz == null) return;
            ui.ObserveQuiz(quiz);
            ui.Label(card, "룬 대결 " + fight.QuestionNumber + "/3 · 제한시간 없음 · 전투 정지", 30, 18, 700, 27, 16);
            ui.Label(card, quiz.Title + " · " + quiz.Step.phase, 30, 59, 700, 35, 23);
            ui.Label(card, quiz.Step.prompt, 30, 110, 700, 75, 22, alignment: TextAnchor.UpperLeft);
            for (int i = 0; i < quiz.Step.choices.Length; i++)
            {
                int answer = i;
                bool eliminated = quiz.IsEliminated(i);
                ui.ChoiceButton(card, (i + 1) + "  " + (eliminated ? "제외한 보기" : quiz.Step.choices[i]), 30 + i % 2 * 357, 202 + i / 2 * 62, 338, 53,
                    () => fight.Submit(answer), enabled: !eliminated);
            }
            string text = quiz.HintVisible ? quiz.Step.hint : feedback ?? "정답: 좁은 장판·패턴 완화\n오답: 생명 피해·넓은 즉사 장판·패턴 강화\n답을 낸 뒤 장판이 보이면 A/D로 표시 밖까지 이동하세요.";
            ui.Label(card, text, 30, 339, 700, 117, 17, alignment: TextAnchor.UpperLeft);
            ui.Button(card, "H  힌트 (" + game.Hints + ")", 30, 480, 338, 44, fight.UseHint, enabled: !quiz.HintVisible && game.Hints > 0);
            ui.Button(card, "ESC  잠시 쉼", 390, 480, 338, 44, game.Pause);
        }
    }
}
