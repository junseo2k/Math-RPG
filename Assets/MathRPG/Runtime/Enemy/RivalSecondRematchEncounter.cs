using UnityEngine;

namespace MathRPG
{
    public sealed class RivalSecondRematchEncounter
    {
        readonly TrainingGame game;
        float attackTimer, questionTimer;
        int attackIndex, questionIndex;
        bool phasePending;
        public RivalSecondRematchDefinition Definition { get; }
        public bool HasValidData => Definition != null && Definition.IsValid();
        public bool Active { get; private set; }
        public int Health { get; private set; }
        public int Rage { get; private set; }
        public int Phase { get; private set; } = 1;
        public bool WindingUp { get; private set; }
        public bool LowSweep { get; private set; }
        public bool HazardActive { get; private set; }
        public bool LethalHazard { get; private set; }
        public float HazardX { get; private set; }
        public float HazardRadius { get; private set; }
        public float HazardRemaining { get; private set; }
        public QuizSession Quiz { get; private set; }
        public int QuestionNumber => questionIndex % 3 + 1;
        public string LastEvent { get; private set; }
        public string DeathCause { get; private set; }
        public RivalSecondRematchEncounter(TrainingGame game, RivalSecondRematchDefinition data) { this.game = game; Definition = data; }

        public void Begin()
        {
            if (game.Mode != TrainingMode.ChapterFourStory || !game.Progress.chapterFour.Awakened) return;
            if (!HasValidData) { game.UI.RefreshOverlay("리매치 데이터가 없습니다. Math RPG → Open Training Prototype을 다시 열어주세요."); return; }
            if (!game.SaveSecondRematch(RematchProgress.Introduction)) return;
            Restore();
        }

        public void Learn()
        {
            if (game.Mode != TrainingMode.SecondRematchIntroduction) return;
            if (!game.SaveSecondRematch(RematchProgress.Lesson)) return;
            game.ShowSecondRematchMode(TrainingMode.SecondRematchLesson);
        }

        public void StartDuel()
        {
            if (!HasValidData || (game.Mode != TrainingMode.SecondRematchLesson &&
                !(Active && (game.Mode == TrainingMode.GameOver || game.Mode == TrainingMode.Paused)))) return;
            if (!game.SaveSecondRematch(RematchProgress.Duel)) return;
            game.PrepareSecondRematch();
            Active = true;
            Health = Definition.health;
            attackTimer = Definition.attackInterval;
            questionTimer = Definition.questionInterval;
            game.ShowSecondRematchMode(TrainingMode.Playing);
            game.Notify("Q: 룬 대결 · 문제에는 제한시간이 없다. 장판 예고가 뜨면 표시 밖으로 이동하자.", 7);
        }

        public void Restore()
        {
            var progress = game.Progress.chapterFour.secondRematch;
            if (progress == RematchProgress.None || !HasValidData) return;
            game.PrepareSecondRematch();
            if (progress == RematchProgress.Departed) game.HideSecondRematchOpponent();
            game.ShowSecondRematchMode(progress == RematchProgress.Introduction ? TrainingMode.SecondRematchIntroduction :
                progress == RematchProgress.Victory ? TrainingMode.SecondRematchVictory :
                progress == RematchProgress.Departed ? TrainingMode.SecondRematchDeparture : TrainingMode.SecondRematchLesson);
        }

        public void Tick(float dt)
        {
            if (!Active || game.Mode != TrainingMode.Playing) return;
            if (HazardActive)
            {
                HazardRemaining = Mathf.Max(0, HazardRemaining - dt);
                if (HazardRemaining > 0) return;
                HazardActive = false;
                game.ResolveSecondRematchHazard();
                if (game.Mode != TrainingMode.Playing) return;
                attackTimer = Definition.attackInterval;
                return;
            }
            if (phasePending) { EnterSecondPhase(); return; }
            questionTimer -= dt;
            if (!WindingUp && questionTimer <= 0) { OpenQuestion(); return; }
            attackTimer -= dt;
            if (!WindingUp) game.MoveSecondRematch(dt);
            if (attackTimer > 0) return;
            if (WindingUp)
            {
                WindingUp = false;
                attackTimer = Definition.attackInterval * SpeedFactor;
                game.StrikeSecondRematch(LowSweep);
            }
            else
            {
                LowSweep = Phase == 1 ? attackIndex++ % 2 == 0 : attackIndex++ % 3 != 1;
                WindingUp = true;
                attackTimer = Mathf.Max(.35f, Definition.attackWindup * SpeedFactor);
            }
        }

        float SpeedFactor => Mathf.Max(.4f, (Phase == 2 ? Definition.phaseTwoIntervalMultiplier : 1) - Rage * Definition.rageSpeedStep);

