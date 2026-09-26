using UnityEngine;

namespace MathRPG
{
    public sealed class RivalEncounter
    {
        readonly TrainingGame game;
        public RivalDuelDefinition Definition { get; }
        public bool InEncounter { get; private set; }
        public bool SecondPattern { get; private set; }
        public RivalChallenge Challenge { get; private set; }
        public int LastPlayerPower { get; private set; }
        public int LastRivalPower { get; private set; }
        float challengeDelay;
        int questionIndex;

        public RivalEncounter(TrainingGame game, RivalDuelDefinition definition)
        { this.game = game; Definition = definition; }

        public void Begin(bool retry = false)
        {
            if (InEncounter || !game.Course.Finished || game.Course.Index != 3) return;
            if (game.Progress.rivalDefeated)
            { game.Notify("이미 결투를 마쳤습니다. 정령의 숲 이야기는 다음 제작 구간입니다.", 4); return; }
            if (Definition == null || !Definition.IsValid())
            { game.Notify("라이벌 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); return; }
            InEncounter = true;
            SecondPattern = false;
            questionIndex = 0;
            Challenge = null;
            game.PrepareRivalEncounter();
            game.ShowRivalMode(TrainingMode.RivalIntroduction);
            if (retry) StartCombat();
        }

        public void StartCombat()
        {
            if (!InEncounter || game.Progress.rivalDefeated || game.Mode != TrainingMode.RivalIntroduction) return;
            game.enemy.ResetEnemy(Definition.health, Definition.openingWindup);
            ApplyPattern(false);
            challengeDelay = Definition.firstChallengeDelay;
            game.ShowRivalMode(TrainingMode.Playing);
            game.Notify("라이벌이 검을 들었다. 빛이 모이면 같은 흐름을 함께 읽는다.", 4);
        }

        public void TickCombat(float dt)
        {
            if (!InEncounter || game.enemy.Phase == EnemyPhase.Defeated) return;
            CheckPattern();
            challengeDelay -= dt;
            if (challengeDelay > 0) return;
            Challenge = new RivalChallenge(Definition.questions[questionIndex++ % Definition.questions.Length], game.Hints,
                Definition.answerTime, Definition.rivalSolveTime);
            game.ShowRivalMode(TrainingMode.RivalChallenge);
        }

        public void TickChallenge(float dt)
        {
            if (Challenge == null || game.Mode != TrainingMode.RivalChallenge) return;
            Challenge.Tick(dt);
            if (!Challenge.Finished) return;
            LastPlayerPower = Challenge.PlayerReady ? Definition.exchangePower : 0;
            LastRivalPower = Challenge.RivalReady ? Mathf.Max(1, Mathf.RoundToInt(Definition.exchangePower * Definition.rivalPowerMultiplier)) : 0;
            game.ResolveRivalExchange(LastPlayerPower, LastRivalPower);
            if (game.Mode == TrainingMode.GameOver) return;
            if (game.enemy.Health <= 0) { Win(); return; }
            CheckPattern();
            game.ShowRivalMode(TrainingMode.RivalResult);
        }

        public void Submit(int index)
        {
            if (game.Mode != TrainingMode.RivalChallenge || Challenge == null) return;
            var result = Challenge.Answer(index);
            if (result == AnswerResult.Ignored) return;
            game.UI.RefreshOverlay(result == AnswerResult.Wrong ? "빛이 흩어졌다. 남은 길을 다시 이어보자." : null);
        }

        public void UseHint()
        {
            if (game.Mode != TrainingMode.RivalChallenge || Challenge == null || Challenge.Finished || !Challenge.Quiz.UseHint()) return;
            game.SetRivalHints(Challenge.Quiz.HintsRemaining);
            game.UI.RefreshOverlay();
        }

        public void ContinueCombat()
        {
            if (game.Mode != TrainingMode.RivalResult || !InEncounter) return;
            Challenge = null;
            challengeDelay = Definition.challengeInterval;
            game.ShowRivalMode(TrainingMode.Playing);
        }

        void CheckPattern()
        {
            if (SecondPattern || game.enemy.Health > Definition.health * .5f) return;
            SecondPattern = true;
            ApplyPattern(true);
            game.Notify(Definition.patternChangeLine, 4);
        }

        void ApplyPattern(bool second)
        {
            game.enemy.ConfigurePattern(second ? Definition.secondLowAttacks : Definition.openingLowAttacks,
                second ? Definition.secondWindup : Definition.openingWindup,
                second ? Definition.secondRecovery : Definition.openingRecovery);
        }

        public void Win()
        {
            if (!InEncounter || game.Progress.rivalDefeated || game.Health <= 0 || game.enemy.Health > 0) return;
            Challenge = null;
            game.RecordRivalVictory();
        }

        public void Reset()
        { InEncounter = false; SecondPattern = false; Challenge = null; questionIndex = 0; challengeDelay = 0; }
    }
}
