using UnityEngine;

namespace MathRPG
{
    public sealed class ForestEncounter
    {
        readonly TrainingGame game;
        readonly ForestVillageDefinition[] villages;
        float questionDelay, gimmickRemaining, patternDelay;
        int questionIndex, gimmicksUsed, patternIndex;
        bool villageBossActive, chestActive;

        public ForestVillageDefinition Definition => GetVillage(Progress.villageIndex);
        public ForestVillageDefinition GetVillage(int index) => index >= 0 && index < villages.Length ? villages[index] : null;
        public ForestProgressData Progress => game.Progress.forest;
        public bool InVillage => Progress.inVillage;
        public bool BossActive => villageBossActive || (game.Guardian != null && game.Guardian.Active);
        public bool ChestActive => chestActive;
        public bool TimedChallenge => villageBossActive && !chestActive;
        public float ChallengeTime => gimmickRemaining;
        public QuizSession ActiveQuiz => game.Guardian.Active ? game.Guardian.Quiz : Quiz;
        public string CombatName => game.Guardian.Active ? game.Guardian.Definition.bossName : Definition.bossName;
        public int AttackDamage => game.Guardian.Active ? game.Guardian.Definition.attackDamage : Definition.attackDamage;
        public QuizSession Quiz { get; private set; }
        public bool HasValidData => Definition != null && Definition.IsValid();

        public ForestEncounter(TrainingGame game, params ForestVillageDefinition[] villages)
        { this.game = game; this.villages = villages; }

        public bool IsVillageAvailable(int index) { var data = GetVillage(index); return data != null && data.IsValid(); }