        public void OpenQuestion()
        {
            if (!Active || game.Mode != TrainingMode.Playing || HazardActive || phasePending || WindingUp) return;
            if (Quiz == null) Quiz = new QuizSession(Definition.questions[questionIndex % 3], game.Hints);
            game.ShowSecondRematchMode(TrainingMode.SecondRematchChallenge);
        }

        public void Submit(int index)
        {
            if (!Active || game.Mode != TrainingMode.SecondRematchChallenge || Quiz == null) return;
            var result = Quiz.Answer(index);
            if (result == AnswerResult.Advanced) { game.UI.RefreshOverlay("좋아. 이어지는 식도 차근차근 보자."); return; }
            if (result != AnswerResult.Wrong && result != AnswerResult.Completed) return;
            bool correct = result == AnswerResult.Completed;
            Rage = Mathf.Clamp(Rage + (correct ? -1 : 1), 0, Definition.maximumRage);
            LastEvent = correct ? "정답 · 패턴 완화 " + Rage : "오답 · 생명 -" + Definition.wrongDamage + " · 패턴 강화 " + Rage;
            if (!correct)
            {
                DeathCause = "룬 대결의 오답 피해";
                game.DamagePlayer(Definition.wrongDamage, true);
                if (game.Mode == TrainingMode.GameOver) return;
            }
            else
            {
                Quiz = null;
                questionIndex++;
                game.ResolveSecondRematchRune();
                if (!Active) return;
            }
            questionTimer = Definition.questionInterval;
            attackTimer = Definition.attackInterval;
            WindingUp = false;
            game.ShowSecondRematchMode(TrainingMode.Playing);
            float failure = Phase == 2 ? Definition.phaseTwoFailureChance : Definition.runeFailureChance;
            if (Random.value < failure)
            {
                LastEvent += "\n라이벌2의 식은 맞았지만, 룬이 발동하지 않았다.";
                game.ShowSecondRematchMisfire();
            }
            else
            {
                LethalHazard = !correct;
                HazardRadius = correct ? Definition.narrowHazardRadius : Definition.wideHazardRadius;
                HazardX = Mathf.Clamp(game.player.transform.position.x, -Definition.arenaHalfWidth + HazardRadius, Definition.arenaHalfWidth - HazardRadius);
                HazardRemaining = Definition.hazardWarning;
                HazardActive = true;
                LastEvent += correct ? "\n좁은 장판 — 표시 밖으로 이동!" : "\n넓은 즉사 장판 — 표시 밖으로 이동!";
            }
            game.Notify(LastEvent, 5);
        }

        public int ReceiveHit(int amount)
        {
            if (!Active || amount <= 0) return 0;
            int threshold = Mathf.Max(1, Mathf.CeilToInt(Definition.health * Definition.phaseThreshold));
            int floor = Phase == 1 ? threshold : 0;
            int dealt = Mathf.Min(amount, Mathf.Max(0, Health - floor));
            Health -= dealt;
            if (Phase == 1 && Health <= threshold) phasePending = true;
            if (Health == 0) Win();
            return dealt;
        }

        void EnterSecondPhase()
        {
            phasePending = false;
            Phase = 2;
            WindingUp = false;
            attackIndex = 0;
            attackTimer = Definition.attackInterval;
            game.ShowSecondRematchMode(TrainingMode.SecondRematchPhase);
        }

        public void ContinuePhase()
        {
            if (Active && game.Mode == TrainingMode.SecondRematchPhase) game.ShowSecondRematchMode(TrainingMode.Playing);
        }

        public void UseHint()
        {
            if (game.Mode != TrainingMode.SecondRematchChallenge || Quiz == null || !Quiz.UseHint()) return;
            game.SetSecondRematchHints(Quiz.HintsRemaining);
            game.UI.RefreshOverlay();
        }

        void Win()
        {
            Active = HazardActive = WindingUp = false;
            Quiz = null;
            game.ShowSecondRematchMode(TrainingMode.SecondRematchVictory);
            game.SaveSecondRematch(RematchProgress.Victory);
        }

        public void Depart()
        {
            if (game.Mode != TrainingMode.SecondRematchVictory) return;
            if (!game.SaveSecondRematch(RematchProgress.Departed)) return;
            game.HideSecondRematchOpponent();
            game.ShowSecondRematchMode(TrainingMode.SecondRematchDeparture);
        }

        public void RememberDeparture()
        {
            if (game.Mode == TrainingMode.SecondRematchDeparture && game.SaveSecondRematch(RematchProgress.Departed))
                game.UI.RefreshOverlay("라이벌이 길을 열어준 순간을 기억했습니다.");
        }

        public void SetDeathCause(string cause) => DeathCause = cause;
        public void Reset()
        {
            Active = HazardActive = LethalHazard = WindingUp = phasePending = false;
            Health = Rage = attackIndex = questionIndex = 0;
            Phase = 1;
            Quiz = null;
            attackTimer = questionTimer = HazardRemaining = 0;
            LastEvent = DeathCause = "";
        }
    }
}
