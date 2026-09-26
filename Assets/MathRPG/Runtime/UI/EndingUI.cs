using UnityEngine;
using UnityEngine.UI;

namespace MathRPG
{
    public sealed class EndingUI
    {
        readonly TrainingUI ui;
        readonly TrainingGame game;
        CanvasGroup sceneFade;
        RectTransform movingActor, trainingBlade;
        Vector2 moveFrom, moveTo;
        Button advance;

        public EndingUI(TrainingUI ui, TrainingGame game) { this.ui = ui; this.game = game; }

        public void Tick()
        {
            if (!game.EndingScreen) return;
            if (advance != null) advance.interactable = game.CanAdvanceEnding;
            float elapsed = game.EndingElapsed;
            if (sceneFade != null) sceneFade.alpha = Mathf.Clamp01(elapsed / Mathf.Clamp(game.Ending.fadeDuration, .2f, 3));
            if (movingActor != null)
                movingActor.anchoredPosition = Vector2.Lerp(moveFrom, moveTo, Mathf.SmoothStep(0, 1, elapsed / Mathf.Clamp(game.Ending.motionDuration, .5f, 4)));
            if (trainingBlade != null) trainingBlade.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 1.5f) * 12);
        }

        public void Draw(Transform parent, string feedback)
        {
            var beat = game.CurrentEndingBeat;
            var page = game.Ending.Page(beat);
            movingActor = trainingBlade = null;
            bool complete = beat == EndingBeat.Complete;
            ui.Label(parent, complete ? "수학 전투 RPG / END" : "EPILOGUE  /  " + (int)beat + " · 7", 100, 20, 1080, 25, 14, game.Ending.gold);
            ui.Label(parent, page.title, 100, 55, 1080, 46, 31);
            var scene = ui.Block(parent, "Ending scene", 100, 117, 1080, 200, page.backdrop).transform;
            sceneFade = scene.gameObject.AddComponent<CanvasGroup>();
            DrawScene(scene, beat);
            ui.Label(parent, page.body, 100, 341, 1080, 211, 20, alignment: TextAnchor.UpperLeft);
            ui.Label(parent, feedback ?? (game.EndingReplay ? "엔딩 다시보기 · 완료 기록은 유지됩니다." : complete ? "엔딩 완료 기록이 저장되었습니다." : "장면마다 자동으로 기억이 남습니다."), 100, 558, 1080, 39, 15);
            UnityEngine.Events.UnityAction next = game.AdvanceEnding;
            if (complete) next = game.ReplayEnding;
            advance = ui.Button(parent, page.action, 100, 613, 500, 48, next, true, game.CanAdvanceEnding);
            ui.Button(parent, "기억 남기기", 620, 613, 270, 48, game.SaveEndingRecord);
            ui.Button(parent, "기억 이어보기", 910, 613, 270, 48, game.LoadCheckpoint);
            ui.Label(parent, complete ? "F5  완료 기록 저장 · F9  기록 불러오기" : "E  계속 · F5  기억 남기기 · F9  기록 불러오기", 100, 680, 1080, 24, 14);
            Tick();
        }

        RectTransform Person(Transform scene, string name, float x, float y, Color color)
        {
            var person = ui.Block(scene, name, x, y, 36, 80, Color.clear).rectTransform;
            ui.Block(person, "Head", 8, 0, 20, 20, new Color(.77f, .67f, .55f));
            ui.Block(person, "Coat", 6, 24, 24, 43, color);
            ui.Block(person, "Arm", 1, 27, 5, 34, color);
            ui.Block(person, "Arm", 30, 27, 5, 34, color);
            ui.Block(person, "Foot", 7, 68, 8, 12, new Color(.2f, .24f, .26f));
            ui.Block(person, "Foot", 22, 68, 8, 12, new Color(.2f, .24f, .26f));
            return person;
        }

        void Move(RectTransform actor, float destinationX)
        {
            movingActor = actor;
            moveFrom = actor.anchoredPosition;
            moveTo = new Vector2(destinationX, moveFrom.y);
        }

        void DrawScene(Transform scene, EndingBeat beat)
        {
            var data = game.Ending;
            ui.Block(scene, "Ground", 0, 180, 1080, 20, new Color(.12f, .15f, .16f));
            switch (beat)
            {
                case EndingBeat.Appointment:
                    ui.Block(scene, "Hall pillar", 160, 20, 28, 160, new Color(.38f, .35f, .39f));
                    ui.Block(scene, "Hall pillar", 885, 20, 28, 160, new Color(.38f, .35f, .39f));
                    ui.Block(scene, "Throne", 715, 37, 92, 137, new Color(.38f, .25f, .26f));
                    Person(scene, "King", 742, 87, data.gold);
                    ui.Block(scene, "Crown", 747, 78, 26, 10, data.gold);
                    Move(Person(scene, "Protagonist", 415, 100, data.protagonist), 478);
                    ui.Block(scene, "Appointment letter", 520, 116, 45, 31, new Color(.83f, .8f, .69f));
                    ui.Label(scene, "총사령관 임명", 310, 23, 380, 28, 17, data.gold, TextAnchor.MiddleCenter);
                    break;
                case EndingBeat.Family:
                    ui.Block(scene, "Home", 740, 28, 200, 152, new Color(.47f, .35f, .25f));
                    ui.Block(scene, "Door", 810, 75, 56, 105, new Color(.18f, .16f, .14f));
                    ui.Block(scene, "Window light", 888, 60, 32, 43, data.gold);
                    Person(scene, "Father", 430, 100, new Color(.48f, .52f, .46f));
                    Person(scene, "Mother", 508, 100, data.family);
                    Move(Person(scene, "Returning protagonist", 670, 100, data.protagonist), 546);
                    ui.Block(scene, "Welcoming arm", 538, 132, 34, 6, data.family);
                    break;
                case EndingBeat.King:
                    ui.Block(scene, "Throne", 153, 32, 86, 148, new Color(.35f, .23f, .29f));
                    Person(scene, "Worried king", 178, 91, data.gold);
                    ui.Block(scene, "Crown", 183, 81, 26, 10, data.gold);
                    Person(scene, "Minister", 320, 100, new Color(.47f, .55f, .61f));
                    ui.Block(scene, "Public notice", 600, 30, 250, 108, new Color(.73f, .7f, .6f));
                    ui.Label(scene, "공개된 왕명 문서\n인장 분실 공표 · 재난 계획", 617, 45, 216, 78, 16, new Color(.18f, .16f, .17f), TextAnchor.MiddleCenter);
                    for (int i = 0; i < 3; i++) Person(scene, "Listener", 630 + i * 65, 112, new Color(.4f + i * .06f, .5f, .47f));
                    break;
                case EndingBeat.RivalOne:
                    ui.Block(scene, "Workbench", 493, 139, 195, 10, new Color(.47f, .33f, .21f));
                    ui.Block(scene, "Bench leg", 510, 149, 9, 31, new Color(.36f, .28f, .22f));
                    ui.Block(scene, "Bench leg", 660, 149, 9, 31, new Color(.36f, .28f, .22f));
                    ui.Block(scene, "Resting sword", 536, 130, 101, 5, new Color(.73f, .78f, .79f));
                    Move(Person(scene, "Rival one", 385, 100, new Color(.68f, .53f, .34f)), 457);
                    break;
                case EndingBeat.RivalTwo:
                    var rival = Person(scene, "Rival two", 285, 100, new Color(.68f, .44f, .32f));
                    trainingBlade = ui.Block(rival, "Training sword", 39, 7, 5, 69, new Color(.74f, .8f, .81f)).rectTransform;
                    for (int i = 0; i < 3; i++)
                    {
                        var soldier = Person(scene, "Soldier", 470 + i * 120, 100, new Color(.43f, .52f, .57f));
                        ui.Block(soldier, "Practice sword", 39, 7, 5, 69, new Color(.58f, .55f, .45f));
                    }
                    break;
                case EndingBeat.Companion:
                    ui.Block(scene, "Distant light", 827, 27, 36, 36, new Color(.73f, .77f, .75f));
                    Move(Person(scene, "Protagonist", 429, 100, data.protagonist), 453);
                    ui.Block(scene, "Quiet road", 497, 177, 170, 3, new Color(.36f, .42f, .44f));
                    break;
                case EndingBeat.Present:
                    ui.Block(scene, "Framed appointment", 160, 29, 104, 96, data.gold);
                    ui.Block(scene, "Appointment paper", 166, 35, 92, 84, new Color(.78f, .76f, .65f));
                    ui.Label(scene, "임명장", 172, 58, 80, 27, 17, new Color(.25f, .25f, .23f), TextAnchor.MiddleCenter);
                    Person(scene, "Protagonist at desk", 473, 101, data.protagonist);
                    ui.Block(scene, "Desk", 509, 139, 181, 9, new Color(.49f, .35f, .25f));
                    ui.Block(scene, "Paper", 526, 132, 83, 6, new Color(.87f, .84f, .74f));
                    ui.Label(scene, "2x + 3 = 9  →  x = 3", 361, 37, 434, 35, 22, data.gold, TextAnchor.MiddleCenter);
                    break;
                case EndingBeat.Complete:
                    ui.Block(scene, "Horizon", 183, 120, 714, 2, data.gold);
                    ui.Label(scene, "내 손으로 풀어낸 순간", 200, 63, 680, 43, 27, data.gold, TextAnchor.MiddleCenter);
                    break;
            }
        }
    }
}
