namespace MathRPG
{
    public sealed class HumanVillageEncounter
    {
        readonly TrainingGame game;
        float delay;
        int cursor;
        readonly HumanVillageDefinition firstVillage;
        public HumanVillageDefinition Definition => GetVillage(Progress.villageIndex);
        public HumanVillageProgress Progress => game.Progress.human;
        public bool InVillage => Progress.inVillage;
        public bool BossActive { get; private set; }
        public QuizSession Quiz { get; private set; }
        public bool HasValidData => Definition != null && Definition.IsValid();

        public HumanVillageEncounter(TrainingGame game, HumanVillageDefinition definition)
        { this.game = game; firstVillage = definition; }

        public bool IsVillageAvailable(int index)
        {
            var data = GetVillage(index);
            return data != null && data.IsValid();
        }

        HumanVillageDefinition GetVillage(int index)
        {
            if (index < 0 || index > 2) return null;
            var data = firstVillage;
            for (int i = 0; i < index && data != null; i++) data = data.nextVillage;
            return data;
        }

        void MoveToVillage(int index)
        {
            if (!IsVillageAvailable(index)) { game.Notify("마을 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); return; }
            Progress.villageIndex = index;
            game.PrepareHumanVillage();
            game.SaveHumanProgress();
            game.ShowHumanMode(Progress.CurrentVisited ? TrainingMode.Playing : TrainingMode.HumanArrival);
        }

        public void Enter()
        {
            if (InVillage || !game.Progress.forest.aggregateDefeated || !game.Forest.InVillage || game.Forest.BossActive ||
                game.Progress.forest.villageIndex != 2 ||
                (game.Mode != TrainingMode.Playing && game.Mode != TrainingMode.ForestChapterClear)) return;
            if (!IsVillageAvailable(0))
            { game.Notify("인간 마을 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); return; }
            game.Progress.forest.inVillage = false;
            Progress.villageIndex = 0;
            Progress.inVillage = true;
            game.PrepareHumanVillage();
            game.SaveHumanProgress();
            game.ShowHumanMode(Progress.CurrentVisited ? TrainingMode.Playing : TrainingMode.HumanArrival);
        }

        public void FinishArrival()
        {
            if (!InVillage || game.Mode != TrainingMode.HumanArrival) return;
            Progress.CurrentVisited = true;
            game.SaveHumanProgress();
            game.ShowHumanMode(TrainingMode.Playing);
            game.Notify("왼쪽 " + Definition.residentName + "에게 다가가 E로 말을 걸어보자.", 5);
        }

        public void Talk()
        {
            if (!InVillage || BossActive || game.Mode != TrainingMode.Playing) return;
            game.ShowHumanMode(TrainingMode.HumanRequest);
        }

        public void AcceptRequest()
        {
            if (!InVillage || game.Mode != TrainingMode.HumanRequest) return;
            Progress.CurrentQuestAccepted = true;
            game.RestoreHumanResources();
            game.SaveHumanProgress();
            game.ShowHumanMode(TrainingMode.Playing);
            game.Notify(Progress.CurrentBossDefeated ? "길은 열렸지만, 사람들의 불안은 남아 있다." : "오른쪽 길에서 E를 눌러 " + Definition.bossName + "에게 향하자.", 5);
        }

        public void StartBoss()
        {
            if (game.InHunting) return;
            if (!InVillage || BossActive || game.Mode != TrainingMode.Playing) return;
            if (!Progress.CurrentQuestAccepted) { game.Notify("먼저 " + Definition.residentName + "의 의뢰를 들어보자."); return; }
            if (Progress.CurrentBossDefeated)
            {
                if (Progress.villageIndex < 2) MoveToVillage(Progress.villageIndex + 1);
                else game.HumanClimax.Enter();
                return;
            }
            if (game.RequireHuntingGround(3, Progress.villageIndex)) return;
            ResetCombat();
            BossActive = true;
            delay = Definition.firstQuestionDelay;
            game.PrepareHumanBoss();
            game.Notify(Definition.bossName + "가 길을 막는다. 흩어진 식의 흐름을 읽어보자.", 5);
        }

        public void TickCombat(float dt)
        {
            if (!BossActive || game.Mode != TrainingMode.Playing || game.enemy.Health <= 0) return;
            delay -= dt;
            if (delay > 0) return;
            Quiz = new QuizSession(Definition.questions[cursor++ % Definition.questions.Length], game.Hints);
            game.ShowHumanMode(TrainingMode.HumanChallenge);
        }

        public void Submit(int index)
        {
            if (!BossActive || game.Mode != TrainingMode.HumanChallenge || Quiz == null) return;
            var result = Quiz.Answer(index);
            if (result == AnswerResult.Wrong)
            {
                game.DamagePlayer(Definition.wrongDamage, true);
                if (game.Mode != TrainingMode.GameOver)
                    game.UI.RefreshOverlay("흐름이 흔들렸다. 생명 -" + Definition.wrongDamage + " · 남은 보기를 다시 살펴보자.");
            }
            else if (result == AnswerResult.Advanced) game.UI.RefreshOverlay("식을 풀어냈다. 남은 연결을 이어보자.");
            else if (result == AnswerResult.Completed)
            {
                Quiz = null;
                delay = Definition.questionInterval;
                game.ShowHumanMode(TrainingMode.Playing);
                game.ResolveHumanRune();
                if (game.enemy.Health <= 0) Win();
            }
        }

        public void UseHint()
        {
            if (!BossActive || game.Mode != TrainingMode.HumanChallenge || Quiz == null || !Quiz.UseHint()) return;
            game.SetHumanHints(Quiz.HintsRemaining);
            game.UI.RefreshOverlay();
        }

        public void Win()
        {
            if (!BossActive || game.enemy.Health > 0 || game.Health <= 0 || Progress.CurrentBossDefeated) return;
            Progress.CurrentBossDefeated = true;
            game.RecordHumanVictory();
        }

        public void ReturnToVillage()
        {
            if (!InVillage || game.Mode == TrainingMode.GameOver) return;
            game.PrepareHumanVillage();
            game.ShowHumanMode(TrainingMode.Playing);
            game.Notify(Definition.villageName + "로 돌아왔습니다.");
        }

        public void Leave()
        {
            if (!InVillage || BossActive || game.Mode != TrainingMode.Playing) return;
            if (Progress.villageIndex > 0) { MoveToVillage(Progress.villageIndex - 1); return; }
            if (!game.Forest.IsVillageAvailable(2)) { game.Notify("숲 마을 데이터를 먼저 생성해주세요."); return; }
            Progress.inVillage = false;
            game.Progress.forest.inVillage = true;
            game.Progress.forest.villageIndex = 2;
            game.PrepareHumanVillage();
            game.SaveHumanProgress();
            game.ShowHumanMode(TrainingMode.Playing);
            game.Notify("안개뜰로 돌아왔습니다.");
        }

        public void ResetCombat() { BossActive = false; Quiz = null; cursor = 0; delay = 0; }
    }
}
