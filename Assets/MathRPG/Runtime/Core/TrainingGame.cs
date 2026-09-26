using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public enum TrainingMode { Introduction, Playing, Quiz, Paused, GameOver, Reward, RivalIntroduction, RivalChallenge, RivalResult, ChapterClear, ForestArrival, ForestRequest, ForestChallenge, ForestRelic, GuardianAwakening, GuardianIntroduction, ForestChapterClear, HumanArrival, HumanRequest, HumanChallenge, HumanAftermath, HumanLetter, HumanDisaster, HumanClimaxChallenge, HumanClimaxVictory, RivalSecondIntroduction, RivalSecondChallenge, RivalSecondBreakdown, RivalSecondDefeat, HumanChapterComplete, ChapterFourStory, SecondRematchIntroduction, SecondRematchLesson, SecondRematchChallenge, SecondRematchPhase, SecondRematchVictory, SecondRematchDeparture, CalamityIntroduction, CalamityFarewell, CalamityIsolation, CalamityChallenge, CalamityPhase, CalamityVictory, EndingStory, EndingComplete, HuntingLesson }

    public sealed partial class TrainingGame : MonoBehaviour
    {
        public TrainingDefinition definition;
        public PlayerMotor player;
        public TrainingEnemy enemy;
        public Transform spirit;
        public Camera worldCamera;
        public Transform instructor;
        [SerializeField, Min(0)] float spiritFollowSpeed = 6;
        [SerializeField, Min(0)] float spiritStopDistance = .01f;
        Vector3 spiritFollowPosition, cameraBasePosition;
        bool cameraBaseCaptured;
        Transform EnemyHitVisual => InCalamity ? calamityView?.HitVisual : InSecondRematch ? secondRematchView?.HitVisual : enemy.visual;
        public TrainingMode Mode { get; private set; }
        public QuizSession Quiz { get; private set; }
        public CheckpointData Progress { get; private set; } = new CheckpointData();
        public int Health { get; private set; }
        public int Hints { get; private set; }
        public int DefeatedCount { get; private set; }
        public int Combo { get; private set; }
        public string Notice { get; private set; }
        public string Ambient { get; private set; }
        public bool InstructorQuiz { get; private set; }
        public bool ChestQuiz { get; private set; }
        public bool BlockTestQuiz { get; private set; }
        public bool CanStartBlockTest
        {
            get
            {
#if UNITY_EDITOR
                return !InHunting && Course != null && Course.Index == 0 && Mode == TrainingMode.Playing && castDelay <= 0 &&
                    !InChapterFour && !Rival.InEncounter && !Forest.InVillage && !Human.InVillage && !Trials.SparringActive;
#else
                return false;
#endif
            }
        }
        public TrainingCourseProgress Course { get; private set; }
        public InstructorTrials Trials { get; private set; }
        public TrainingStageDefinition Stage => Course.Stage;
        public TrainingStageProgress StageProgress => Course.State;
        public int ExamCompleted => Trials.CompletedQuestions;
        public float RuneCooldown => runeCooldown;
        public float QuickCooldown => quickCooldown;
        public string CheckpointPath { get; set; }
        string SavePath => string.IsNullOrEmpty(CheckpointPath) ? CheckpointStore.DefaultPath : CheckpointPath;
        public bool NearInstructor => player != null && Mathf.Abs(player.transform.position.x + 8.7f) < 1.05f;
        public bool HasOpponent => enemy != null && enemy.gameObject.activeInHierarchy && enemy.Phase != EnemyPhase.Defeated;
        public int NearbyChest
        {
            get
            {
                if (InHunting || Course == null || InSecondRematch || Forest.InVillage || Human.InVillage || Stage.trialKind != InstructorTrialKind.Treasure) return -1;
                for (int i = 0; i < 2; i++)
                    if (Mathf.Abs(player.transform.position.x - TrainingCourseView.ChestX(i)) < 1.2f) return i;
                return -1;
            }
        }
        public bool HasCheckpoint => System.IO.File.Exists(SavePath);
        public bool CanSave => Mode == TrainingMode.Playing && !InSecondRematch && !Trials.SparringActive && !Rival.InEncounter && !Forest.BossActive && !Human.BossActive && !HumanClimax.Active && !RivalSecond.Active && player.transform.position.x < -5.2f;
        public CombatEffects Effects { get; private set; }
        public TrainingUI UI { get; private set; }
        float attackCooldown, quickCooldown, runeCooldown, respawn, noticeTimer, ambientTimer, comboTimer, castDelay;
        int questionCursor, introIndex, quizChest = -1;
        TrainingCourseView courseView;
        bool ambientPlayed;
        TrainingMode resumeMode = TrainingMode.Playing;
        public string IntroText => definition.dialogue.introduction[introIndex];
        public string RewindText => Human.InVillage ? Human.Definition.rewind : Forest.InVillage ? Forest.Definition.rewind : definition.dialogue.rewind[Mathf.Max(0, Progress.deaths - 1) % definition.dialogue.rewind.Length];

        void Start()
        {
            if (definition == null || player == null || enemy == null || definition.questions == null || definition.questions.Length == 0 ||
                definition.runeSkill == null || definition.dialogue == null || definition.dialogue.introduction == null ||
                definition.dialogue.introduction.Length == 0 || definition.dialogue.rewind == null || definition.dialogue.rewind.Length == 0)
            { Debug.LogError("Training scene is incomplete. Use Math RPG > Create Training Prototype."); enabled = false; return; }
            foreach (var question in definition.questions)
            {
                if (question == null || !question.IsValid(out _))
                { Debug.LogError("A training question is invalid. Check the Training content asset."); enabled = false; return; }
            }
            var courseDefinition = definition.course != null ? definition.course : Resources.Load<TrainingCourseDefinition>("MathRPG/TrainingCourse");
            if (courseDefinition == null || !courseDefinition.IsValid())
            { Debug.LogError("Training course data is missing or invalid. Use Math RPG > Open Training Prototype to create course data."); enabled = false; return; }
            Course = new TrainingCourseProgress(courseDefinition, Progress);
            Trials = new InstructorTrials(Course);
            Trials.Completed += CompleteExam;
            if (instructor == null)
            {
                var found = GameObject.Find("Instructor");
                if (found != null) instructor = found.transform;
            }
            courseView = gameObject.AddComponent<TrainingCourseView>();
            courseView.Initialize(Course, enemy, instructor);
            InitializeRival();
            InitializeForest();
            InitializeClimax();
            InitializeHuman();
            InitializeHumanClimax();
            InitializeRivalSecond();
            InitializeChapterFour();
            InitializeSecondRematch();
            InitializeCalamity();
            InitializeEnding();
            InitializeHunting();
            Effects = gameObject.AddComponent<CombatEffects>();
            if (worldCamera != null)
            {
                cameraBasePosition = worldCamera.transform.position;
                cameraBaseCaptured = true;
            }
            if (spirit != null) spiritFollowPosition = spirit.position;
            UI = gameObject.AddComponent<TrainingUI>();
            UI.Initialize(this);
            Health = definition.playerHealth;
            Hints = definition.hintCharges;
            ResetArena();
            Mode = TrainingMode.Introduction;
            UI.RefreshOverlay();
        }

        void Update()
        {
            if (UI == null) return;
            float dt = Time.deltaTime;
            Effects.Tick(Time.unscaledDeltaTime);
            if (worldCamera != null)
            {
                worldCamera.orthographicSize = Mathf.Max(6.75f, 12f / Mathf.Max(.5f, worldCamera.aspect));
                worldCamera.transform.position = cameraBasePosition + new Vector3(Mathf.Sin(Time.unscaledTime * 123) * Effects.Shake, Mathf.Cos(Time.unscaledTime * 97) * Effects.Shake, 0);
            }
            if (spirit != null)
            {
                var target = player.transform.position + new Vector3(-.9f * player.Facing, 2.35f, 0);
                spiritFollowPosition = Vector3.Lerp(spiritFollowPosition, target, Time.unscaledDeltaTime * spiritFollowSpeed);
                if ((spiritFollowPosition - target).sqrMagnitude <= spiritStopDistance * spiritStopDistance)
                    spiritFollowPosition = target;
                spirit.position = spiritFollowPosition + Vector3.up * (Mathf.Sin(Time.unscaledTime * 2.7f) * .15f);
            }
            if (GameInput.Pressed(Key.Escape))
            {
                if (Mode == TrainingMode.Playing || Mode == TrainingMode.RivalChallenge || Mode == TrainingMode.ForestChallenge || Mode == TrainingMode.HumanChallenge || Mode == TrainingMode.HumanClimaxChallenge || Mode == TrainingMode.RivalSecondChallenge || Mode == TrainingMode.SecondRematchChallenge || Mode == TrainingMode.SecondRematchPhase || Mode == TrainingMode.CalamityChallenge || Mode == TrainingMode.CalamityPhase) Pause();
                else if (Mode == TrainingMode.Paused) Resume();
                else if (Mode == TrainingMode.Quiz) CancelQuiz();
            }
            TickRivalInput(dt);
            TickForestInput();
            TickHumanInput();
            TickHumanClimaxInput();
            TickRivalSecondInput(dt);
            TickChapterFourInput(dt);
            TickSecondRematchInput();
            TickCalamityInput(dt);
            TickEndingInput(dt);
            bool wasHuntingLesson = Mode == TrainingMode.HuntingLesson;
            TickHuntingInput();
            if (wasHuntingLesson) { UI.Tick(); return; }
            if (Mode == TrainingMode.Quiz)
            {
                if (GameInput.Pressed(Key.Digit1)) SubmitAnswer(0);
                else if (GameInput.Pressed(Key.Digit2)) SubmitAnswer(1);
                else if (GameInput.Pressed(Key.Digit3)) SubmitAnswer(2);
                else if (GameInput.Pressed(Key.Digit4)) SubmitAnswer(3);
                if (GameInput.Pressed(Key.H)) UseHint();
            }
            if (Mode == TrainingMode.Playing && Time.unscaledTime >= Effects.FreezeUntil)
            {
                noticeTimer -= dt;
                if (noticeTimer <= 0) Notice = "";
                ambientTimer -= dt;
                if (ambientTimer <= 0) Ambient = "";
                attackCooldown -= dt; quickCooldown -= dt; runeCooldown -= dt; comboTimer -= dt;
                if (comboTimer <= 0) Combo = 0;
                player.Tick(dt, GameInput.Horizontal, GameInput.Jump, GameInput.Crouch);
                ClampSecondRematchPlayer();
                ClampCalamityPlayer();
                if (castDelay > 0)
                {
                    castDelay -= dt;
                    if (castDelay <= 0) DealAttack(definition.runeSkill.damage + Progress.skillBooks * 8, definition.runeSkill.range, true);
                }
                if (GameInput.Attack) Attack();
                if (GameInput.Pressed(Key.K)) QuickSkill();
                if (GameInput.Pressed(Key.Q)) BeginRune();
                if (GameInput.Pressed(Key.E)) Interact();
                if (GameInput.Pressed(Key.F5)) SaveCheckpoint();
                if (GameInput.Pressed(Key.F9)) LoadCheckpoint();
                if (Mode == TrainingMode.Playing) TickEnemy(dt);
                if (!InHunting && !ambientPlayed && player.transform.position.x > -4.8f)
                {
                    ambientPlayed = true;
                    Ambient = Human.InVillage ? (Progress.human.CurrentBossDefeated ? Human.Definition.clearedAmbient : Human.Definition.ambient) : Forest.InVillage ? Forest.Definition.ambient : definition.dialogue.ambient != null && definition.dialogue.ambient.Length > 0 ? definition.dialogue.ambient[0] : "";
                    ambientTimer = 6;
                }
            }
            UI.Tick();
        }

        void TickEnemy(float dt)
        {
            if (InHunting) { TickHuntingEnemy(dt); return; }
            if (Calamity.Active) { Calamity.Tick(dt); return; }
            if (SecondRematch.Active) { SecondRematch.Tick(dt); return; }
            if (!enemy.gameObject.activeInHierarchy) return;
            if (RivalSecond.Active) return;
            if (HumanClimax.Active)
            {
                HumanClimax.TickCombat(dt);
                if (HumanClimax.Active && Mode == TrainingMode.Playing) TickHumanClimaxAttack(dt);
                return;
            }
            if (Human.BossActive)
            {
                if (enemy.Phase == EnemyPhase.Defeated) return;
                Human.TickCombat(dt);
                if (Mode != TrainingMode.Playing) return;
            }
            if (Forest.BossActive)
            {
                if (enemy.Phase == EnemyPhase.Defeated) return;
                Forest.TickCombat(dt);
                if (Mode != TrainingMode.Playing) return;
            }
            if (Rival.InEncounter)
            {
                if (enemy.Phase == EnemyPhase.Defeated) return;
                Rival.TickCombat(dt);
                if (Mode != TrainingMode.Playing) return;
            }
            if (enemy.Phase == EnemyPhase.Defeated)
            {
                respawn -= dt;
                if (respawn <= 0) enemy.ResetEnemy(Stage.enemyHealth, Stage.enemyWindup, DefeatedCount);
                return;
            }
            // The instructor's side of the courtyard is a safe checkpoint.
            if (!Rival.InEncounter && !Forest.BossActive && !Human.BossActive && !HumanClimax.Active && !RivalSecond.Active && player.transform.position.x < -5.2f)
            {
                if (Trials.SparringActive)
                {
                    ResetArena();
                    Notify("교관: 여기까지 하자. 준비되면 다시 말을 걸어라.");
                    return;
                }
                if (enemy.Phase == EnemyPhase.Windup || enemy.transform.position.x < -1)
                    enemy.ResetEnemy(Stage.enemyHealth, Stage.enemyWindup);
                return;
            }
            if (!enemy.Tick(dt, player)) return;
            float distance = Mathf.Abs(player.transform.position.x - enemy.transform.position.x);
            Effects.Slash(enemy.transform.position + new Vector3(0, enemy.LowSweep ? .3f : 1.35f), player.transform.position.x < enemy.transform.position.x ? -1 : 1, false);
            if (distance > 3.2f || player.Dodges(enemy.LowSweep))
            { if (distance <= 3.2f) Notify("빈틈이 열렸다. 지금 반격!", 1.4f); return; }
            if (player.Invulnerable) return;
            player.KnockBack(Mathf.Sign(player.transform.position.x - enemy.transform.position.x));
            DamagePlayer(Human.BossActive ? Human.Definition.attackDamage : Forest.BossActive ? Forest.AttackDamage : Rival.InEncounter ? Rival.Definition.swordDamage : Stage.enemyDamage, false);
        }

        public void AdvanceIntro()
        {
            if (Mode != TrainingMode.Introduction) return;
            introIndex++;
            if (introIndex >= definition.dialogue.introduction.Length)
            { SetMode(TrainingMode.Playing); Notify("오른쪽에서 전투를 연습하거나, 왼쪽 교관에게 다가가 E를 누르세요.", 7); }
            else UI.RefreshOverlay();
        }

        public void Attack()
        {
            if (Calamity.Transformed) return;
            if (Mode != TrainingMode.Playing || attackCooldown > 0 || castDelay > 0) return;
            attackCooldown = .3f;
            DealAttack(12 + Mathf.Min(Combo, 4), 2.25f, false);
        }

        public void QuickSkill()
        {
            if (Calamity.Transformed) return;
            if (Mode != TrainingMode.Playing || quickCooldown > 0 || castDelay > 0) return;
            quickCooldown = 1.7f;
            DealAttack(18, 3.1f, false);
        }

        void DealAttack(int damage, float range, bool heavy)
        {
            player.Swing();
            Effects.Slash(player.transform.position + Vector3.up, player.Facing, heavy);
            var delta = enemy.transform.position - player.transform.position;
            if (!HasOpponent || Mathf.Abs(delta.x) > range || delta.x * player.Facing < -.45f || player.Height > 1.9f)
            { if (heavy) Notify("검기가 허공을 갈랐다. 적을 바라보고 거리를 좁혀보자."); return; }
            if (Calamity.Active)
            {
                int dealt = Calamity.ReceiveHit(damage);
                if (dealt > 0) Effects.Impact(enemy.transform.position + Vector3.up, heavy, hitVisual: EnemyHitVisual);
                Combo++;
                comboTimer = 2.5f;
                UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, dealt.ToString(), heavy);
                return;
            }
            if (SecondRematch.Active)
            {
                int dealt = SecondRematch.ReceiveHit(damage);
                if (dealt > 0) Effects.Impact(enemy.transform.position + Vector3.up, heavy, hitVisual: EnemyHitVisual);
                Combo++;
                comboTimer = 2.5f;
                UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, dealt.ToString(), heavy);
                return;
            }
            if (RivalSecond.Active)
            {
                int dealt = RivalSecond.ReceiveHit(damage);
                if (dealt > 0) Effects.Impact(enemy.transform.position + Vector3.up, heavy, hitVisual: EnemyHitVisual);
                Combo++;
                comboTimer = 2.5f;
                UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, dealt.ToString(), heavy);
                return;
            }
            if (Guardian.Active) damage = Guardian.ModifyIncomingDamage(damage);
            bool killed = enemy.ReceiveDamage(damage, player.Facing, heavy);
            if (damage > 0) Effects.Impact(enemy.transform.position + Vector3.up, heavy, hitVisual: EnemyHitVisual);
            Combo++;
            comboTimer = 2.5f;
            UI.FloatText(enemy.transform.position + Vector3.up * 1.5f, damage.ToString(), heavy);
            if (InHunting)
            {
                if (killed) RecordHuntingKill();
                return;
            }
            if (HumanClimax.Active)
            {
                HumanClimax.AfterDamage();
                return;
            }
            if (Human.BossActive)
            {
                if (killed) Human.Win();
                return;
            }
            if (Forest.BossActive)
            {
                if (killed) Forest.Win();
                return;
            }
            if (Rival.InEncounter)
            {
                if (killed) Rival.Win();
                return;
            }
            if (killed)
            {
                DefeatedCount++;
                respawn = 3;
                Health = Mathf.Min(definition.playerHealth, Health + 8);
                Notify("검이 가벼워졌다. 생명 +8  ·  다음 상대가 곧 나타납니다.", 3);
                int oldKills = StageProgress.huntKills;
                Trials.EnemyDefeated();
                if (Mode != TrainingMode.Reward && oldKills != StageProgress.huntKills) WriteCheckpoint();
            }
        }

        public void BeginRune()
        {
            if (Mode != TrainingMode.Playing || runeCooldown > 0 || castDelay > 0) return;
            if (InHunting) { OpenHuntingQuestion(); return; }
            if (Calamity.Active) { Calamity.OpenQuestion(); return; }
            if (SecondRematch.Active) { SecondRematch.OpenQuestion(); return; }
            if (RivalSecond.Active) { RivalSecond.OpenQuestion(); return; }
            if (!HasOpponent) { Notify("지금은 맞설 상대가 없습니다."); return; }
            if (Mathf.Abs(enemy.transform.position.x - player.transform.position.x) > definition.runeSkill.range)
            { Notify("룬의 사거리 밖이다. 적에게 조금 더 다가가자."); return; }
            InstructorQuiz = false;
            ChestQuiz = false;
            if (Progress.skillBooks >= definition.runeSkill.booksForInstantCast) { QueueRune(); return; }
            var questionPool = HumanClimax.Active ? HumanClimax.Definition.questions : Human.BossActive ? Human.Definition.questions : Guardian.Active ? Guardian.Definition.questions : Forest.BossActive ? Forest.Definition.questions : Rival.InEncounter ? Rival.Definition.questions : Stage.questions;
            Quiz = new QuizSession(questionPool[questionCursor++ % questionPool.Length], Hints, Progress.skillBooks > 0);
            SetMode(TrainingMode.Quiz);
        }

        public void BeginExam()
        {
            if (InHunting) return;
            if (Mode != TrainingMode.Playing || InSecondRematch || !NearInstructor || castDelay > 0 || Trials.SparringActive || Rival.InEncounter || Forest.InVillage || Human.InVillage) return;
            if (StageProgress.cleared)
            { Health = definition.playerHealth; Hints = definition.hintCharges; Notify("교관: 몸과 룬을 정비해 두었다. 배운 감각을 잊지 마라.", 4); return; }
            Trials.Begin();
            if (Mode == TrainingMode.Reward) return;
            WriteCheckpoint();
            Notify(Stage.briefing, 6);
            if (Stage.trialKind == InstructorTrialKind.Questions)
            {
                InstructorQuiz = true;
                ChestQuiz = false;
                Quiz = new QuizSession(Stage.questions[0], Hints);
                SetMode(TrainingMode.Quiz);
            }
            else if (Stage.trialKind == InstructorTrialKind.Sparring)
            {
                Health = definition.playerHealth;
                Hints = definition.hintCharges;
                player.ResetAtCheckpoint();
                player.transform.position = new Vector3(-1, PlayerMotor.GroundY, 0);
                attackCooldown = quickCooldown = runeCooldown = castDelay = 0;
                Combo = 0;
                enemy.ResetEnemy(Stage.enemyHealth, Stage.enemyWindup);
                courseView.SetOpponent(true, true);
            }
        }

        public void Interact()
        {
            if (Mode != TrainingMode.Playing || castDelay > 0) return;
            if (InHunting) { InteractWithHunting(); return; }
            if (InCalamity) { Notify("ESC로 잠시 쉬거나 정령 격리 직전부터 재도전할 수 있습니다."); return; }
            if (InSecondRematch) { Notify("Q로 룬 대결, ESC로 잠시 쉬거나 재도전할 수 있습니다."); return; }
            if (RivalSecond.Active) { Notify("친구가 앞을 막고 있다. ESC로 잠시 쉴 수 있습니다."); return; }
            if (HumanClimax.Active) { Notify("ESC에서 마을로 돌아가면 재난 보스전을 중단할 수 있습니다."); return; }
            if (Human.InVillage) { InteractWithHuman(); return; }
            if (Forest.InVillage) { InteractWithForest(); return; }
            if (Rival.InEncounter) { Notify("결투를 중단하려면 ESC에서 훈련소로 돌아가기를 선택하세요."); return; }
            if (player.transform.position.x <= TrainingCourseView.EntranceX) MoveTraining(-1);
            else if (player.transform.position.x >= TrainingCourseView.ExitX) MoveTraining(1);
            else if (NearInstructor) BeginExam();
            else if (NearbyChest >= 0) BeginChest(NearbyChest);
        }

        void MoveTraining(int direction)
        {
            if (Trials.SparringActive) { Notify("대련 중에는 이동할 수 없습니다. 왼쪽으로 물러나면 대련을 중단합니다."); return; }
            if (direction > 0 && Course.Index < 3 && StageProgress.cleared && RequireHuntingGround(1, Course.Index)) return;
            if (direction > 0 && Course.Index == 3 && Course.Finished)
            { if (Progress.rivalDefeated) Forest.Enter(); else Rival.Begin(); return; }
            if (!Course.Move(direction))
            {
                Notify(direction < 0 ? "첫 훈련소입니다." : !StageProgress.cleared ? "이곳 교관의 인정을 받아야 길이 열립니다." : "네 교관의 인정을 모두 받았습니다. 이번 제작 범위의 끝입니다.", 4);
                return;
            }
            ResetArena();
            ambientPlayed = false;
            Ambient = "";
            questionCursor = 0;
            WriteCheckpoint();
            Notify(Stage.locationName + "에 도착했습니다. 왼쪽 교관에게 말을 걸어보자.", 5);
        }

        public void BeginChest(int index)
        {
            if (Mode != TrainingMode.Playing || index < 0 || index > 1 || NearbyChest != index || castDelay > 0) return;
            if (StageProgress.ChestOpen(index)) { Notify("이미 열린 상자입니다."); return; }
            quizChest = index;
            ChestQuiz = true;
            InstructorQuiz = false;
            Quiz = new QuizSession(Stage.chestQuestions[index], Hints);
            SetMode(TrainingMode.Quiz);
        }

        public void SubmitAnswer(int index)
        {
            if (Mode != TrainingMode.Quiz || Quiz == null) return;
            HandleQuizAnswer(Quiz.Answer(index));
        }

        public void SubmitBlocks(BlockSubmission submission)
        {
            if (Mode != TrainingMode.Quiz || Quiz == null) return;
            HandleQuizAnswer(Quiz.AnswerBlocks(submission), true);
        }

        public void BeginBlockTest(int index)
        {
            if (!CanStartBlockTest || index < 0 || index > 1) return;
            var question = Resources.Load<QuestionDefinition>("MathRPG/BlockCalculatorTests/" + (index == 0 ? "Arithmetic" : "Quadratic"));
            if (question == null || !question.IsValid(out _))
            {
                Notify("Math RPG > Create Block Calculator Test Questions로 임시 문항을 먼저 생성하세요.", 7);
                return;
            }
            InstructorQuiz = ChestQuiz = false;
            BlockTestQuiz = true;
            Quiz = new QuizSession(question, Hints);
            SetMode(TrainingMode.Quiz);
        }

        void HandleQuizAnswer(AnswerResult result, bool fromBlocks = false)
        {
            if (result == AnswerResult.Wrong)
            {
                if (ChestQuiz)
                { UI.RefreshOverlay("봉인이 이어지지 않았다. 피해 없이 다시 선택할 수 있습니다."); return; }
                int wrongDamage = InHunting ? HuntingArea.wrongAnswerDamage : definition.wrongAnswerDamage;
                DamagePlayer(wrongDamage, true);
                if (Mode == TrainingMode.GameOver) return;
                UI.RefreshOverlay((fromBlocks ? "조합을 다시 골라보자.  생명 -" : "룬이 흔들렸다. 지워진 길을 피해 다시 이어보자.  생명 -") + wrongDamage);
            }
            else if (result == AnswerResult.Advanced) UI.RefreshOverlay("빛이 모인다. 마지막 흐름을 이어보자.");
            else if (result == AnswerResult.Completed)
            {
                if (BlockTestQuiz)
                {
                    int mistakes = Quiz.Mistakes;
                    Quiz = null;
                    BlockTestQuiz = false;
                    SetMode(TrainingMode.Playing);
                    Notify("계산기 테스트 완료 · 오답 " + mistakes + "회. 같은 버튼으로 다시 시작할 수 있습니다.", 5);
                    return;
                }
                if (!ChestQuiz) Health = Mathf.Min(definition.playerHealth, Health + 4);
                if (InstructorQuiz)
                {
                    Trials.AnswerQuestion();
                    if (Mode != TrainingMode.Reward)
                    {
                        Quiz = new QuizSession(Stage.questions[ExamCompleted % Stage.questions.Length], Hints);
                        UI.RefreshOverlay("교관: 좋다. 이번에는 다른 흐름을 읽어봐라.");
                    }
                }
                else if (ChestQuiz)
                {
                    Quiz = null;
                    ChestQuiz = false;
                    SetMode(TrainingMode.Playing);
                    Trials.OpenChest(quizChest);
                    if (Mode != TrainingMode.Reward)
                    {
                        WriteCheckpoint();
                        Notify(StageProgress.trialAccepted ? "상자의 봉인이 풀렸다. 나머지 상자도 찾아보자." : "상자의 봉인이 풀렸다. 교관에게 말을 걸어보자.");
                    }
                    quizChest = -1;
                }
                else { Quiz = null; SetMode(TrainingMode.Playing); QueueRune(); }
            }
        }

        void QueueRune()
        {
            runeCooldown = definition.runeSkill.cooldown;
            castDelay = .22f;
            Effects.Charge(player.transform.position + Vector3.up);
            Notify(definition.runeSkill.displayName + " — 발동", 1.3f);
        }

        void CompleteExam()
        {
            Health = definition.playerHealth;
            Hints = definition.hintCharges;
            Quiz = null;
            InstructorQuiz = ChestQuiz = false;
            castDelay = 0;
            if (Stage.trialKind == InstructorTrialKind.Sparring) courseView.SetOpponent(false, false);
            SetMode(TrainingMode.Reward);
            WriteCheckpoint();
        }

        public void UseHint()
        {
            if (Mode != TrainingMode.Quiz || Quiz == null || !Quiz.UseHint()) return;
            Hints = Quiz.HintsRemaining;
            UI.RefreshOverlay();
        }

        public void CancelQuiz()
        {
            if (Mode != TrainingMode.Quiz) return;
            Quiz = null;
            BlockTestQuiz = false;
            InstructorQuiz = ChestQuiz = false;
            quizChest = -1;
            SetMode(TrainingMode.Playing);
            Notify("룬을 거두었다. 준비되면 다시 시도하자.");
        }

        public void DamagePlayer(int damage, bool fromQuiz)
        {
            if (Mode == TrainingMode.GameOver) return;
            Health = Mathf.Max(RivalSecond.Active ? 1 : 0, Health - Mathf.Max(0, damage));
            Combo = 0;
            Effects.Impact(player.transform.position + Vector3.up, !fromQuiz, true);
            if (Health == 0)
            {
                Progress.deaths++;
                castDelay = 0;
                Quiz = null;
                SetMode(TrainingMode.GameOver);
            }
        }

        public void Resume()
        {
            if (Mode == TrainingMode.Paused) SetMode(resumeMode);
            else if (Mode == TrainingMode.Reward) SetMode(TrainingMode.Playing);
        }
        public void Pause()
        {
            if (Mode != TrainingMode.Playing && Mode != TrainingMode.RivalChallenge && Mode != TrainingMode.ForestChallenge && Mode != TrainingMode.HumanChallenge && Mode != TrainingMode.HumanClimaxChallenge && Mode != TrainingMode.RivalSecondChallenge && Mode != TrainingMode.SecondRematchChallenge && Mode != TrainingMode.SecondRematchPhase && Mode != TrainingMode.CalamityChallenge && Mode != TrainingMode.CalamityPhase) return;
            resumeMode = Mode;
            SetMode(TrainingMode.Paused);
        }

        public void Rewind()
        {
            if (Mode != TrainingMode.GameOver) return;
            if (InHunting) { RetryHunting(); return; }
            if (Calamity.Active) { Calamity.Retry(); return; }
            if (SecondRematch.Active) { SecondRematch.StartDuel(); return; }
            bool retryRival = Rival.InEncounter;
            ResetArena();
            SetMode(TrainingMode.Playing);
            if (retryRival) { Rival.Begin(true); return; }
            Notify(Human.InVillage ? "정령이 시간을 되감았다. " + Human.Definition.villageName + "에서 다시 준비하자." : Forest.InVillage ? "정령이 시간을 되감았다. " + Forest.Definition.villageName + "에서 다시 준비하자." : "정령이 연무장의 시간을 되감았다.", 4);
        }

        void ResetArena()
        {
            Effects?.ResetFeedback();
            if (worldCamera != null && cameraBaseCaptured) worldCamera.transform.position = cameraBasePosition;
            huntingView?.Hide();
            BlockTestQuiz = false;
            Rival?.Reset();
            Guardian?.Reset();
            Forest?.ResetCombat();
            Human?.ResetCombat();
            HumanClimax?.Reset();
            RivalSecond?.Reset();
            SecondRematch?.Reset();
            secondRematchView?.Hide();
            Calamity?.Reset();
            calamityView?.Hide();
            player.gameObject.SetActive(true);
            if (spirit != null) spirit.gameObject.SetActive(true);
            Trials.ResetSession();
            Health = definition.playerHealth;
            Hints = definition.hintCharges;
            attackCooldown = quickCooldown = runeCooldown = castDelay = 0;
            Combo = 0;
            Quiz = null;
            InstructorQuiz = ChestQuiz = false;
            quizChest = -1;
            respawn = 3;
            player.ResetAtCheckpoint();
            enemy.ResetEnemy(Stage.enemyHealth, Stage.enemyWindup);
            courseView.SetOpponent(Stage.trialKind == InstructorTrialKind.Questions || Stage.trialKind == InstructorTrialKind.Hunt, false);
            ApplyForestRegion();
            ApplyHumanRegion();
        }

        public void SaveCheckpoint()
        {
            if (InHunting)
            {
                if ((Mode == TrainingMode.Playing || Mode == TrainingMode.HuntingLesson) && SaveHunting()) Notify("사냥터 진행을 기억했습니다.");
                return;
            }
            if (EndingScreen) { SaveEndingRecord(); return; }
            if (InCalamity)
            {
                Notify("최종보스 전투 중에는 기록을 덮어쓰지 않습니다. 정령 격리 직전의 기억이 남아 있습니다.", 5);
                return;
            }
            if (SecondRematch.Active && Mode == TrainingMode.Playing)
            {
                if (SaveSecondRematch(RematchProgress.Duel)) Notify("결투 시작을 기억했습니다. 불러오면 개념 안내에서 다시 준비합니다.", 5);
                return;
            }
            if (!CanSave) { Notify("왼쪽의 푸른 표식으로 돌아오면 기억을 남길 수 있습니다."); return; }
            Health = definition.playerHealth;
            Hints = definition.hintCharges;
            WriteCheckpoint();
        }

        void WriteCheckpoint()
        {
            if (CheckpointStore.Save(SavePath, Progress, out var error)) Notify("현재 위치와 진행을 기억했습니다.", 4);
            else { Notify("기억을 남기지 못했습니다: " + error, 8); Debug.LogWarning(error); }
        }

        public void LoadCheckpoint()
        {
            if (Mode != TrainingMode.Playing && Mode != TrainingMode.Paused && Mode != TrainingMode.Introduction && Mode != TrainingMode.HuntingLesson && Mode != TrainingMode.HumanChapterComplete && Mode != TrainingMode.ChapterFourStory &&
                Mode != TrainingMode.SecondRematchIntroduction && Mode != TrainingMode.SecondRematchLesson && Mode != TrainingMode.SecondRematchVictory && Mode != TrainingMode.SecondRematchDeparture &&
                Mode != TrainingMode.CalamityIntroduction && Mode != TrainingMode.CalamityFarewell && Mode != TrainingMode.CalamityIsolation && Mode != TrainingMode.CalamityVictory && !EndingScreen) return;
            if (!CheckpointStore.Load(SavePath, out var loaded, out var error))
            { Notify(error, 5); UI.RefreshOverlay(error); return; }
            if (loaded.hunting.activeIndex >= 0 && !HuntingDataReady()) return;
            if (loaded.forest.inVillage && !Forest.IsVillageAvailable(loaded.forest.villageIndex))
            { UI.RefreshOverlay("숲 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요."); return; }
            if (loaded.human.inVillage && !Human.IsVillageAvailable(loaded.human.villageIndex))
            { Notify("인간 마을 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); UI.RefreshOverlay(Notice); return; }
            if (loaded.human.inVillage && (loaded.human.letterReceived || loaded.human.climaxDefeated) && !HumanClimax.HasValidData)
            { Notify("재난 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); UI.RefreshOverlay(Notice); return; }
            if ((loaded.human.sealTaken || loaded.human.chapter3Complete) && !RivalSecond.HasValidData)
            { Notify("라이벌2 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); UI.RefreshOverlay(Notice); return; }
            if (loaded.chapterFour.Started && !HasChapterFourData)
            { UI.RefreshOverlay("챕터4 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요."); return; }
            if (loaded.chapterFour.secondRematch != RematchProgress.None && !SecondRematch.HasValidData)
            { UI.RefreshOverlay("리매치 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요."); return; }
            if (loaded.chapterFour.calamity != CalamityProgress.None && !Calamity.HasValidData)
            { UI.RefreshOverlay("최종보스 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요."); return; }
            if (loaded.chapterFour.ending != EndingBeat.None && !HasEndingData)
            { UI.RefreshOverlay("엔딩 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요."); return; }
            chapterFourAudio.Stop();
            Progress = loaded;
            Course.Restore(loaded);
            if (InHunting) { SetupHunting(); return; }
            ResetArena();
            ambientPlayed = false;
            Ambient = "";
            questionCursor = 0;
            SetMode(TrainingMode.Playing);
            if (Forest.InVillage && !Progress.forest.CurrentVisited) SetMode(TrainingMode.ForestArrival);
            if (Human.InVillage && !Progress.human.CurrentVisited) SetMode(TrainingMode.HumanArrival);
            HumanClimax.RestoreEvent();
            if (InChapterFour) RestoreChapterFour();
            else RivalSecond.RestoreEvent();
            if (!InChapterFour) Notify("정령: 좋아, 우리가 기억한 곳이야.", 4);
        }

        public void Notify(string message, float seconds = 3) { Notice = message; noticeTimer = seconds; }
        void SetMode(TrainingMode mode) { Mode = mode; UI.RefreshOverlay(); }
        void OnDisable()
        {
            Effects?.ResetFeedback();
            if (worldCamera != null && cameraBaseCaptured) worldCamera.transform.position = cameraBasePosition;
        }

        void OnDestroy() { if (Trials != null) Trials.Completed -= CompleteExam; DisposeChapterFour(); }
    }
}
