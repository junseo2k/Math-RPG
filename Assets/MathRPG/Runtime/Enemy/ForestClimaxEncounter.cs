using UnityEngine;

namespace MathRPG
{
    public enum AggregateStage { Opening, Capture, SplitSpirits, Clones, Core, Defeated }

    public sealed class ForestClimaxEncounter
    {
        readonly TrainingGame game;
        float patternDelay, poisonDelay, challengeRemaining, spiritLaserDelay, rageRemaining;
        int pattern, quizEvent, rageStartHealth;

        public ForestClimaxDefinition Definition { get; }
        public bool Active { get; private set; }
        public AggregateStage Stage { get; private set; }
        public bool WeaknessCaptured { get; private set; }
        public int SplitSpirits { get; private set; }
        public int CloneCount { get; private set; } = 1;
        public int PoisonStacks { get; private set; }
        public float ChallengeRemaining => challengeRemaining;
        public float RageRemaining => rageRemaining;
        public bool GuardianHelpAvailable => Active && !game.Progress.forest.guardianHelpUsed &&
            (Stage == AggregateStage.SplitSpirits || Stage == AggregateStage.Core);
        public QuizSession Quiz { get; private set; }
        public bool HasValidData => Definition != null && Definition.IsValid();
        public bool BlessingActive => false;

        public ForestClimaxEncounter(TrainingGame game, ForestClimaxDefinition definition)
        { this.game = game; Definition = definition; }

