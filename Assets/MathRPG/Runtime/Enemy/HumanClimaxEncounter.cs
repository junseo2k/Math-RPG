using UnityEngine;

namespace MathRPG
{
    public sealed class HumanClimaxEncounter
    {
        readonly TrainingGame game;
        float delay;
        int cursor, questionIndex, solvedMask;
        public HumanClimaxDefinition Definition { get; }
        public bool Active { get; private set; }
        public bool PatternChanged { get; private set; }
        public int Rage { get; private set; }
        public int SolvedCount => ((solvedMask & 1) != 0 ? 1 : 0) + ((solvedMask & 2) != 0 ? 1 : 0) + ((solvedMask & 4) != 0 ? 1 : 0);
        public int AttackDamage => Definition.attackDamage + Rage * Definition.rageDamage;
        public QuizSession Quiz { get; private set; }
        public bool HasValidData => Definition != null && Definition.IsValid();
        HumanVillageProgress Progress => game.Progress.human;

        public HumanClimaxEncounter(TrainingGame game, HumanClimaxDefinition definition)
        { this.game = game; Definition = definition; }

        public void Enter()
        {
            if (Active || !game.Human.InVillage || Progress.villageIndex != 2 || !Progress.thirdBossDefeated ||
                game.Human.BossActive || game.Mode != TrainingMode.Playing) return;
            if (!HasValidData) { game.Notify("재난 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); return; }
            if (Progress.climaxDefeated)
            {
                if (Progress.climaxVictorySeen) game.RivalSecond.Begin();
                else game.ShowHumanMode(TrainingMode.HumanClimaxVictory);
                return;
            }
            if (!Progress.letterReceived)
            {
                Progress.letterReceived = true;
                game.SaveHumanProgress();
            }
            game.ShowHumanMode(Progress.disasterWitnessed ? TrainingMode.HumanDisaster : TrainingMode.HumanLetter);
        }

        public void InterruptLetter()
        {
            if (game.Mode != TrainingMode.HumanLetter) return;
            Progress.disasterWitnessed = true;
            game.SaveHumanProgress();
            game.ShowHumanMode(TrainingMode.HumanDisaster);
        }

        public void StartBoss()
        {
            if (game.Mode != TrainingMode.HumanDisaster || Active || !Progress.disasterWitnessed ||
                Progress.climaxDefeated || !HasValidData) return;
            Reset();
            game.PrepareHumanClimaxBoss();
            Active = true;
            delay = Definition.firstQuestionDelay;
            game.ShowHumanMode(TrainingMode.Playing);
            game.Notify("오답은 생명 피해와 공격 강화를 부른다. 정답으로 강화를 낮추자.", 6);
        }

        public void TickCombat(float dt)
        {
            if (!Active || game.Mode != TrainingMode.Playing) return;
            AfterDamage();
            if (!Active) return;
            delay -= dt;
            if (delay > 0) return;
            questionIndex = cursor++ % Definition.questions.Length;
            Quiz = new QuizSession(Definition.questions[questionIndex], game.Hints);
            game.ShowHumanMode(TrainingMode.HumanClimaxChallenge);
        }

        public void Submit(int index)
        {
            if (!Active || game.Mode != TrainingMode.HumanClimaxChallenge || Quiz == null) return;
            var result = Quiz.Answer(index);
            if (result == AnswerResult.Wrong)
            {
                Rage = Mathf.Min(Definition.maxRage, Rage + 1);
                game.DamagePlayer(Definition.wrongDamage, true);
                if (game.Mode != TrainingMode.GameOver) game.UI.RefreshOverlay("생명 -" + Definition.wrongDamage + " · 공격 강화 " + Rage + "/" + Definition.maxRage + " · 다시 선택할 수 있습니다.");
            }
            else if (result == AnswerResult.Advanced || result == AnswerResult.Completed)
            {
                Rage = Mathf.Max(0, Rage - 1);
                if (result == AnswerResult.Advanced) { game.UI.RefreshOverlay("정답! 공격 강화가 줄었다."); return; }
                solvedMask |= 1 << questionIndex;
                bool sealBroken = solvedMask == 7;
                if (sealBroken) solvedMask = 0;
                Quiz = null;
                delay = Definition.questionInterval;
                game.ShowHumanMode(TrainingMode.Playing);
                game.ResolveHumanClimaxRune(sealBroken);
                AfterDamage();
            }
        }

        public void UseHint()
        {
            if (!Active || game.Mode != TrainingMode.HumanClimaxChallenge || Quiz == null || !Quiz.UseHint()) return;
            game.SetHumanHints(Quiz.HintsRemaining);
            game.UI.RefreshOverlay();
        }

        public void AfterDamage()
        {
            if (!Active || game.Health <= 0) return;
            if (game.enemy.Health <= 0)
            {
                Progress.climaxDefeated = true;
                Progress.royalSealObtained = true;
                game.RecordHumanClimaxVictory();
                return;
            }
            if (!PatternChanged && game.enemy.Health <= Definition.bossHealth / 2)
            {
                PatternChanged = true;
                game.ShiftHumanClimaxPattern();
                game.Notify("검은 흐름이 뒤집혔다. 다음 공격부터 순서가 바뀐다.", 5);
            }
        }

        public void FinishVictory()
        {
            if (game.Mode != TrainingMode.HumanClimaxVictory || !Progress.climaxDefeated) return;
            Progress.climaxVictorySeen = true;
            game.SaveHumanProgress();
            game.RivalSecond.Begin();
        }

        public void RestoreEvent()
        {
            if (!game.Human.InVillage || Progress.villageIndex != 2 || !HasValidData) return;
            if (Progress.climaxDefeated && !Progress.climaxVictorySeen) game.ShowHumanMode(TrainingMode.HumanClimaxVictory);
            else if (Progress.letterReceived && !Progress.disasterWitnessed) game.ShowHumanMode(TrainingMode.HumanLetter);
        }

        public void Reset()
        { Active = false; PatternChanged = false; Rage = 0; Quiz = null; cursor = questionIndex = solvedMask = 0; delay = 0; }
    }
}
