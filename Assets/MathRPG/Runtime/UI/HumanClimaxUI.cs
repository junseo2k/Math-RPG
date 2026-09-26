using UnityEngine;

namespace MathRPG
{
    public sealed class HumanClimaxUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        public HumanClimaxUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }

        public string Objective => game.HumanClimax.Active
            ? "핵의 흐름 " + game.HumanClimax.SolvedCount + "/3 · 완성 시 추가 타격\n오답: 생명 피해·공격 강화 / 정답: 강화 완화"
            : game.Progress.human.sealTaken ? "친구에게 넘어간 인장\n품속의 답장은 아직 뜯지 않았다"
            : game.Progress.human.royalSealObtained ? "왕의 인장 획득 · 답장 미개봉\n오른쪽 길에서 E: 이어지는 이야기"
            : "오른쪽 길에서 E\n" + (game.Progress.human.disasterWitnessed ? "재난에 맞서기" : "도착한 소식 확인");

        public bool Draw(Transform card, string feedback)
        {
            var encounter = game.HumanClimax;
            if (game.Mode == TrainingMode.HumanClimaxChallenge)
            {
                var quiz = encounter.Quiz;
                if (quiz == null) return true;
                ui.ObserveQuiz(quiz);
                ui.Label(card, encounter.Definition.bossName + " / 전투 정지 · 제한시간 없음", 30, 18, 700, 26, 15);
                ui.Label(card, quiz.Title + " · " + quiz.Step.phase, 30, 56, 700, 36, 24);
                ui.Label(card, quiz.Step.prompt, 30, 102, 700, 78, 20, alignment: TextAnchor.UpperLeft);
                for (int i = 0; i < quiz.Step.choices.Length; i++)
                {
                    int index = i;
                    bool removed = quiz.IsEliminated(i);
                    ui.ChoiceButton(card, (i + 1) + "  " + (removed ? "제외한 보기" : quiz.Step.choices[i]),
                        30 + i % 2 * 357, 188 + i / 2 * 60, 338, 50, () => encounter.Submit(index), enabled: !removed);
                }
                ui.Label(card, "공격 강화 " + encounter.Rage + "/" + encounter.Definition.maxRage + " · 핵의 흐름 " + encounter.SolvedCount + "/3", 30, 312, 700, 25, 16);
                string hint = quiz.HintVisible ? "정령: " + quiz.Step.hint : "틀려도 다시 선택할 수 있어. H로 개념을 떠올려보자.";
                ui.Label(card, (string.IsNullOrEmpty(feedback) ? "" : feedback + "\n") + hint, 30, 347, 700, 100, 16, alignment: TextAnchor.UpperLeft);
                ui.Button(card, "H  정령의 도움 (" + game.Hints + ")", 30, 482, 338, 42, encounter.UseHint, enabled: !quiz.HintVisible && game.Hints > 0);
                ui.Button(card, "ESC  잠시 쉼", 390, 482, 338, 42, game.Pause);
                return true;
            }
            string title, body, action;
            UnityEngine.Events.UnityAction callback;
            if (game.Mode == TrainingMode.HumanLetter)
            { title = "늦게 도착한 답장"; body = encounter.Definition.letter; action = "봉투에 손을 댄다"; callback = encounter.InterruptLetter; }
            else if (game.Mode == TrainingMode.HumanDisaster)
            { title = "숲에서 밀려온 재난"; body = encounter.Definition.disaster; action = "재난에 맞선다"; callback = encounter.StartBoss; }
            else if (game.Mode == TrainingMode.HumanClimaxVictory)
            { title = "왕의 인장"; body = encounter.Definition.victory; action = "환호 속에서 고개를 든다"; callback = encounter.FinishVictory; }
            else return false;
            ui.Label(card, "CHAPTER 03 / 숲 깊은 곳", 32, 22, 696, 30, 15);
            ui.Label(card, title, 32, 68, 696, 48, 30);
            ui.Label(card, body, 32, 125, 696, 222, 19, alignment: TextAnchor.UpperLeft);
            if (!string.IsNullOrEmpty(feedback)) ui.Label(card, feedback, 32, 346, 696, 26, 13);
            if (game.Mode == TrainingMode.HumanDisaster)
            {
                ui.Button(card, action, 32, 375, 338, 48, callback, true);
                ui.Button(card, "마을에서 준비하기", 390, 375, 338, 48, game.Human.ReturnToVillage);
            }
            else ui.Button(card, action, 32, 375, 696, 48, callback, true);
            return true;
        }
    }
}
