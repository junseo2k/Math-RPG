using UnityEngine;

namespace MathRPG
{
    public sealed class ForestClimaxUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        public ForestClimaxUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }

        public bool Draw(Transform card)
        {
            var encounter = game.Guardian;
            string title, body, button;
            UnityEngine.Events.UnityAction action;
            if (game.Mode == TrainingMode.GuardianAwakening)
            { title = "세 유물이 부르는 이름"; body = encounter.Definition.awakening; button = "수호 정령을 깨운다"; action = encounter.Awaken; }
            else if (game.Mode == TrainingMode.GuardianIntroduction)
            { title = encounter.Definition.bossName; body = encounter.Definition.introduction; button = "숲의 빛과 함께 싸운다"; action = encounter.StartCombat; }
            else if (game.Mode == TrainingMode.ForestChapterClear)
            { title = "숲이 기억한 약속"; body = encounter.Definition.victory; button = "인간 마을로 향하기"; action = game.Human.Enter; }
            else return false;
            ui.Label(card, "CHAPTER 02  /  " + (game.Mode == TrainingMode.ForestChapterClear ? "정령의 신뢰" : "깨어나는 숲"), 32, 24, 696, 28, 15);
            ui.Label(card, title, 32, 66, 696, 49, 29);
            ui.Label(card, body, 32, 129, 696, 166, 20, alignment: TextAnchor.UpperLeft);
            var data = encounter.Definition;
            string note = game.Mode == TrainingMode.ForestChapterClear ? game.Notice :
                "수호 정령의 힘은 전투 중 한 번만 사용 가능\n75%: 분열 제거·고정 피해 / 25%: 마무리 일격";
            ui.Label(card, note, 32, 305, 696, 57, 15, new Color(.74f, .91f, .69f));
            ui.Button(card, button, 32, 375, 430, 48, action, true);
            ui.Button(card, "마을에서 준비하기", 478, 375, 250, 48, game.Forest.ReturnToVillage);
            return true;
        }
    }
}