        public void Enter()
        {
            if (!game.Progress.rivalDefeated || game.Course.Index != 3 ||
                (game.Mode != TrainingMode.Playing && game.Mode != TrainingMode.ChapterClear) || InVillage) return;
            if (!HasValidData) { game.Notify("숲 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); return; }
            Progress.inVillage = true;
            game.PrepareForestVillage();
            game.SaveForestProgress();
            game.ShowForestMode(Progress.CurrentVisited ? TrainingMode.Playing : TrainingMode.ForestArrival);
        }

        public void FinishArrival()
        {
            if (!InVillage || game.Mode != TrainingMode.ForestArrival) return;
            Progress.CurrentVisited = true;
            game.SaveForestProgress();
            game.ShowForestMode(TrainingMode.Playing);
            game.Notify("왼쪽의 " + Definition.guideName + "에게 다가가 E로 말을 걸어보자.", 5);
        }

        public void Talk()
        {
            if (!InVillage || BossActive || game.Mode != TrainingMode.Playing) return;
            game.ShowForestMode(TrainingMode.ForestRequest);
        }

        public void AcceptRequest()
        {
            if (!InVillage || game.Mode != TrainingMode.ForestRequest) return;
            Progress.CurrentQuestAccepted = true;
            game.RestoreForestResources();
            game.SaveForestProgress();
            game.ShowForestMode(TrainingMode.Playing);
            game.Notify(Progress.CurrentRelic ? Definition.guideName + ": 숲이 네가 한 일을 기억할 거야." :
                Definition.encounterKind == ForestEncounterKind.Chest ? "오른쪽 문에서 봉인 상자를 열어보자." : "오른쪽 문에서 기다리는 상대와 맞서자.", 5);
        }

        public void StartBoss()
        {
            if (game.InHunting || !InVillage || BossActive || game.Mode != TrainingMode.Playing) return;
            if (!Progress.CurrentQuestAccepted) { game.Notify("먼저 " + Definition.guideName + "의 이야기를 들어보자."); return; }
            if (Progress.CurrentRelic) { MoveNext(); return; }
            if (game.RequireHuntingGround(2, Progress.villageIndex)) return;
            villageBossActive = true;
            chestActive = Definition.encounterKind == ForestEncounterKind.Chest;
            Quiz = null;
            questionIndex = gimmicksUsed = patternIndex = 0;
            questionDelay = chestActive ? 0 : Definition.firstQuestionDelay;
            patternDelay = 2.5f;
            game.PrepareForestBoss();
            game.SetForestBossVisible(!chestActive);
            if (chestActive) BeginChallenge();
            else game.Notify(Definition.bossName + "의 움직임을 읽고 장갑 마법에 대비하자.", 5);
        }

        public void TickCombat(float dt)
        {
            if (game.Guardian.Active) { game.Guardian.TickCombat(dt); return; }
            if (!villageBossActive || chestActive || game.Mode != TrainingMode.Playing || game.enemy.Health <= 0) return;
            patternDelay -= dt;
            if (patternDelay <= 0) { RunPattern(); patternDelay = 3.2f; }
            if (gimmicksUsed >= Definition.gimmickCount) return;
            questionDelay -= dt;
            if (questionDelay <= 0) BeginChallenge();
        }

        public void TickChallenge(float dt)
        {
            if (game.Guardian.Active) { game.Guardian.TickChallenge(dt); return; }
            if (!TimedChallenge || game.Mode != TrainingMode.ForestChallenge || Quiz == null) return;
            gimmickRemaining -= dt;
            if (gimmickRemaining <= 0) game.DamagePlayer(game.Health, true);
        }

        void BeginChallenge()
        {
            Quiz = new QuizSession(Definition.questions[questionIndex++ % Definition.questions.Length], game.Hints);
            gimmickRemaining = Definition.gimmickTime;
            gimmicksUsed++;
            game.ShowForestMode(TrainingMode.ForestChallenge);
        }

        void RunPattern()
        {
            patternIndex = (patternIndex + 1) % 3;
            if (Definition.encounterKind == ForestEncounterKind.Collector)
            {
                if (patternIndex == 0 && !game.player.Crouching) game.DamagePlayer(Definition.attackDamage, false);
                else if (patternIndex == 1 && game.player.Grounded) game.DamagePlayer(Definition.attackDamage, false);
                else if (patternIndex == 2) game.Notify("그물에 걸렸다! A/D를 번갈아 눌러 벗어나자.", 2);
            }
            else
            {
                if (patternIndex < 2 && game.player.Grounded) game.DamagePlayer(Definition.attackDamage, false);
                else if (Mathf.Abs(game.player.transform.position.x - game.enemy.transform.position.x) < 3)
                    game.DamagePlayer(Definition.attackDamage * 2, false);
            }
        }

        public void Submit(int index)
        {
            if (game.Guardian.Active) { game.Guardian.Submit(index); return; }
            if (!villageBossActive || game.Mode != TrainingMode.ForestChallenge || Quiz == null) return;
            var result = Quiz.Answer(index);
            if (result == AnswerResult.Wrong)
            {
                if (chestActive)
                {
                    Quiz = new QuizSession(Definition.questions[questionIndex++ % Definition.questions.Length], game.Hints);
                    game.UI.RefreshOverlay("봉인이 되돌아왔다. 다시 풀 수 있다.");
                }
                else game.DamagePlayer(game.Health, true);
                return;
            }
            if (result == AnswerResult.Advanced) { game.UI.RefreshOverlay("계산이 이어졌다. 다음 흐름을 완성하자."); return; }
            Quiz = null;
            if (chestActive) { CompleteVillage(); return; }
            float ratio = gimmickRemaining / Definition.gimmickTime;
            questionDelay = Definition.questionInterval;
            game.ShowForestMode(TrainingMode.Playing);
            game.ResolveForestRune();
            game.Notify(Definition.encounterKind == ForestEncounterKind.Collector ?
                (ratio > .5f ? "병이 깨지고 붙잡힌 정령 대부분이 날아갔다." : "일부 정령만 남아 약해진 레이저를 흩뿌린다.") :
                "커진 원기둥이 되돌아가 솔그늘 정령을 쓰러뜨렸다.", 4);
            if (game.enemy.Health <= 0) Win();
        }

        public void UseHint()
        {
            if (game.Guardian.Active) { game.Guardian.UseHint(); return; }
            if (game.Mode != TrainingMode.ForestChallenge || Quiz == null || !Quiz.UseHint()) return;
            game.SetForestHints(Quiz.HintsRemaining);
            game.UI.RefreshOverlay();
        }

        public void Win()
        {
            if (game.Guardian.Active) { game.Guardian.Win(); return; }
            if (!villageBossActive || game.enemy.Health > 0 || game.Health <= 0 || Progress.CurrentRelic) return;
            CompleteVillage();
        }

        void CompleteVillage()
        {
            villageBossActive = chestActive = false;
            Quiz = null;
            Progress.CurrentRelic = true;
            if (Progress.villageIndex == 2) { Progress.letterWritten = true; Progress.outbreakWitnessed = true; }
            game.RecordForestVictory();
        }

        public void ReturnToVillage()
        {
            if (!InVillage || game.Mode == TrainingMode.GameOver) return;
            game.PrepareForestVillage();
            game.ShowForestMode(TrainingMode.Playing);
            game.Notify(string.IsNullOrWhiteSpace(Definition.clearEvent) ? Definition.villageName + "로 돌아왔습니다." : Definition.clearEvent, 8);
        }

        public void Leave()
        {
            if (!InVillage || BossActive || game.Mode == TrainingMode.GameOver) return;
            if (Progress.villageIndex > 0) { MoveTo(Progress.villageIndex - 1); return; }
            Progress.inVillage = false;
            game.PrepareForestVillage();
            game.SaveForestProgress();
            game.ShowForestMode(TrainingMode.Playing);
            game.Notify("첨탑으로 돌아왔습니다. 오른쪽 문에서 숲을 다시 찾을 수 있습니다.", 5);
        }

        public void ResetCombat() { villageBossActive = chestActive = false; Quiz = null; questionDelay = gimmickRemaining = patternDelay = 0; questionIndex = gimmicksUsed = 0; }

        void MoveNext()
        {
            if (Progress.villageIndex == villages.Length - 1) { if (Progress.aggregateDefeated) game.Human.Enter(); else game.Guardian.Begin(); return; }
            MoveTo(Progress.villageIndex + 1);
        }

        void MoveTo(int index)
        {
            if (!IsVillageAvailable(index)) { game.Notify("마을 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); return; }
            Progress.villageIndex = index;
            game.PrepareForestVillage();
            game.SaveForestProgress();
            game.ShowForestMode(Progress.CurrentVisited ? TrainingMode.Playing : TrainingMode.ForestArrival);
        }
    }
}
