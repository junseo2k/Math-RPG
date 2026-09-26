using UnityEngine;

namespace MathRPG
{
    public sealed class HumanVillageUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        public HumanVillageUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }

        public string Objective
        {
            get
            {
                var town = game.Human;
                if (town.BossActive) return town.Definition.bossName + " 처치\n오답: 생명 피해 · H: 힌트";
                if (game.player.transform.position.x <= TrainingCourseView.EntranceX) return town.Progress.villageIndex == 0 ? "[E] 안개뜰로 돌아가기" : town.Progress.villageIndex == 1 ? "[E] 잿빛 나루로 돌아가기" : "[E] 바람고개로 돌아가기";
                if (town.Progress.CurrentBossDefeated) return town.Progress.villageIndex < 2
                    ? "오른쪽 길에서 E\n" + (town.Definition.nextVillage != null ? town.Definition.nextVillage.villageName + "로 이동" : "다음 마을 데이터 필요")
                    : "마을 의뢰 완료\n왼쪽 주민에게 E로 대화";
                return town.Progress.CurrentQuestAccepted ? "오른쪽 길에서 E\n" + town.Definition.bossName + "에게 향하기" : "왼쪽 " + town.Definition.residentName + "에게 E로 대화";
            }
        }

        public bool Draw(Transform card, string feedback)
        {
            var town = game.Human;
            if (game.Mode == TrainingMode.HumanChallenge)
            {
                var quiz = town.Quiz;
                if (quiz == null) return true;
                ui.ObserveQuiz(quiz);
                ui.Label(card, town.Definition.bossName + " / 전투 정지 · 제한시간 없음", 30, 18, 700, 26, 15);
                ui.Label(card, quiz.Title + " · " + quiz.Step.phase, 30, 62, 700, 36, 24);
                ui.Label(card, quiz.Step.prompt, 30, 111, 700, 77, 21, alignment: TextAnchor.UpperLeft);
                for (int i = 0; i < quiz.Step.choices.Length; i++)
                {
                    int index = i;
                    bool removed = quiz.IsEliminated(i);
                    ui.ChoiceButton(card, (i + 1) + "  " + (removed ? "흐름이 끊긴 길" : quiz.Step.choices[i]),
                        30 + i % 2 * 357, 200 + i / 2 * 61, 338, 51, () => town.Submit(index), enabled: !removed);
                }
                string hint = quiz.HintVisible ? "정령: " + quiz.Step.hint : "곱셈과 균형에서 배운 연결을 떠올려보자. 틀려도 다시 선택할 수 있습니다.";
                ui.Label(card, (feedback == null ? "" : feedback + "\n") + hint, 30, 325, 700, 66, 15, alignment: TextAnchor.UpperLeft);
                ui.Button(card, "H  정령의 도움 (" + game.Hints + ")", 30, 401, 338, 36, town.UseHint, enabled: !quiz.HintVisible && game.Hints > 0);
                ui.Button(card, "ESC  잠시 쉼", 390, 401, 338, 36, game.Pause);
                return true;
            }
            string title, body, action;
            UnityEngine.Events.UnityAction callback;
            if (game.Mode == TrainingMode.HumanArrival)
            { title = town.Definition.villageName; body = town.Definition.arrival; action = "마을로 들어가기"; callback = town.FinishArrival; }
            else if (game.Mode == TrainingMode.HumanRequest)
            { title = town.Definition.residentName; body = town.Progress.CurrentBossDefeated ? town.Definition.aftermath : town.Definition.request; action = town.Progress.CurrentBossDefeated ? "이야기를 마친다" : "의뢰를 맡는다"; callback = town.AcceptRequest; }
            else if (game.Mode == TrainingMode.HumanAftermath)
            { title = "잠시 열린 길"; body = town.Definition.aftermath; action = "마을로 돌아가기"; callback = town.ReturnToVillage; }
            else return false;
            ui.Label(card, "CHAPTER 03 / 잿빛 길", 32, 24, 696, 30, 15);
            ui.Label(card, title, 32, 77, 696, 50, 30);
            ui.Label(card, body, 32, 143, 696, 178, 20, alignment: TextAnchor.UpperLeft);
            ui.Label(card, feedback ?? game.Notice, 32, 324, 696, 38, 14);
            ui.Button(card, action, 32, 375, 696, 48, callback, true);
            return true;
        }
    }
}
