using UnityEngine;

namespace MathRPG
{
    public sealed class ForestVillageUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        readonly ForestClimaxUI climaxUI;
        public ForestVillageUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; climaxUI = new ForestClimaxUI(ui, game); }

        public string Objective
        {
            get
            {
                var forest = game.Forest;
                if (game.Guardian.Active)
                    return game.Guardian.Stage == AggregateStage.Core ? "중심 광폭화 " + game.Guardian.RageRemaining.ToString("0.0") + "초\n목표 피해를 주거나 R: 수호 정령" :
                        "집합체 " + game.Guardian.Stage + " · 독 " + game.Guardian.PoisonStacks + "/2" +
                        (game.Guardian.GuardianHelpAvailable ? "\nR: 수호 정령의 힘" : "");
                if (forest.BossActive) return forest.ChestActive ? "봉인 상자 문제 · 재도전 무제한" : forest.CombatName + "과 맞서기\n기믹 오답·시간 초과: 즉사";
                if (game.player.transform.position.x <= TrainingCourseView.EntranceX)
                    return forest.Progress.villageIndex == 0 ? "[E] 첨탑으로 돌아가기" : "[E] " + (forest.GetVillage(forest.Progress.villageIndex - 1)?.villageName ?? "이전 마을") + "로 돌아가기";
                if (forest.Progress.CurrentRelic)
                    return forest.Progress.villageIndex == 2 ? (forest.Progress.aggregateDefeated ? "챕터2 완료 · 정령의 신뢰\n오른쪽 문 E: 인간 마을로" : forest.Progress.guardianAwakened ? "수호 정령의 가호\n오른쪽 문 E: 집합체에 도전" : "세 유물 회수 완료 · 3 / 3\n오른쪽 문 E: 수호 정령 각성") : "유물 회수 완료\n오른쪽 문 E: " + (forest.GetVillage(forest.Progress.villageIndex + 1)?.villageName ?? "다음 마을");
                return forest.Progress.CurrentQuestAccepted ? "오른쪽 문에서 E\n" + (forest.Definition.encounterKind == ForestEncounterKind.Chest ? "봉인 상자 열기" : "관문으로 이동") : "왼쪽 " + forest.Definition.guideName + "에게 E로 대화";
            }
        }

        public bool Draw(Transform card, string feedback)
        {
            if (climaxUI.Draw(card)) return true;
            var forest = game.Forest;
            if (game.Mode == TrainingMode.ForestChallenge)
            {
                var quiz = forest.ActiveQuiz;
                if (quiz == null) return true;
                ui.ObserveQuiz(quiz);
                string state = game.Guardian.Active ? "체력 구간 기믹 · " + game.Guardian.ChallengeRemaining.ToString("0.0") + "초" :
                    forest.ChestActive ? "재도전 무제한" : forest.ChallengeTime.ToString("0.0") + "초";
                ui.Label(card, forest.CombatName + "  /  전투 정지 · " + state, 30, 16, 700, 28, 14,
                    game.Guardian.Active ? new Color(1f, .8f, .5f) : Color.white);
                ui.Label(card, quiz.Title + " · " + quiz.Step.phase, 30, 44, 700, 30, 21);
                if (quiz.Step.geometry != null && quiz.Step.geometry.figures != null && quiz.Step.geometry.figures.Length > 0)
                    GeometryDiagramUI.Draw(ui, card, quiz.Step.geometry);
                else if (quiz.Step.diagram != null) TriangleDiagramUI.Draw(ui, card, quiz.Step.diagram);
                ui.Label(card, quiz.Step.prompt, 30, 215, 700, 60, 21, alignment: TextAnchor.UpperLeft);
                for (int i = 0; i < quiz.Step.choices.Length; i++)
                {
                    int index = i;
                    bool removed = quiz.IsEliminated(i);
                    ui.ChoiceButton(card, (i + 1) + "  " + (removed ? "흐름이 끊긴 길" : quiz.Step.choices[i]),
                        30 + i % 2 * 357, 280 + i / 2 * 60, 338, 50, () => forest.Submit(index), enabled: !removed);
                }
                ui.Label(card, (feedback == null ? "" : feedback + "\n") + (quiz.HintVisible ? "정령: " + quiz.Step.hint : "식을 차례대로 읽자."),
                    30, 407, 700, 72, 16, alignment: TextAnchor.UpperLeft);
                ui.Button(card, "H  정령의 도움 (" + game.Hints + ")", 30, 494, 330, 42, forest.UseHint, enabled: !quiz.HintVisible && game.Hints > 0);
                ui.Button(card, "ESC  잠시 쉼", 390, 494, 338, 42, game.Pause);
                return true;
            }
            string title, body, action;
            UnityEngine.Events.UnityAction callback;
            if (game.Mode == TrainingMode.ForestArrival)
            { title = forest.Definition.villageName; body = forest.Definition.arrival; action = "마을로 들어가기"; callback = forest.FinishArrival; }
            else if (game.Mode == TrainingMode.ForestRequest)
            { title = forest.Definition.guideName; body = forest.Progress.CurrentRelic ? forest.Definition.reward : forest.Definition.request; action = "이야기를 마친다"; callback = forest.AcceptRequest; }
            else if (game.Mode == TrainingMode.ForestRelic)
            { title = forest.Definition.relicName + " · 획득"; body = forest.Definition.reward; action = "마을로 돌아가기"; callback = forest.ReturnToVillage; }
            else return false;
            ui.Label(card, "CHAPTER 02  /  정령의 숲", 32, 24, 696, 30, 15);
            ui.Label(card, title, 32, 76, 696, 52, 30);
            ui.Label(card, body, 32, 143, 696, 178, 21, alignment: TextAnchor.UpperLeft);
            ui.Label(card, game.Notice, 32, 324, 696, 38, 14);
            ui.Button(card, action, 32, 375, 696, 48, callback, true);
            return true;
        }
    }
}
