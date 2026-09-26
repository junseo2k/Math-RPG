using UnityEngine;

namespace MathRPG
{
    public sealed class RivalSecondEncounter
    {
        readonly TrainingGame game;
        float questionDelay, attackDelay;
        int attackIndex;
        public RivalSecondDefinition Definition { get; }
        public bool Active { get; private set; }
        public int Health { get; private set; }
        public int Pressure { get; private set; }
        public float Remaining { get; private set; }
        public bool WindingUp { get; private set; }
        public bool LowSweep { get; private set; }
        public QuizSession Quiz { get; private set; }
        public bool HasValidData => Definition != null && Definition.IsValid();
        HumanVillageProgress Progress => game.Progress.human;
        public RivalSecondEncounter(TrainingGame game, RivalSecondDefinition definition) { this.game = game; Definition = definition; }

        public void Begin()
        {
            if (Active || !Progress.climaxDefeated || !Progress.climaxVictorySeen || !game.Human.InVillage || Progress.villageIndex != 2) return;
            if (!HasValidData) { game.Notify("라이벌2 데이터가 없습니다. Math RPG 메뉴에서 프로토타입을 다시 열어주세요.", 6); game.UI.RefreshOverlay(game.Notice); return; }
            if (Progress.chapter3Complete) { game.ShowRivalSecondEnding(); return; }
            if (Progress.rivalSecondDefeated) { game.ShowHumanMode(TrainingMode.RivalSecondDefeat); return; }
            Progress.sealTaken = true;
            game.SaveHumanProgress();
            game.ShowHumanMode(TrainingMode.RivalSecondIntroduction);
        }

        public void StartDuel()
        {
            if (game.Mode != TrainingMode.RivalSecondIntroduction || !HasValidData || Progress.rivalSecondDefeated) return;
            game.PrepareRivalSecond();
            Active = true;
            Health = Definition.health;
            questionDelay = Definition.questionInterval;
            attackDelay = Definition.attackInterval;
            game.ShowHumanMode(TrainingMode.Playing);
            game.Notify("검으로 맞서고, Q로 룬을 연결하자.", 5);
        }

        public void Tick(float dt)
        {
            if (!Active) return;
            if (game.Mode == TrainingMode.RivalSecondChallenge)
            {
                Remaining = Mathf.Max(0, Remaining - dt);
                if (Remaining <= 0) FinishQuestion(false);
                return;
            }
            if (game.Mode != TrainingMode.Playing) return;
            questionDelay -= dt;
            if (questionDelay <= 0) { OpenQuestion(); return; }
            attackDelay -= dt;
            if (!WindingUp) game.MoveRivalSecond(dt);
            if (attackDelay > 0) return;
            if (WindingUp)
            {
                WindingUp = false;
                attackDelay = Definition.attackInterval;
                game.StrikeRivalSecond(LowSweep);
            }
            else
            {
                LowSweep = attackIndex++ % 2 == 0;
                WindingUp = true;
                attackDelay = Definition.attackWindup;
            }
        }

        public void OpenQuestion()
        {
            if (!Active || game.Mode != TrainingMode.Playing) return;
            Quiz = new QuizSession(Definition.questions[Pressure], game.Hints);
            Remaining = Mathf.Max(Definition.minimumTimeLimit, Definition.firstTimeLimit - Pressure * Definition.timeReduction);
            game.ShowHumanMode(TrainingMode.RivalSecondChallenge);
        }

        public void Submit(int index)
        {
            if (!Active || game.Mode != TrainingMode.RivalSecondChallenge || Quiz == null) return;
            var result = Quiz.Answer(index);
            if (result == AnswerResult.Completed) FinishQuestion(true);
            else if (result == AnswerResult.Advanced) game.UI.RefreshOverlay("답이 이어졌다. 남은 흐름을 연결하자.");
            else if (result == AnswerResult.Wrong) game.UI.RefreshOverlay("다시 선택할 수 있어. 식의 조건부터 보자.");
        }

        void FinishQuestion(bool correct)
        {
            Quiz = null;
            if (correct) game.ResolveRivalSecondRune();
            Pressure++;
            if (Pressure >= Definition.questions.Length)
            {
                game.StopRivalSecondCast();
                game.ShowHumanMode(TrainingMode.RivalSecondBreakdown);
                return;
            }
            questionDelay = Definition.questionInterval;
            game.ShowHumanMode(TrainingMode.Playing);
            game.Notify(correct ? "정답이다. 그런데 다음 식을 잡는 손이 점점 무거워진다." : "시간을 놓쳤다. 숨이 가빠지고 손끝이 굳는다.", 5);
        }

        public int ReceiveHit(int damage)
        {
            int dealt = Mathf.Min(Mathf.Max(0, damage), Mathf.Max(0, Health - 1));
            Health -= dealt;
            if (Health == 1)
            {
                questionDelay = 0;
                game.Notify("친구가 검을 맞대고 룬 대결을 강요한다.", 3);
            }
            return dealt;
        }

        public void UseHint()
        {
            if (!Active || game.Mode != TrainingMode.RivalSecondChallenge || Quiz == null || !Quiz.UseHint()) return;
            game.SetHumanHints(Quiz.HintsRemaining);
            game.UI.RefreshOverlay();
        }

        public void FailRune()
        {
            if (game.Mode != TrainingMode.RivalSecondBreakdown) return;
            Active = false;
            Progress.rivalSecondDefeated = true;
            game.SaveHumanProgress();
            game.ShowHumanMode(TrainingMode.RivalSecondDefeat);
        }

        public void Depart()
        {
            if (game.Mode != TrainingMode.RivalSecondDefeat) return;
            Progress.chapter3Complete = true;
            Progress.inVillage = false;
            game.SaveHumanProgress();
            game.ShowRivalSecondEnding();
        }

        public void RestoreEvent()
        {
            if (!HasValidData) return;
            if (Progress.chapter3Complete) game.ShowRivalSecondEnding();
            else if (Progress.rivalSecondDefeated) game.ShowHumanMode(TrainingMode.RivalSecondDefeat);
            else if (Progress.sealTaken || (Progress.climaxVictorySeen && Progress.inVillage && Progress.villageIndex == 2)) Begin();
        }

        public void Reset()
        { Active = false; Quiz = null; Health = 0; Pressure = 0; Remaining = 0; WindingUp = false; attackIndex = 0; questionDelay = attackDelay = 0; }
    }
}
