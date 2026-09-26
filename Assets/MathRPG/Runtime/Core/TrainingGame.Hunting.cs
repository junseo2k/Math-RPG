using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public HuntingGroundDefinition HuntingGrounds { get; private set; }
        public bool InHunting => Progress.hunting != null && Progress.hunting.activeIndex >= 0;
        public int HuntingIndex => Progress.hunting.activeIndex;
        public HuntingAreaDefinition HuntingArea => HuntingGrounds.areas[HuntingIndex];
        public HuntingAreaProgress HuntingState => Progress.hunting.areas[HuntingIndex];
        public int HuntingLessonPage { get; private set; }
        public string HuntingObjective => HuntingState.completed ? "사냥 완료 · 오른쪽 길 E\nL: 개념 다시 보기" :
            "길을 여는 사냥 " + HuntingState.kills + " / " + HuntingArea.huntTarget + "\nQ: 개념의 룬 · L: 개념 다시 보기";
        HuntingGroundView huntingView;
        float huntingRespawn, huntingQuestionDelay;
        bool huntingReview;

        void InitializeHunting()
        {
            HuntingGrounds = Resources.Load<HuntingGroundDefinition>("MathRPG/HuntingGrounds");
            huntingView = gameObject.AddComponent<HuntingGroundView>();
            huntingView.Initialize(enemy);
        }

        internal bool RequireHuntingGround(int chapter, int area)
        {
            int index = (chapter - 1) * 3 + area;
            if (index < 0 || index >= HuntingGroundDefinition.AreaCount) return true;
            if (Progress.hunting.areas[index].completed) return false;
            EnterHunting(index);
            return true;
        }

        bool HuntingDataReady()
        {
            if (HuntingGrounds != null && HuntingGrounds.IsValid(out _)) return true;
            const string message = "사냥터 데이터가 없습니다. Math RPG > Open Training Prototype으로 콘텐츠를 생성해주세요.";
            Notify(message, 8);
            UI.RefreshOverlay(message);
            return false;
        }

        void EnterHunting(int index)
        {
            if (!HuntingDataReady()) return;
            int previous = Progress.hunting.activeIndex;
            Progress.hunting.activeIndex = index;
            if (!SaveHunting())
            {
                Progress.hunting.activeIndex = previous;
                UI.RefreshOverlay(Notice);
                return;
            }
            SetupHunting();
        }

        void SetupHunting()
        {
            ResetArena();
            courseView.SetRegionVisible(false);
            forestView.SetVisible(false, Progress.forest.CurrentRelic);
            humanView.SetVisible(false);
            climaxView.SetVillage(false);
            huntingView.Show(HuntingArea, HuntingState.completed);
            player.gameObject.SetActive(true);
            if (spirit != null) spirit.gameObject.SetActive(true);
            Ambient = Notice = "";
            ambientPlayed = true;
            huntingRespawn = 0;
            huntingQuestionDelay = HuntingArea.questionInterval;
            if (HuntingState.completed) enemy.gameObject.SetActive(false);
            else enemy.ResetEnemy(HuntingArea.enemyHealth, HuntingArea.attackWindup, HuntingState.kills);
            huntingReview = false;
            HuntingLessonPage = (Progress.hunting.chapterIntroductions & (1 << (HuntingIndex / 3))) == 0 ? -1 :
                Mathf.Min(HuntingState.lessonsRead, HuntingArea.lessons.Length);
            SetMode(HuntingLessonPage < HuntingArea.lessons.Length ? TrainingMode.HuntingLesson : TrainingMode.Playing);
        }

        internal void ReviewHuntingConcepts()
        {
            if (!InHunting || (Mode != TrainingMode.Playing && Mode != TrainingMode.Paused)) return;
            huntingReview = true;
            HuntingLessonPage = -1;
            SetMode(TrainingMode.HuntingLesson);
        }

        internal void AdvanceHuntingLesson()
        {
            if (!InHunting || Mode != TrainingMode.HuntingLesson) return;
            int oldRead = HuntingState.lessonsRead;
            int oldIntroductions = Progress.hunting.chapterIntroductions;
            if (HuntingLessonPage < 0) Progress.hunting.chapterIntroductions |= 1 << (HuntingIndex / 3);
            else HuntingState.lessonsRead = Mathf.Max(HuntingState.lessonsRead, HuntingLessonPage + 1);
            if (!SaveHunting())
            {
                HuntingState.lessonsRead = oldRead;
                Progress.hunting.chapterIntroductions = oldIntroductions;
                return;
            }
            HuntingLessonPage++;
            if (HuntingLessonPage >= HuntingArea.lessons.Length)
            {
                SetMode(TrainingMode.Playing);
                Notify(huntingReview ? "배운 흐름을 다시 떠올렸다." : "일반 몬스터를 쓰러뜨려 길을 열자. L로 설명을 다시 볼 수 있습니다.", 5);
            }
            else UI.RefreshOverlay();
        }

        void TickHuntingInput()
        {
            if (!InHunting) return;
            if (Mode == TrainingMode.HuntingLesson && GameInput.Pressed(Key.F5)) { SaveCheckpoint(); return; }
            if (Mode == TrainingMode.HuntingLesson && GameInput.Pressed(Key.F9)) { LoadCheckpoint(); return; }
            if (Mode == TrainingMode.HuntingLesson && GameInput.Pressed(Key.E)) AdvanceHuntingLesson();
            else if (Mode == TrainingMode.Playing && GameInput.Pressed(Key.L)) ReviewHuntingConcepts();
        }

        void TickHuntingEnemy(float dt)
        {
            if (HuntingState.completed) return;
            if (enemy.Phase == EnemyPhase.Defeated)
            {
                huntingRespawn -= dt;
                if (huntingRespawn <= 0)
                {
                    enemy.ResetEnemy(HuntingArea.enemyHealth, HuntingArea.attackWindup, HuntingState.kills);
                    huntingQuestionDelay = HuntingArea.questionInterval;
                }
                return;
            }
            huntingQuestionDelay -= dt;
            if (huntingQuestionDelay <= 0 && castDelay <= 0)
            {
                OpenHuntingQuestion();
                return;
            }
            if (!enemy.Tick(dt, player)) return;
            float distance = Mathf.Abs(player.transform.position.x - enemy.transform.position.x);
            Effects.Slash(enemy.transform.position + new Vector3(0, enemy.LowSweep ? .3f : 1.35f), player.transform.position.x < enemy.transform.position.x ? -1 : 1, false);
            if (distance > 3.2f || player.Dodges(enemy.LowSweep) || player.Invulnerable) return;
            player.KnockBack(Mathf.Sign(player.transform.position.x - enemy.transform.position.x));
            DamagePlayer(HuntingArea.enemyDamage, false);
        }

        void OpenHuntingQuestion()
        {
            if (!InHunting || Mode != TrainingMode.Playing || !HasOpponent || HuntingState.completed) return;
            var lesson = HuntingArea.lessons[HuntingState.kills % HuntingArea.lessons.Length];
            InstructorQuiz = ChestQuiz = BlockTestQuiz = false;
            Quiz = new QuizSession(lesson.question, Hints);
            huntingQuestionDelay = HuntingArea.questionInterval;
            SetMode(TrainingMode.Quiz);
        }

        void RecordHuntingKill()
        {
            HuntingState.kills = Mathf.Min(HuntingArea.huntTarget, HuntingState.kills + 1);
            HuntingState.completed = HuntingState.kills >= HuntingArea.huntTarget;
            huntingRespawn = HuntingArea.respawnDelay;
            Health = Mathf.Min(definition.playerHealth, Health + HuntingArea.killHeal);
            huntingView.SetGate(HuntingState.completed);
            if (!SaveHunting()) return;
            Notify(HuntingState.completed ? "길이 열렸다. 오른쪽 끝에서 E로 계속 나아가자." :
                "사냥 " + HuntingState.kills + " / " + HuntingArea.huntTarget + " · 다음 흐름을 준비하자.", 5);
        }

        void InteractWithHunting()
        {
            if (player.transform.position.x <= TrainingCourseView.EntranceX) LeaveHunting(false);
            else if (player.transform.position.x >= TrainingCourseView.ExitX)
            {
                if (!HuntingState.completed) { Notify("사냥 " + HuntingState.kills + " / " + HuntingArea.huntTarget + " · 앞길은 아직 막혀 있다."); return; }
                LeaveHunting(true);
            }
            else Notify("왼쪽 끝 E: 돌아가기 · 오른쪽 끝 E: 다음 길 · L: 개념 다시 보기");
        }

        internal void LeaveHunting(bool forward)
        {
            if (!InHunting || (Mode != TrainingMode.Playing && Mode != TrainingMode.Paused && Mode != TrainingMode.HuntingLesson)) return;
            if (forward && !HuntingState.completed) return;
            int index = HuntingIndex;
            if (index >= 9 && ((forward && index < 11) || (!forward && index > 9)))
            {
                EnterHunting(index + (forward ? 1 : -1));
                return;
            }
            Progress.hunting.activeIndex = -1;
            if (!SaveHunting())
            {
                Progress.hunting.activeIndex = index;
                UI.RefreshOverlay(Notice);
                return;
            }
            ResetArena();
            SetMode(TrainingMode.Playing);
            if (index >= 9)
            {
                RestoreChapterFour();
                if (forward) SecondRematch.Begin();
            }
            else if (forward)
            {
                if (index < 3) MoveTraining(1);
                else if (index < 6) Forest.StartBoss();
                else Human.StartBoss();
            }
            else Notify("돌아왔습니다. 오른쪽 길에서 사냥터에 다시 들어갈 수 있습니다.");
        }

        internal void RetryHunting()
        {
            if (InHunting && (Mode == TrainingMode.GameOver || Mode == TrainingMode.Paused)) SetupHunting();
        }

        bool SaveHunting()
        {
            if (CheckpointStore.Save(SavePath, Progress, out var error)) return true;
            Notify("사냥터 기록을 저장하지 못했습니다: " + error, 8);
            UI.RefreshOverlay(Notice);
            return false;
        }
    }
}
