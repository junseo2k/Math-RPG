using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRPG
{
    public sealed class FinalCalamityUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        TextMeshProUGUI countdown;
        readonly List<ReplayFrame> replay = new List<ReplayFrame>();
        float recordDelay, replayTime;
        RectTransform replayPlayer, replayBoss, replayHazard, replaySword;
        struct ReplayFrame { public Vector2 player, boss; public float center, radius; public bool hazard, windup, low, crouched; }
        public FinalCalamityUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }
        public string Warning
        {
            get
            {
                var fight = game.Calamity;
                if (!fight.Active || game.Mode != TrainingMode.Playing) return "";
                if (fight.HazardActive) return "즉사 장판 " + fight.HazardRemaining.ToString("0.0") + "초 — 표시 밖으로!";
                if (fight.SpiritWarningRemaining > 0) return "정령화 예고 — 이동·회피 준비";
                if (fight.WindingUp) return fight.LowSweep ? "낮은 검격 — SPACE 점프" : "높은 검격 — S 숙이기";
                if (fight.Transformed) return "정령화 " + fight.SpiritRemaining.ToString("0.0") + "초 · 이동·회피만 가능";
                return "";
            }
        }

        public void Tick()
        {
            if (!game.InCalamity) return;
            var fight = game.Calamity;
            if (game.Mode == TrainingMode.CalamityChallenge && countdown != null)
            {
                countdown.text = (fight.TimeCrushed ? "시간 압축  " : "남은 시간  ") + fight.Remaining.ToString("0.0") + "초";
                countdown.color = fight.TimeCrushed ? new Color(1, .55f, .4f) : Color.white;
            }
            if (game.Mode == TrainingMode.CalamityFarewell || game.Mode == TrainingMode.CalamityIsolation)
            { replay.Clear(); recordDelay = replayTime = 0; }
            if (fight.Active && game.Mode == TrainingMode.Playing && Time.unscaledTime >= game.Effects.FreezeUntil)
            {
                recordDelay -= Time.deltaTime;
                if (recordDelay <= 0)
                {
                    recordDelay = .05f;
                    replay.Add(new ReplayFrame { player = game.player.transform.position, boss = game.enemy.transform.position,
                        center = fight.HazardX, radius = fight.HazardRadius, hazard = fight.HazardActive, windup = fight.WindingUp, low = fight.LowSweep, crouched = game.player.Dodges(false) });
                    if (replay.Count > 40) replay.RemoveAt(0);
                }
            }
            if (game.Mode != TrainingMode.GameOver || replayPlayer == null || replay.Count == 0) return;
            replayTime += Time.unscaledDeltaTime * .35f;
            int index = Mathf.Min(replay.Count - 1, Mathf.FloorToInt(replayTime / .05f));
            var frame = replay[index];
            float half = fight.Definition.arenaHalfWidth;
            float px = (frame.player.x / half + 1) * 330;
            float bx = (frame.boss.x / half + 1) * 330;
            float height = frame.crouched ? 11 : 21;
            replayPlayer.sizeDelta = new Vector2(10, height);
            replayPlayer.anchoredPosition = new Vector2(px - 5, -(98 - height - Mathf.Clamp(frame.player.y - PlayerMotor.GroundY, 0, 3) * 17));
            replayBoss.anchoredPosition = new Vector2(bx - 6, -60);
            replayHazard.gameObject.SetActive(frame.hazard);
            replayHazard.anchoredPosition = new Vector2(((frame.center - frame.radius) / half + 1) * 330, -28);
            replayHazard.sizeDelta = new Vector2(frame.radius / half * 660, 72);
            replaySword.gameObject.SetActive(frame.windup);
            float direction = frame.player.x < frame.boss.x ? -1 : 1;
            replaySword.anchoredPosition = new Vector2(direction < 0 ? bx - 60 : bx, frame.low ? -91 : -65);
        }

        public bool Draw(Transform card, string feedback)
        {
            if (!game.InCalamity) return false;
            var fight = game.Calamity;
            if (game.Mode == TrainingMode.CalamityChallenge) { DrawQuestion(card, feedback); return true; }
            if (game.Mode == TrainingMode.GameOver && fight.Active) { DrawDeath(card, feedback); return true; }
            if (game.Mode == TrainingMode.Paused && fight.Active)
            {
                ui.Label(card, "잠시 숨 고르기", 32, 30, 696, 45, 29);
                ui.Label(card, "A/D 이동 · SPACE 점프 · S 숙이기\nJ 검격 · K 잔광 · Q 룬 대결 · R 풀이 전환\n\n문제 제한시간·장판·정령화·마력 침식이 모두 멈췄습니다.\n\n전투 중간 기록은 남길 수 없습니다.\n재도전은 정령 격리 직전으로 돌아갑니다.", 32, 110, 696, 260, 20, alignment: TextAnchor.UpperLeft);
                ui.Label(card, feedback ?? "", 32, 400, 696, 65, 15);
                ui.Button(card, "계속", 32, 480, 338, 48, game.Resume, true);
                ui.Button(card, "격리 직전으로", 390, 480, 338, 48, fight.Retry);
                return true;
            }
            string title, body, action;
            UnityEngine.Events.UnityAction callback;
            switch (game.Mode)
            {
                case TrainingMode.CalamityIntroduction:
                    title = "뒤틀린 시간의 중심"; body = fight.Definition.introduction; action = "정령을 돌아본다"; callback = fight.Farewell; break;
                case TrainingMode.CalamityFarewell:
                    title = "마지막으로 남긴 기억"; body = fight.Definition.farewell; action = "재앙 앞에 선다"; callback = fight.Isolate; break;
                case TrainingMode.CalamityIsolation:
                    title = "홀로 남은 자리"; body = fight.Definition.isolation; action = "맞선다"; callback = fight.StartBattle; break;
                case TrainingMode.CalamityPhase:
                    title = "페이즈 " + fight.Phase; body = fight.Phase == 2 ? fight.Definition.phaseTwo : fight.Definition.phaseThree; action = "내 움직임을 믿는다"; callback = fight.ContinuePhase; break;
                case TrainingMode.CalamityVictory:
                    title = "재앙 격파"; body = fight.Definition.victory; action = "그 이후의 이야기"; callback = game.BeginEnding; break;
                default: return false;
            }
            ui.Label(card, "CHAPTER 04 / 최종보스", 32, 20, 696, 28, 15);
            ui.Label(card, title, 32, 65, 696, 48, 29);
            ui.Label(card, body, 32, 130, 696, 265, 20, alignment: TextAnchor.UpperLeft);
            ui.Label(card, feedback ?? "", 32, 402, 696, 64, 15, alignment: TextAnchor.UpperLeft);
            ui.Button(card, action, 32, 480, 696, 48, callback, true);
            return true;
        }

        void DrawQuestion(Transform card, string feedback)
        {
            var fight = game.Calamity;
            ui.Label(card, "룬 대결 " + fight.ProblemNumber + "/3 · 정령의 도움 없음", 30, 18, 440, 28, 15);
            countdown = ui.Label(card, "", 485, 18, 243, 28, 17, alignment: TextAnchor.MiddleRight);
            ui.Label(card, fight.Problem.title, 30, 60, 700, 36, 23);
            ui.Label(card, fight.UsingShortcut ? "식의 구조로 한 번에" : "직접 계산 " + fight.DirectStepNumber + "/" + fight.Problem.directSteps.Length, 30, 100, 700, 26, 15);
            ui.Label(card, fight.Question.prompt, 30, 138, 700, 65, 22, alignment: TextAnchor.UpperLeft);
            for (int i = 0; i < fight.Question.choices.Length; i++)
            {
                int answer = i;
                ui.ChoiceButton(card, (i + 1) + "  " + fight.Question.choices[i], 30 + i % 2 * 357, 218 + i / 2 * 62, 338, 52, () => fight.Submit(answer));
            }
            string thought = fight.UsingShortcut ? fight.Problem.insight : fight.InsightFound ? "이 식에는 더 짧은 길이 있다. R로 구조를 다시 보자." : "하나씩 계산할 수도 있다. 하지만 식 전체를 보면 어떨까?";
            ui.Label(card, (feedback == null ? "" : feedback + "\n") + thought, 30, 351, 700, 110, 17, alignment: TextAnchor.UpperLeft);
            ui.Button(card, "R  다른 풀이를 떠올린다", 30, 480, 338, 44, fight.Rethink, true, !fight.UsingShortcut);
            ui.Button(card, "ESC  잠시 쉼", 390, 480, 338, 44, game.Pause);
            Tick();
        }

        void DrawDeath(Transform card, string feedback)
        {
            replayTime = 0;
            ui.Label(card, "끊어진 순간", 32, 28, 696, 48, 29);
            ui.Label(card, game.Calamity.DeathCause, 32, 92, 696, 90, 21, alignment: TextAnchor.UpperLeft);
            ui.Label(card, "직전 움직임 · 느리게 보기  /  청록: 나  ·  보라: 기사  ·  빨강: 위험", 32, 202, 696, 28, 14);
            var strip = ui.PanelBackground(card, "Death replay", 50, 240, 660, 120, recessed: true).transform;
            ui.Block(strip, "Ground", 0, 99, 660, 2, new Color(.5f, .5f, .55f));
            replayHazard = ui.Block(strip, "Hazard", 0, 28, 1, 72, new Color(1, .2f, .2f, .35f)).rectTransform;
            replayBoss = ui.Block(strip, "Knight", 500, 60, 12, 38, new Color(.7f, .4f, .8f)).rectTransform;
            replayPlayer = ui.Block(strip, "Player", 200, 77, 10, 21, new Color(.4f, .9f, .9f)).rectTransform;
            replaySword = ui.Block(strip, "Sword warning", 400, 65, 60, 4, new Color(1, .4f, .2f)).rectTransform;
            replayHazard.gameObject.SetActive(false);
            replaySword.gameObject.SetActive(false);
            ui.Label(card, feedback ?? "정령이 마지막으로 남긴 격리 직전의 기록으로 돌아갑니다.\n보스의 생명·페이즈·문제 진행이 초기화됩니다.", 32, 386, 696, 74, 17, alignment: TextAnchor.UpperLeft);
            ui.Button(card, "격리 직전으로 돌아간다", 32, 480, 696, 48, game.Calamity.Retry, true);
            Tick();
        }
    }
}
