using UnityEngine;

namespace MathRPG
{
    public sealed class HuntingGroundUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        public HuntingGroundUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }

        public bool Draw(Transform card, string feedback)
        {
            if (!game.InHunting) return false;
            var area = game.HuntingArea;
            if (game.Mode == TrainingMode.HuntingLesson)
            {
                int page = game.HuntingLessonPage;
                ui.Label(card, "CHAPTER " + (game.HuntingIndex / 3 + 1) + " / " + area.locationName, 32, 22, 696, 30, 15);
                ui.Label(card, page < 0 ? "이번 길에서 배울 것" : area.lessons[page].title, 32, 68, 696, 48, 28);
                ui.Label(card, page < 0 ? area.chapterIntroduction : area.lessons[page].explanation, 32, 130, 696, 115, 22, alignment: TextAnchor.UpperLeft);
                ui.PanelBackground(card, "Concept example", 28, 255, 704, 110, recessed: true);
                ui.Label(card, page < 0 ? "짧게 읽고, 몬스터와 싸우며 써보자.\n배운 내용은 L로 언제든 다시 볼 수 있다." : area.lessons[page].example,
                    44, 271, 672, 80, 21, alignment: TextAnchor.UpperLeft);
                ui.Label(card, feedback ?? (page < 0 ? "챕터 안내" : "개념 " + (page + 1) + " / " + area.lessons.Length), 32, 382, 696, 45, 16);
                ui.Button(card, page == area.lessons.Length - 1 ? "E  사냥터로" : "E  다음", 32, 466, 338, 48, game.AdvanceHuntingLesson, true);
                ui.Button(card, "돌아가기", 390, 466, 338, 48, () => game.LeaveHunting(false));
                return true;
            }
            if (game.Mode == TrainingMode.GameOver)
            {
                ui.Label(card, "다시 이어갈 수 있는 길", 32, 42, 696, 48, 29);
                ui.Label(card, "사냥터: " + area.locationName + "\n\n생명과 힌트를 회복하고 다시 시작합니다.\n완료한 사냥과 개념 안내 기록은 유지됩니다.", 32, 128, 696, 220, 22, alignment: TextAnchor.UpperLeft);
                ui.Button(card, "사냥터 재도전", 32, 466, 696, 48, game.RetryHunting, true);
                return true;
            }
            if (game.Mode == TrainingMode.Paused)
            {
                ui.Label(card, "잠시 숨 고르기", 32, 42, 696, 48, 29);
                ui.Label(card, game.HuntingObjective + "\n\nA/D 이동 · J 검격 · K 잔광 · Q 개념의 룬\nE 출입구 · L 개념 다시 보기", 32, 124, 696, 210, 21, alignment: TextAnchor.UpperLeft);
                ui.Label(card, feedback ?? "", 32, 360, 696, 50, 16);
                ui.Button(card, "계속", 32, 466, 220, 48, game.Resume, true);
                ui.Button(card, "개념 다시 보기", 266, 466, 220, 48, game.ReviewHuntingConcepts);
                ui.Button(card, "돌아가기", 500, 466, 228, 48, () => game.LeaveHunting(false));
                return true;
            }
            return false;
        }
    }
}