        public void Begin()
        {
            if (game.Mode != TrainingMode.Playing || !game.Forest.InVillage || game.Forest.BossActive ||
                game.Progress.forest.villageIndex != 2 || game.Progress.forest.RelicCount != 3 || !game.Progress.forest.outbreakWitnessed) return;
            if (!HasValidData) { game.Notify("수호 정령 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); return; }
            if (game.Progress.forest.aggregateDefeated) { game.ShowForestMode(TrainingMode.ForestChapterClear); return; }
            game.ShowForestMode(game.Progress.forest.guardianAwakened ? TrainingMode.GuardianIntroduction : TrainingMode.GuardianAwakening);
        }

        public void Awaken()
        {
            if (game.Mode != TrainingMode.GuardianAwakening || game.Progress.forest.RelicCount != 3 || !HasValidData) return;
            game.Progress.forest.guardianAwakened = true;
            game.PrepareForestVillage();
            game.SaveForestProgress();
            game.ShowForestMode(TrainingMode.GuardianIntroduction);
        }

        public void StartCombat()
        {
            if (game.Mode != TrainingMode.GuardianIntroduction || !game.Progress.forest.guardianAwakened ||
                game.Progress.forest.aggregateDefeated || !HasValidData) return;
            Reset();
            Active = true;
            Stage = AggregateStage.Opening;
            patternDelay = 2;
            game.PrepareForestClimax();
            game.ShowForestMode(TrainingMode.Playing);
            game.Notify("장갑이 약점을 찾지 못했다. 집합체의 움직임을 직접 읽어야 한다.", 5);
        }

        public void TickCombat(float dt)
        {
            if (!Active || game.Mode != TrainingMode.Playing) return;
            CheckHealthEvents();
            if (!Active || game.Mode != TrainingMode.Playing) return;
            if (Stage == AggregateStage.Core)
            {
                rageRemaining -= dt;
                if (game.enemy.Health <= rageStartHealth - Definition.rageDamageTarget || game.enemy.Health <= 0) { Win(); return; }
                if (rageRemaining <= 0) { game.DamagePlayer(game.Health, true); return; }
            }
            poisonDelay -= dt;
            if (PoisonStacks > 0 && poisonDelay <= 0)
            {
                poisonDelay = 2;
                game.DamagePlayer(Definition.poisonDamage * PoisonStacks, false);
            }
            if (SplitSpirits > 0)
            {
                spiritLaserDelay -= dt;
                if (spiritLaserDelay <= 0)
                {
                    spiritLaserDelay = 2.2f;
                    if (Mathf.Abs(GameInput.Horizontal) < .1f) game.DamagePlayer(Definition.attackDamage, false);
                    else if (Mathf.Abs(game.player.transform.position.x - game.enemy.transform.position.x) < 2.5f)
                        game.enemy.ReceiveDamage(Mathf.Max(1, Mathf.RoundToInt(Definition.health * .02f)), -1, false);
                }
            }
            patternDelay -= dt;
            if (patternDelay <= 0) { RunPattern(); patternDelay = CloneCount == 3 ? 1.7f : CloneCount == 2 ? 2.2f : 2.8f; }
        }

        public void TickChallenge(float dt)
        {
            if (!Active || game.Mode != TrainingMode.ForestChallenge || Quiz == null) return;
            challengeRemaining -= dt;
            if (challengeRemaining > 0) return;
            FinishQuiz(false);
        }

        void CheckHealthEvents()
        {
            float health = (float)game.enemy.Health / Mathf.Max(1, Definition.health);
            if (Stage == AggregateStage.Opening && health <= .9f) BeginQuiz(1, AggregateStage.Capture, Definition.captureTime);
            else if (Stage == AggregateStage.Capture && health <= .75f)
            {
                Stage = AggregateStage.SplitSpirits;
                SplitSpirits = 4;
                spiritLaserDelay = 1.2f;
                game.RefreshClimaxVisuals();
                game.Notify("네 마리 정령이 갈라져 나왔다. 레이저를 집합체 쪽으로 유도하거나 R로 수호 정령을 부르자.", 6);
            }
            else if (Stage == AggregateStage.SplitSpirits && health <= .5f) BeginQuiz(2, AggregateStage.Clones, Definition.captureTime);
            else if (Stage == AggregateStage.Clones && health <= .25f)
            {
                Stage = AggregateStage.Core;
                SplitSpirits = 0;
                CloneCount = 1;
                rageStartHealth = game.enemy.Health;
                rageRemaining = Definition.rageTime;
                game.RefreshClimaxVisuals();
                game.Notify("집합체의 중심이 모든 정령을 먹어치웠다. 제한시간 안에 무너뜨리거나 R로 마무리하자!", 6);
            }
        }

        void BeginQuiz(int eventId, AggregateStage nextStage, float duration)
        {
            quizEvent = eventId;
            Stage = nextStage;
            Quiz = new QuizSession(Definition.questions[eventId - 1], game.Hints);
            challengeRemaining = duration;
            game.ShowForestMode(TrainingMode.ForestChallenge);
        }

        void RunPattern()
        {
            pattern = (pattern + 1) % 5;
            switch (pattern)
            {
                case 0:
                    if (Mathf.Abs(GameInput.Horizontal) < .1f) game.DamagePlayer(Definition.attackDamage, false);
                    game.Notify("자폭 정령이 4초 동안 추적한다. 계속 움직여 따돌리자.", 2.2f);
                    break;
                case 1:
                    if (!game.player.Crouching) game.DamagePlayer(Definition.attackDamage, false);
                    game.Notify("전방 대형 레이저 — 숙여서 피하자.", 2);
                    break;
                case 2:
                    PoisonStacks = Mathf.Min(2, PoisonStacks + 1);
                    poisonDelay = 2;
                    game.Notify("독 " + PoisonStacks + "/2중첩", 2);
                    break;
                case 3:
                    if (Mathf.Abs(game.player.transform.position.x - game.enemy.transform.position.x) < 3.5f)
                        game.DamagePlayer(Definition.attackDamage, false);
                    game.Notify("박치기 3연속 — 거리를 벌리자.", 2);
                    break;
                default:
                    if (Random.value < .3f) game.Notify("룬 장갑 화염구가 불발했다. 집합체가 스스로 기절했다!", 2.5f);
                    else game.DamagePlayer(Definition.attackDamage * 2, false);
                    break;
            }
        }

        public void Submit(int index)
        {
            if (!Active || game.Mode != TrainingMode.ForestChallenge || Quiz == null) return;
            var result = Quiz.Answer(index);
            if (result == AnswerResult.Wrong) { FinishQuiz(false); return; }
            if (result == AnswerResult.Advanced) { game.UI.RefreshOverlay("계산이 이어졌다. 남은 흐름을 완성하자."); return; }
            FinishQuiz(true);
        }

        void FinishQuiz(bool correct)
        {
            Quiz = null;
            game.ShowForestMode(TrainingMode.Playing);
            if (quizEvent == 1)
            {
                WeaknessCaptured = correct;
                game.Notify(correct ? "포착 대결 승리. 약점이 열려 이후 공격이 강해진다." : "포착 기회를 놓쳤다. 전투는 계속된다.", 4);
            }
            else
            {
                CloneCount = correct ? 2 : 3;
                if (correct) game.enemy.ReceiveDamage(Mathf.Max(1, game.enemy.Health / 10), -1, true);
                game.RefreshClimaxVisuals();
                game.Notify(correct ? "두 분신으로 갈라졌다. 한쪽이 작아지며 체력을 잃었다." : "세 분신으로 갈라져 공격 밀도가 높아졌다.", 4);
            }
            quizEvent = 0;
        }

        public void UseHint()
        {
            if (!Active || game.Mode != TrainingMode.ForestChallenge || Quiz == null || !Quiz.UseHint()) return;
            game.SetForestHints(Quiz.HintsRemaining);
            game.UI.RefreshOverlay();
        }

        public void UseGuardianHelp()
        {
            if (!GuardianHelpAvailable) return;
            game.Progress.forest.guardianHelpUsed = true;
            if (Stage == AggregateStage.Core) { game.enemy.ReceiveDamage(game.enemy.Health, -1, true); Win(); return; }
            SplitSpirits = 0;
            game.enemy.ReceiveDamage(Mathf.Max(Definition.guardianDamage, Mathf.RoundToInt(Definition.health * Definition.guardianDamageFraction)), -1, true);
            game.RefreshClimaxVisuals();
            game.SaveForestProgress();
            game.Notify("수호 정령의 빛이 분열 정령을 지우고 집합체를 꿰뚫었다.", 5);
        }

        public int ModifyIncomingDamage(int damage)
        {
            int boosted = WeaknessCaptured ? Mathf.RoundToInt(damage * 1.3f) : damage;
            float floorRatio = Stage == AggregateStage.Opening ? .9f : Stage == AggregateStage.Capture ? .75f :
                Stage == AggregateStage.SplitSpirits ? .5f : Stage == AggregateStage.Clones ? .25f : 0;
            int floor = Mathf.CeilToInt(Definition.health * floorRatio);
            return floor > 0 ? Mathf.Clamp(boosted, 0, Mathf.Max(0, game.enemy.Health - floor)) : boosted;
        }

        public void Win()
        {
            if (!Active || game.enemy.Health > 0 || game.Health <= 0 || game.Progress.forest.aggregateDefeated) return;
            game.Progress.forest.aggregateDefeated = true;
            Stage = AggregateStage.Defeated;
            Active = false;
            Quiz = null;
            game.RecordClimaxVictory();
        }

        public void Reset()
        {
            Active = WeaknessCaptured = false;
            Stage = AggregateStage.Opening;
            SplitSpirits = PoisonStacks = pattern = quizEvent = rageStartHealth = 0;
            CloneCount = 1;
            patternDelay = poisonDelay = challengeRemaining = spiritLaserDelay = rageRemaining = 0;
            Quiz = null;
        }
    }
}
