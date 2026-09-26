using UnityEngine;
using TMPro;

namespace MathRPG
{
    public sealed class RivalDuelUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        readonly Color cyan = new Color(.46f, .9f, .91f);
        readonly Color rose = new Color(.94f, .56f, .57f);
        TextMeshProUGUI timer, playerState, rivalState;

        public RivalDuelUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }

        public bool Draw(Transform card, string feedback)
        {
            var encounter = game.Rival;
            if (game.Mode == TrainingMode.RivalIntroduction)
            {
                ui.Label(card, "숲으로 이어지는 길  /  " + encounter.Definition.displayName, 32, 24, 696, 30, 15, rose);
                ui.Label(card, "먼저 떠난 사람", 32, 81, 696, 55, 32);
                ui.Label(card, encounter.Definition.introduction, 32, 160, 696, 139, 21, alignment: TextAnchor.UpperLeft);
                ui.Label(card, "같은 흐름을 읽을 때는 시간이 흐릅니다. ESC로 잠시 멈출 수 있습니다.", 32, 310, 696, 47, 15, cyan);
                ui.Button(card, "검을 든다", 32, 375, 340, 48, encounter.StartCombat, true);
                ui.Button(card, "첨탑으로 돌아가기", 388, 375, 340, 48, game.LeaveRivalEncounter);
                return true;
            }
            if (game.Mode == TrainingMode.ChapterClear)
            {
                ui.Label(card, "CHAPTER 01  /  결투를 넘어서", 32, 24, 696, 30, 15, cyan);
                ui.Label(card, "다음은, 정령의 숲", 32, 81, 696, 55, 32);
                ui.Label(card, encounter.Definition.victory, 32, 153, 696, 142, 21, alignment: TextAnchor.UpperLeft);
                ui.Label(card, game.Notice, 32, 307, 696, 47, 15, cyan);
                ui.Button(card, "정령의 숲으로", 32, 375, 340, 48, game.Forest.Enter, true);
                ui.Button(card, "첨탑으로 돌아가기", 388, 375, 340, 48, game.LeaveRivalEncounter);
                return true;
            }
            if (game.Mode == TrainingMode.RivalResult)
            {
                ui.Label(card, "맞부딪친 검기", 32, 25, 696, 48, 30);
                DrawPower(card, "내 룬", encounter.LastPlayerPower, 110, cyan);
                DrawPower(card, "라이벌의 룬", encounter.LastRivalPower, 205, rose);
                ui.Label(card, encounter.LastPlayerPower > 0 ? "같은 흐름을 읽었다. 두 검기의 울림은 달랐다." : "빛이 모이기 전에 검기가 밀려왔다. 다음 흐름은 놓치지 말자.", 32, 311, 696, 45, 18);
                ui.Button(card, "검을 다시 든다", 32, 375, 696, 48, encounter.ContinueCombat, true);
                return true;
            }
            if (game.Mode != TrainingMode.RivalChallenge) return false;
            var challenge = encounter.Challenge;
            var quiz = challenge.Quiz;
            ui.ObserveQuiz(quiz);
            timer = ui.Label(card, "", 30, 15, 690, 30, 16, cyan);
            playerState = ui.Label(card, "", 30, 48, 345, 29, 16, cyan);
            rivalState = ui.Label(card, "", 390, 48, 340, 29, 16, rose, TextAnchor.MiddleRight);
            ui.Label(card, quiz.Title + " · " + quiz.Step.phase, 30, 85, 695, 34, 22);
            ui.Label(card, challenge.PlayerReady ? "룬을 붙잡았다. 상대의 검기가 모이고 있다." : quiz.Step.prompt, 30, 127, 695, 66, 21, alignment: TextAnchor.UpperLeft);
            if (!challenge.PlayerReady)
                for (int i = 0; i < quiz.Step.choices.Length; i++)
                {
                    int index = i;
                    bool removed = quiz.IsEliminated(i);
                    ui.ChoiceButton(card, (i + 1) + "   " + (removed ? "흐름이 끊긴 길" : quiz.Step.choices[i]), 30 + i % 2 * 357, 201 + i / 2 * 63, 338, 53,
                        () => encounter.Submit(index), enabled: !removed);
                }
            string hint = quiz.HintVisible ? "정령: " + quiz.Step.hint : feedback ?? "기다리는 동안에도 제한시간은 흐릅니다.  [1~4] 선택  ·  [ESC] 쉼";
            ui.Label(card, hint, 30, 334, 695, 50, 16, cyan, TextAnchor.UpperLeft);
            ui.Button(card, "H  정령의 도움  (" + game.Hints + ")", 30, 394, 310, 39, encounter.UseHint,
                enabled: !challenge.PlayerReady && !quiz.HintVisible && game.Hints > 0);
            ui.Label(card, "조준  →  차징  →  발동", 370, 394, 360, 39, 16, cyan, TextAnchor.MiddleRight);
            Tick();
            return true;
        }

        void DrawPower(Transform card, string label, int power, float y, Color color)
        {
            ui.Label(card, label, 32, y, 490, 36, 22, color);
            ui.Label(card, power.ToString(), 560, y, 168, 36, 30, color, TextAnchor.MiddleRight);
            ui.Block(card, "Rune power", 32, y + 43, 696f * power / game.Rival.Definition.exchangePower, 12, color);
        }

        public void Tick()
        {
            if (game.Mode != TrainingMode.RivalChallenge || game.Rival.Challenge == null || timer == null) return;
            var challenge = game.Rival.Challenge;
            timer.text = "동시에 읽는 흐름   ·   남은 시간 " + challenge.Remaining.ToString("0.0") + "초";
            playerState.text = challenge.PlayerReady ? "내 룬  ·  발동 준비" : "내 룬  ·  흐름을 잇는 중";
            rivalState.text = challenge.RivalReady ? "라이벌  ·  발동 준비" : "라이벌  ·  " + Mathf.RoundToInt(challenge.RivalProgress * 100) + "%";
        }
    }
}
