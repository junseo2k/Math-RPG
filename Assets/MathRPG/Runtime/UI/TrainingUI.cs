using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MathRPG
{
    public sealed class TrainingUI : MonoBehaviour
    {
        static readonly Color Cyan = new Color(.46f, .9f, .91f);
        static readonly Color Gold = UISkin.AccentText;
        static readonly Color White = UISkin.BodyText;
        static readonly Color Muted = UISkin.MutedText;
        UISkin skin;
        QuizSession observedQuiz;
        int observedMistakes, observedStep;
        bool observedCompleted;
        RectTransform answerBadge;
        Image answerIcon;
        TextMeshProUGUI answerLabel;
        float answerBadgeUntil;
        TrainingGame game;
        RectTransform frame, overlay, floats;
        TextMeshProUGUI healthText, hintText, statusText, noticeText, ambientText, warningText, comboText, objectiveText;
        Image hpFill;
        TextMeshProUGUI runeText, quickText, locationText;
        RivalDuelUI rivalUI;
        ForestVillageUI forestUI;
        HumanVillageUI humanUI;
        HumanClimaxUI humanClimaxUI;
        RivalSecondUI rivalSecondUI;
        ChapterFourUI chapterFourUI;
        RivalSecondRematchUI secondRematchUI;
        FinalCalamityUI calamityUI;
        EndingUI endingUI;
        BlockCalculatorUI calculatorUI;
        HuntingGroundUI huntingUI;
        RectTransform blockTestButtons;
        TextMeshProUGUI chapterText;
        readonly List<FloatingNumber> numbers = new List<FloatingNumber>();
        sealed class FloatingNumber { public TextMeshProUGUI label; public float life; }

        public void Initialize(TrainingGame controller)
        {
            game = controller;
            skin = new UISkin();
            calculatorUI = new BlockCalculatorUI(this, game);
            huntingUI = new HuntingGroundUI(this, game);
            rivalUI = new RivalDuelUI(this, game);
            forestUI = new ForestVillageUI(this, game);
            humanUI = new HumanVillageUI(this, game);
            humanClimaxUI = new HumanClimaxUI(this, game);
            rivalSecondUI = new RivalSecondUI(this, game);
            chapterFourUI = new ChapterFourUI(this, game);
            secondRematchUI = new RivalSecondRematchUI(this, game);
            calamityUI = new FinalCalamityUI(this, game);
            endingUI = new EndingUI(this, game);
            var root = new GameObject("Training HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            frame = new GameObject("Design frame", typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(root.transform, false);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            frame.sizeDelta = new Vector2(1280, 720);
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            BuildHUD();
            blockTestButtons = Rect(frame, "Calculator test questions", 40, 194, 285, 100);
            Button(blockTestButtons, "테스트 · 사칙연산", 0, 0, 280, 40, () => game.BeginBlockTest(0));
            Button(blockTestButtons, "테스트 · 근의공식", 0, 48, 280, 40, () => game.BeginBlockTest(1));
            blockTestButtons.gameObject.SetActive(false);
            floats = Rect(frame, "Damage numbers", 0, 0, 1280, 720);
        }

        RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        internal Image Block(Transform parent, string name, float x, float y, float width, float height, Color color, bool raycast = false)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal Image PanelBackground(Transform parent, string name, float x, float y, float width, float height, bool recessed = false)
        {
            var image = Block(parent, name, x, y, width, height, UISkin.PanelFallback);
            skin.ApplyPanel(image, recessed);
            return image;
        }

        internal TextMeshProUGUI Label(Transform parent, string value, float x, float y, float width, float height, int size = 18, Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = Rect(parent, "Text", x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.color = color ?? White;
            text.alignment = ToTMPAlignment(alignment);
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        static TextAlignmentOptions ToTMPAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Left;
            }
        }

        internal Button Button(Transform parent, string title, float x, float y, float width, float height, UnityEngine.Events.UnityAction action, bool primary = false, bool enabled = true,
            UIControlStyle style = UIControlStyle.Long, bool latched = false, bool eliminated = false)
        {
            var image = Block(parent, title, x, y, width, height, UISkin.ButtonFallback, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = enabled;
            skin.ApplyButton(button, style, primary, latched, eliminated);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);
            float inset = eliminated ? 39 : 12;
            Label(image.transform, title, inset, 0, width - inset - 12, height, 18, UISkin.ButtonText, TextAnchor.MiddleCenter);
            if (eliminated) ResultIcon(image.transform, false, 12, (height - 20) * .5f, 20);
            return button;
        }

        internal Button ChoiceButton(Transform parent, string title, float x, float y, float width, float height,
            UnityEngine.Events.UnityAction action, bool enabled = true)
            => Button(parent, title, x, y, width, height, action, enabled: enabled, eliminated: !enabled);

        Image ResultIcon(Transform parent, bool correct, float x, float y, float size)
        {
            var image = Block(parent, correct ? "Correct" : "Wrong", x, y, size, size, Color.white);
            image.sprite = correct ? skin.Check : skin.Cross;
            image.preserveAspect = true;
            image.enabled = image.sprite != null;
            return image;
        }

        internal void ObserveQuiz(QuizSession quiz)
        {
            if (observedQuiz == quiz) return;
            CaptureQuizResult();
            observedQuiz = quiz;
            observedMistakes = quiz?.Mistakes ?? 0;
            observedStep = quiz?.StepIndex ?? 0;
            observedCompleted = quiz != null && quiz.Completed;
        }

        void CaptureQuizResult()
        {
            if (observedQuiz == null) return;
            if (observedQuiz.Mistakes > observedMistakes) ShowAnswerResult(false);
            else if (observedQuiz.StepIndex > observedStep || (!observedCompleted && observedQuiz.Completed)) ShowAnswerResult(true);
            observedMistakes = observedQuiz.Mistakes;
            observedStep = observedQuiz.StepIndex;
            observedCompleted = observedQuiz.Completed;
        }

        internal void ShowAnswerResult(bool correct, bool timedOut = false)
        {
            if (answerBadge == null)
            {
                answerBadge = PanelBackground(frame, "Answer result", 1040, 212, 200, 48).rectTransform;
                answerIcon = ResultIcon(answerBadge, correct, 14, 12, 24);
                answerLabel = Label(answerBadge, "", 48, 0, 140, 48, 18);
            }
            answerIcon.sprite = correct ? skin.Check : skin.Cross;
            answerIcon.enabled = answerIcon.sprite != null;
            answerLabel.text = timedOut ? "시간 초과" : correct ? "정답" : "오답";
            answerBadgeUntil = Time.unscaledTime + 1.6f;
            answerBadge.gameObject.SetActive(true);
            answerBadge.SetAsLastSibling();
        }

        void BuildHUD()
        {
            var top = PanelBackground(frame, "Top band", 24, 22, 1232, 86);
            locationText = Label(top.transform, game.Stage.locationName, 20, 8, 285, 31, 23);
            chapterText = Label(top.transform, "CHAPTER 01  /  룬의 첫 울림", 20, 43, 280, 24, 12, Gold);
            healthText = Label(top.transform, "", 330, 10, 290, 25, 15);
            Block(top.transform, "Health track", 330, 45, 275, 9, new Color(.19f, .28f, .31f));
            hpFill = Block(top.transform, "Health", 330, 45, 275, 9, Cyan);
            hintText = Label(top.transform, "", 640, 9, 230, 28, 17, Gold);
            statusText = Label(top.transform, "", 640, 40, 270, 24, 13, Muted);
            Button(top.transform, "기억 불러오기", 946, 20, 154, 42, game.LoadCheckpoint);
            Button(top.transform, "도움말", 1110, 20, 102, 42, () => { if (game.Mode == TrainingMode.Playing) Pause(); });
            warningText = Label(frame, "", 370, 120, 540, 43, 23, Gold, TextAnchor.MiddleCenter);
            comboText = Label(frame, "", 1050, 140, 180, 60, 26, Gold, TextAnchor.MiddleRight);
            objectiveText = Label(frame, "", 40, 120, 310, 65, 16, Muted, TextAnchor.UpperLeft);
            ambientText = Label(frame, "", 52, 265, 285, 100, 17, new Color(.78f, .84f, .79f), TextAnchor.MiddleLeft);
            noticeText = Label(frame, "", 235, 568, 810, 42, 17, White, TextAnchor.MiddleCenter);
            var bottom = PanelBackground(frame, "Controls", 24, 625, 1232, 74);
            Label(bottom.transform, "A D  이동    SPACE  점프    S  숙이기", 20, 9, 425, 24, 15, White);
            Label(bottom.transform, "E  대화·상자·이동     F5  기억 남기기     ESC  쉼", 20, 36, 480, 23, 13, Muted);
            Button(bottom.transform, "J  검격", 550, 13, 160, 48, game.Attack);
            var quick = Button(bottom.transform, "", 720, 13, 180, 48, game.QuickSkill);
            quickText = quick.GetComponentInChildren<TextMeshProUGUI>();
            var rune = Button(bottom.transform, "", 910, 13, 300, 48, game.BeginRune, true);
            runeText = rune.GetComponentInChildren<TextMeshProUGUI>();
        }

        void Pause()
        {
            // Public controller entry point keeps button and keyboard behavior identical.
            game.Pause();
        }

        public void Tick()
        {
            CaptureQuizResult();
            if (answerBadge != null)
            {
                answerBadge.gameObject.SetActive(Time.unscaledTime < answerBadgeUntil);
                answerBadge.SetAsLastSibling();
            }
            blockTestButtons.gameObject.SetActive(game.CanStartBlockTest);
            if (game.EndingScreen) { endingUI.Tick(); return; }
            warningText.fontSize = game.InSecondRematch ? 18 : 23;
            locationText.text = game.Human.InVillage ? game.Human.Definition.villageName : game.Forest.InVillage ? game.Forest.Definition.villageName : game.Rival.InEncounter ? "숲으로 이어지는 길" : game.Stage.locationName;
            chapterText.text = game.Human.InVillage ? "CHAPTER 03  /  잿빛 길" : game.Forest.InVillage ? "CHAPTER 02  /  정령의 숲" : "CHAPTER 01  /  룬의 첫 울림";
            rivalUI.Tick();
            rivalSecondUI.Tick();
            chapterFourUI.Tick();
            calamityUI.Tick();
            healthText.text = "생명   " + game.Health + " / " + game.definition.playerHealth;
            hpFill.rectTransform.sizeDelta = new Vector2(275f * game.Health / game.definition.playerHealth, 9);
            hintText.text = "정령의 힘   " + game.Hints + " / " + game.definition.hintCharges;
            statusText.text = game.Rival.InEncounter ? "라이벌  ·  " + game.enemy.Health + " / " + game.enemy.MaxHealth : "훈련소 " + (game.Course.Index + 1) + " / 4   ·   스킬북 " + game.Progress.skillBooks;
            if (game.Forest.InVillage) statusText.text = game.Forest.BossActive ? "수호자  ·  " + game.enemy.Health + " / " + game.enemy.MaxHealth : "유물 " + game.Progress.forest.RelicCount + " / 3";
            if (game.Guardian.Active) statusText.text = "집합체  ·  " + game.enemy.Health + " / " + game.enemy.MaxHealth;
            if (game.Guardian.BlessingActive) statusText.text += " · 가호";
            if (game.Human.InVillage) statusText.text = game.Human.BossActive ? game.Human.Definition.bossName + " · " + game.enemy.Health + " / " + game.enemy.MaxHealth :
                game.Progress.human.CurrentBossDefeated ? "마을 의뢰 완료" : game.Progress.human.CurrentQuestAccepted ? "마을 의뢰 진행 중" : "주민의 이야기 듣기";
            if (game.HumanClimax.Active)
            {
                locationText.text = "돌빛 장터 외곽";
                statusText.text = game.HumanClimax.Definition.bossName + " · " + game.enemy.Health + "/" + game.enemy.MaxHealth + " · 강화 " + game.HumanClimax.Rage;
            }
            else if (game.Human.InVillage && game.Progress.human.royalSealObtained && !game.Progress.human.sealTaken) statusText.text += " · 왕의 인장";
            if (game.Human.InVillage && game.Progress.human.sealTaken) statusText.text = "인장 상실 · 답장 미개봉";
            quickText.text = game.QuickCooldown > 0 ? "K  잔광  " + game.QuickCooldown.ToString("0.0") + "s" : "K  잔광";
            runeText.text = game.RuneCooldown > 0 ? "Q  여명 가르기  " + game.RuneCooldown.ToString("0.0") + "s" : "Q  여명 가르기";
            noticeText.text = game.Notice;
            ambientText.text = game.Ambient;
            comboText.text = game.Combo > 1 ? game.Combo + "  COMBO" : "";
            objectiveText.text = game.Human.InVillage ? humanUI.Objective : game.Forest.InVillage ? forestUI.Objective : TrainingCourseText.Objective(game);
            if (game.Human.InVillage && game.Progress.human.villageIndex == 2 && game.Progress.human.thirdBossDefeated && game.player.transform.position.x > TrainingCourseView.EntranceX)
                objectiveText.text = humanClimaxUI.Objective;
            warningText.text = game.Mode == TrainingMode.Playing && game.HasOpponent && game.enemy.Phase == EnemyPhase.Windup && (game.Human.BossActive || game.Forest.BossActive || game.Rival.InEncounter || game.player.transform.position.x >= -5.2f) ? (game.enemy.LowSweep ? "낮게 휩쓴다  —  SPACE 점프" : "높게 가른다  —  S 숙이기") : "";
            if (game.HumanClimax.Active)
            {
                objectiveText.text = humanClimaxUI.Objective;
                warningText.text = game.Mode == TrainingMode.Playing ? game.HumanClimaxWarning : "";
            }
            if (game.RivalSecond.Active)
            {
                locationText.text = "돌빛 장터 외곽 · 결투";
                statusText.text = game.RivalSecond.Definition.rivalName + " · " + game.RivalSecond.Health + "/" + game.RivalSecond.Definition.health;
                objectiveText.text = "인장을 돌려받기 위한 결투\nJ·K: 검격 / Q: 룬 대결";
                warningText.text = game.Mode == TrainingMode.Playing && game.RivalSecond.WindingUp
                    ? (game.RivalSecond.LowSweep ? "낮은 검격 — SPACE 점프" : "높은 검격 — S 숙이기") : "";
            }
            if (game.Progress.human.chapter3Complete && !game.InChapterFour)
            {
                chapterText.text = "CHAPTER 03 / 끝";
                locationText.text = "행방불명";
                statusText.text = "인장 상실 · 답장 미개봉";
                objectiveText.text = "챕터3 완료";
                warningText.text = "";
            }
            if (game.InChapterFour)
            {
                chapterText.text = "CHAPTER 04 / 재앙";
                locationText.text = game.Progress.chapterFour.InInnerWorld ? "심상세계" : "무너진 길";
                statusText.text = game.Progress.chapterFour.LetterTorn ? "인장 상실 · 찢어진 답장" : game.Progress.chapterFour.LetterOpened ? "인장 상실 · 개봉한 답장" : "인장 상실 · 답장 미개봉";
                if (game.Progress.chapterFour.Awakened) statusText.text += " · 각성";
                else if (game.Progress.chapterFour.SpiritRevealed) statusText.text += " · 정령의 정체";
                objectiveText.text = chapterFourUI.Objective;
                warningText.text = "";
            }
            if (game.InSecondRematch)
            {
                var fight = game.SecondRematch;
                locationText.text = "재앙으로 향하는 길";
                statusText.text = fight.Active ? "라이벌2 " + fight.Health + "/" + fight.Definition.health + " · 페이즈 " + fight.Phase : "라이벌2와의 재회";
                objectiveText.text = fight.Active ? "강화 " + fight.Rage + "/" + fight.Definition.maximumRage + " · Q 룬 대결\n장판은 A/D로 표시 밖까지 이동" :
                    game.Progress.chapterFour.secondRematch >= RematchProgress.Victory ? "라이벌2가 열어준 길" : "지수와 로그의 흐름 익히기";
                warningText.text = secondRematchUI.Warning;
                runeText.text = "Q  지수·로그 룬 대결";
            }
            if (game.InCalamity)
            {
                var fight = game.Calamity;
                locationText.text = "뒤틀린 시간의 중심";
                statusText.text = fight.Active ? "재앙 " + fight.Health + "/" + fight.Definition.health + " · 페이즈 " + fight.Phase : fight.Won ? "재앙 격파" : "정령의 마지막 기억";
                hintText.text = fight.Active || fight.Won || game.Mode == TrainingMode.CalamityIsolation ? "정령의 도움 없음" : "정령과 함께";
                objectiveText.text = fight.Active ? "강화 " + fight.Rage + "/" + fight.Definition.maximumRage + (fight.Phase == 3 ? " · 회피와 검격\n장판 밖에서 빈틈을 노리기" : " · Q 룬 / R 풀이 전환\n정답이어도 장판 밖으로 이동") : fight.Won ? "재앙을 쓰러뜨렸다" : "재앙과 마주하기";
                warningText.text = calamityUI.Warning;
                runeText.text = fight.Transformed ? "Q  정령화로 봉쇄" : fight.Active && fight.Phase == 3 ? "Q  시간에 막힌 룬" : "Q  발상의 전환";
                if (fight.Transformed) quickText.text = "K  정령화로 봉쇄";
            }
            if (game.InHunting)
            {
                locationText.text = game.HuntingArea.locationName;
                chapterText.text = "CHAPTER 0" + (game.HuntingIndex / 3 + 1) + " / 사냥터 " + (game.HuntingIndex % 3 + 1) + " / 3";
                statusText.text = "사냥 " + game.HuntingState.kills + " / " + game.HuntingArea.huntTarget;
                objectiveText.text = game.HuntingObjective;
                runeText.text = "Q  개념의 룬";
                warningText.text = game.Mode == TrainingMode.Playing && game.HasOpponent && game.enemy.Phase == EnemyPhase.Windup ?
                    (game.enemy.LowSweep ? "낮은 공격 — SPACE 점프" : "높은 공격 — S 숙이기") : "";
            }
            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                var n = numbers[i];
                n.life -= Time.unscaledDeltaTime;
                if (n.life <= 0) { Destroy(n.label.gameObject); numbers.RemoveAt(i); continue; }
                n.label.rectTransform.anchoredPosition += Vector2.up * (Time.unscaledDeltaTime * 45);
                var c = n.label.color; c.a = Mathf.Clamp01(n.life * 2); n.label.color = c;
            }
        }

        public void FloatText(Vector3 position, string value, bool heavy)
        {
            var screen = game.worldCamera.WorldToScreenPoint(position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, screen, null, out var local);
            var label = Label(floats, value, local.x + 600, 340 - local.y, 80, 50, heavy ? 34 : 26, heavy ? Cyan : Gold, TextAnchor.MiddleCenter);
            numbers.Add(new FloatingNumber { label = label, life = .85f });
        }

        public void RefreshOverlay(string feedback = null)
        {
            CaptureQuizResult();
            ObserveQuiz(null);
            if (overlay != null) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); }
            if (game.EndingScreen)
            {
                overlay = Block(frame, "Ending backdrop", 0, 0, 1280, 720, new Color(.035f, .055f, .075f, 1), true).rectTransform;
                endingUI.Draw(overlay, feedback);
                return;
            }
            if (game.Mode == TrainingMode.Playing) return;
            overlay = Block(frame, "Modal shade", 0, 0, 1280, 720, new Color(.02f, .05f, .09f, .83f), true).rectTransform;
            bool forestQuiz = game.Mode == TrainingMode.ForestChallenge || game.Mode == TrainingMode.HumanClimaxChallenge || game.Mode == TrainingMode.RivalSecondChallenge;
            bool tallCard = forestQuiz || game.Mode == TrainingMode.ChapterFourStory || game.InSecondRematch || game.InHunting;
            bool blockQuiz = game.Mode == TrainingMode.Quiz && game.Quiz != null && game.Quiz.Step.type == QuestionStepType.BlockInput;
            var card = PanelBackground(overlay, "Modal card", 260, blockQuiz ? 48 : tallCard ? 80 : 128, 760, blockQuiz ? 610 : tallCard ? 560 : 454).transform;
            if (huntingUI.Draw(card, feedback)) return;
            if (calamityUI.Draw(card, feedback)) return;
            if (secondRematchUI.Draw(card, feedback)) return;
            if (chapterFourUI.Draw(card, feedback)) return;
            if (rivalUI.Draw(card, feedback)) return;
            if (forestUI.Draw(card, feedback)) return;
            if (humanUI.Draw(card, feedback)) return;
            if (humanClimaxUI.Draw(card, feedback)) return;
            if (rivalSecondUI.Draw(card, feedback)) return;
            if (game.Mode == TrainingMode.Quiz) { DrawQuiz(card, feedback); return; }
            if (game.Mode == TrainingMode.Introduction)
            {
                Label(card, "PROLOGUE  /  왕국의 훈련소", 32, 22, 695, 26, 13, Gold);
                Label(card, "룬의 첫 울림", 32, 66, 695, 52, 34);
                Label(card, game.IntroText, 32, 140, 695, 150, 22, White, TextAnchor.UpperLeft);
                Label(card, feedback ?? "검은 손으로, 룬은 생각으로 움직인다.", 32, 310, 695, 32, 15, Muted);
                Button(card, "계속", 32, 370, 338, 52, game.AdvanceIntro, true);
                Button(card, "기억 이어가기", 386, 370, 342, 52, game.LoadCheckpoint, enabled: game.HasCheckpoint);
            }
            else if (game.Mode == TrainingMode.Paused)
            {
                Label(card, "잠시 숨 고르기", 32, 28, 696, 48, 29);
                Label(card, "A / D 또는 ← / →   이동\nSPACE   점프     S / ↓   숙이기\nJ   검격     K   잔광 (즉시 스킬)\nQ   여명 가르기 (룬의 흐름)\n1~4   보기 선택     H   정령의 힌트\nE   대화·상자·훈련소 이동     F5 / F9   저장 / 불러오기", 32, 95, 696, 218, 20, White, TextAnchor.UpperLeft);
                Label(card, feedback ?? (game.Forest.BossActive || game.Human.BossActive || game.HumanClimax.Active ? "마을로 돌아가면 보스의 생명과 문제 진행이 초기화됩니다. 의뢰 기록은 유지됩니다." : game.Rival.InEncounter ? "지금은 결투와 제한시간이 멈춰 있습니다. 돌아가면 이번 결투를 다시 시작합니다." : "문제를 푸는 동안 전투가 멈춥니다. 왼쪽 푸른 표식에서 저장할 수 있습니다."), 32, 310, 696, 45, 15, Muted);
                Button(card, "계속하기", 32, 373, 220, 50, game.Resume, true);
                if (game.Rival.InEncounter) Button(card, "훈련소로 돌아가기", 266, 373, 220, 50, game.LeaveRivalEncounter);
                else if (game.Forest.BossActive) Button(card, "마을로 돌아가기", 266, 373, 220, 50, game.Forest.ReturnToVillage);
                else if (game.Human.BossActive) Button(card, "마을로 돌아가기", 266, 373, 220, 50, game.Human.ReturnToVillage);
                else if (game.HumanClimax.Active) Button(card, "마을로 돌아가기", 266, 373, 220, 50, game.Human.ReturnToVillage);
                else Button(card, "기억 불러오기", 266, 373, 220, 50, game.LoadCheckpoint, enabled: game.HasCheckpoint);
                Button(card, "소리 켜기 / 끄기", 500, 373, 228, 50, () => AudioListener.volume = AudioListener.volume > 0 ? 0 : 1);
            }
            else if (game.Mode == TrainingMode.GameOver)
            {
                Label(card, "정령이 붙잡은 순간", 32, 30, 696, 34, 15, Cyan);
                Label(card, "아직, 끝이 아니야.", 32, 94, 696, 60, 34);
                Label(card, game.RewindText, 32, 179, 696, 105, 23, White, TextAnchor.UpperLeft);
                Label(card, game.Human.InVillage ? "생명과 정령의 힘을 회복하고 " + game.Human.Definition.villageName + "로 돌아갑니다." : game.Forest.InVillage ? "생명과 정령의 힘을 회복하고 " + game.Forest.Definition.villageName + "로 돌아갑니다." : game.Rival.InEncounter ? "생명과 정령의 힘을 회복하고 라이벌과의 결투를 처음부터 다시 시작합니다." : "생명과 정령의 힘을 회복하고 훈련장 입구로 돌아갑니다.", 32, 303, 696, 40, 16, Muted);
                Button(card, "시간 되감기", 32, 371, 696, 52, game.Rewind, true);
            }
            else if (game.Mode == TrainingMode.Reward)
            {
                Label(card, "교관의 인정", 32, 29, 696, 30, 14, Gold);
                Label(card, game.Stage.bookName + " · 획득", 32, 86, 696, 60, 32);
                Label(card, game.Stage.rewardDialogue, 32, 165, 696, 90, 22, White, TextAnchor.UpperLeft);
                Label(card, TrainingCourseText.Reward(game), 32, 270, 696, 74, 17, Cyan);
                Button(card, "연무장으로 돌아가기", 32, 372, 696, 52, game.Resume, true);
            }
        }

        void DrawQuiz(Transform card, string feedback)
        {
            var quiz = game.Quiz;
            if (quiz == null) return;
            ObserveQuiz(quiz);
            Label(card, game.InHunting ? "사냥터의 흐름 / 전투 정지 · 제한시간 없음" : game.BlockTestQuiz ? "계산기 임시 문항  /  " + (quiz.StepIndex + 1) + " / " + quiz.StepCount : game.ChestQuiz ? "봉인 상자  /  실패 시 피해 없음" : game.InstructorQuiz ? "교관의 시험   " + (game.ExamCompleted + 1) + " / " + game.Stage.questionCount : "여명 가르기  /  전투 정지", 30, 17, 560, 25, 13, Gold);
            Button(card, "거두기", 620, 13, 110, 34, game.CancelQuiz);
            Label(card, quiz.Title + "  ·  " + quiz.Step.phase, 30, 56, 695, 38, 24);
            Label(card, quiz.Step.prompt, 30, 107, 695, game.InHunting ? 100 : 67, 23, White, TextAnchor.UpperLeft);
            if (quiz.Step.type == QuestionStepType.BlockInput)
            {
                calculatorUI.Draw(card, quiz, feedback);
                return;
            }
            for (int i = 0; i < quiz.Step.choices.Length; i++)
            {
                int index = i;
                bool removed = quiz.IsEliminated(index);
                ChoiceButton(card, (index + 1) + "   " + (removed ? "흐름이 끊긴 길" : quiz.Step.choices[index]), 30 + i % 2 * 357, (game.InHunting ? 225 : 191) + i / 2 * 66, 338, 55,
                    () => game.SubmitAnswer(index), enabled: !removed);
            }
            string hint = quiz.HintVisible ? "정령: " + quiz.Step.hint : feedback ?? (game.InstructorQuiz ? game.Stage.briefing : "어떤 흐름을 쓸지 선택하세요.  [1~4] 또는 클릭");
            if (quiz.LastCalculation != null) hint = quiz.LastCalculation.Display + "\n" + hint;
            Label(card, hint, 30, game.InHunting ? 370 : 325, 695, game.InHunting ? 74 : 58, 16, quiz.HintVisible ? Gold : Muted, TextAnchor.UpperLeft);
            Button(card, "H  정령의 도움  (" + game.Hints + ")", 30, game.InHunting ? 480 : 392, 300, 42, game.UseHint, enabled: !quiz.HintVisible && game.Hints > 0);
            Label(card, "조준   →   차징   →   발동", 351, game.InHunting ? 480 : 392, 379, 42, 16, Gold, TextAnchor.MiddleRight);
        }

    }
}
