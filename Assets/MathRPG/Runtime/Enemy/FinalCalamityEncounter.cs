using UnityEngine;

namespace MathRPG
{
    public sealed class FinalCalamityEncounter
    {
        readonly TrainingGame game;
        float attackTimer, questionTimer, pressureTimer, spiritTimer, hazardTimer, recoveryTimer, questionElapsed;
        int attackIndex, problemIndex, stepIndex, insightMask;
        bool phasePending;
        public FinalCalamityDefinition Definition { get; }
        public bool HasValidData => Definition != null && Definition.IsValid();
        public bool Active { get; private set; }
        public bool Won { get; private set; }
        public int Health { get; private set; }
        public int Phase { get; private set; } = 1;
        public int Rage { get; private set; }
        public bool WindingUp { get; private set; }
        public bool LowSweep { get; private set; }
        public bool HazardActive { get; private set; }
        public float HazardX { get; private set; }
        public float HazardRadius { get; private set; }
        public float HazardRemaining { get; private set; }
        public float SpiritWarningRemaining { get; private set; }
        public float SpiritRemaining { get; private set; }
        public bool Transformed => SpiritRemaining > 0;
        public float Remaining { get; private set; }
        public bool TimeCrushed { get; private set; }
        public bool UsingShortcut { get; private set; }
        public int ProblemNumber => problemIndex % 3 + 1;
        public int DirectStepNumber => stepIndex + 1;
        public CalamityProblem Problem => Definition.problems[problemIndex % 3];
        public CalamityAnswer Question => UsingShortcut ? Problem.shortcut : Problem.directSteps[stepIndex];
        public bool InsightFound => (insightMask & (1 << (problemIndex % 3))) != 0;
        public string DeathCause { get; private set; }
        public string LastEvent { get; private set; }
        public bool CanRetry => Active && (game.Mode == TrainingMode.GameOver || game.Mode == TrainingMode.Paused);
        public FinalCalamityEncounter(TrainingGame game, FinalCalamityDefinition definition) { this.game = game; Definition = definition; }

        public void Begin()
        {
            if (game.Mode != TrainingMode.SecondRematchDeparture || game.Progress.chapterFour.secondRematch != RematchProgress.Departed) return;
            if (!HasValidData) { game.UI.RefreshOverlay("최종보스 데이터가 없습니다. Math RPG → Open Training Prototype을 다시 열어주세요."); return; }
            if (!game.SaveCalamity(CalamityProgress.Introduction)) return;
            Restore();
        }

        public void Farewell()
        {
            if (game.Mode != TrainingMode.CalamityIntroduction || !game.SaveCalamity(CalamityProgress.Farewell)) return;
            game.ShowCalamityMode(TrainingMode.CalamityFarewell);
        }

        public void Isolate()
        {
            if (game.Mode != TrainingMode.CalamityFarewell || !game.SaveCalamity(CalamityProgress.Isolated)) return;
            game.SetCalamitySpiritVisible(false);
            game.ShowCalamityMode(TrainingMode.CalamityIsolation);
        }

        public void StartBattle()
        {
            if (game.Mode != TrainingMode.CalamityIsolation || !HasValidData) return;
            game.PrepareCalamity(false);
            Active = true;
            Health = Definition.health;
            attackTimer = Definition.attackInterval;
            questionTimer = Definition.questionInterval;
            pressureTimer = Definition.pressureInterval;
            spiritTimer = Definition.spiritInterval;
            hazardTimer = Definition.finalHazardInterval;
            game.ShowCalamityMode(TrainingMode.Playing);
            game.Notify("정령의 도움은 닿지 않는다. Q로 식을 읽고, R로 다른 풀이를 떠올리자.", 6);
        }

        public void Restore()
        {
            var progress = game.Progress.chapterFour.calamity;
            if (progress == CalamityProgress.None || !HasValidData) return;
            game.PrepareCalamity(progress != CalamityProgress.Defeated);
            if (progress == CalamityProgress.Defeated)
            {
                Won = true;
                game.HideCalamityBoss();
                game.ShowCalamityMode(TrainingMode.CalamityVictory);
            }
            else game.ShowCalamityMode(progress == CalamityProgress.Introduction ? TrainingMode.CalamityIntroduction : TrainingMode.CalamityFarewell);
        }

        public void Retry()
        {
            if (!CanRetry || !game.SaveCalamity(CalamityProgress.Farewell)) return;
            Restore();
        }

        public void TickQuestion(float dt)
        {
            if (!Active || game.Mode != TrainingMode.CalamityChallenge) return;
            questionElapsed += dt;
            Remaining = Mathf.Max(0, Remaining - dt);
            if (!TimeCrushed && questionElapsed >= Definition.timeCrushAfter)
            {
                TimeCrushed = true;
                Remaining = Mathf.Min(Remaining, Phase == 1 ? Definition.firstCrushedTime : Definition.secondCrushedTime);
            }
            if (Remaining <= 0) FinishQuestion(false, true);
        }

        public void Tick(float dt)
        {
            if (!Active || game.Mode != TrainingMode.Playing) return;
            pressureTimer -= dt;
            if (pressureTimer <= 0)
            {
                pressureTimer = Definition.pressureInterval;
                Hurt(Definition.pressureDamage, "재앙의 지속적인 마력 침식", true);
                if (game.Mode == TrainingMode.GameOver) return;
            }
            if (Transformed) SpiritRemaining = Mathf.Max(0, SpiritRemaining - dt);
            if (HazardActive)
            {
                HazardRemaining = Mathf.Max(0, HazardRemaining - dt);
                if (HazardRemaining > 0) return;
                HazardActive = false;
                game.ResolveCalamityHazard();
                recoveryTimer = Definition.hazardRecovery;
                attackTimer = Definition.attackInterval;
                return;
            }
            if (phasePending) { NextPhase(); return; }
            if (recoveryTimer > 0) { recoveryTimer -= dt; return; }
            if (SpiritWarningRemaining > 0)
            {
                SpiritWarningRemaining = Mathf.Max(0, SpiritWarningRemaining - dt);
                if (SpiritWarningRemaining <= 0)
                {
                    SpiritRemaining = Definition.spiritDuration;
                    spiritTimer = Definition.spiritInterval;
                    game.Notify("몸이 정령으로 변했다. 이동·점프·숙이기로 버텨내자.", 4);
                }
                return;
            }
            if (Phase >= 2 && !Transformed)
            {
                spiritTimer -= dt;
                if (spiritTimer <= 0 && !WindingUp)
                {
                    SpiritWarningRemaining = Definition.spiritWarning;
                    game.Notify("정령화가 다가온다. 공격을 거두고 회피를 준비하자.", 3);
                    return;
                }
            }
            if (Phase == 3)
            {
                hazardTimer -= dt;
                if (hazardTimer <= 0 && !WindingUp) { ArmHazard(false); hazardTimer = Definition.finalHazardInterval; return; }
            }
            else if (!Transformed)
            {
                questionTimer -= dt;
                if (questionTimer <= 0 && !WindingUp) { OpenQuestion(); return; }
            }
            attackTimer -= dt;
            if (!WindingUp) game.MoveCalamity(dt);
            if (attackTimer > 0) return;
            if (WindingUp)
            {
                WindingUp = false;
                attackTimer = Definition.attackInterval * SpeedFactor;
                game.StrikeCalamity(LowSweep);
            }
            else
            {
                LowSweep = Phase == 1 ? attackIndex++ % 2 == 0 : Phase == 2 ? attackIndex++ % 3 != 1 : attackIndex++ % 3 == 1;
                WindingUp = true;
                attackTimer = Mathf.Max(.45f, Definition.attackWindup * SpeedFactor);
            }
        }

        float SpeedFactor => Mathf.Max(.5f, (Phase == 3 ? Definition.finalAttackMultiplier : 1) - Rage * Definition.rageSpeedStep);

        public void OpenQuestion()
        {
            if (!Active || game.Mode != TrainingMode.Playing) return;
            if (Phase == 3) { game.Notify("지금은 움직임을 읽자. 장판을 피하고 검으로 빈틈을 노리자."); return; }
            if (Transformed || SpiritWarningRemaining > 0 || (Phase >= 2 && spiritTimer <= 0) || HazardActive || WindingUp || phasePending || recoveryTimer > 0) return;
            UsingShortcut = TimeCrushed = false;
            Remaining = Definition.questionTime;
            questionElapsed = 0;
            stepIndex = 0;
            game.ShowCalamityMode(TrainingMode.CalamityChallenge);
        }

        public void Rethink()
        {
            if (!Active || game.Mode != TrainingMode.CalamityChallenge || UsingShortcut || Remaining <= 0) return;
            UsingShortcut = true;
            insightMask |= 1 << (problemIndex % 3);
            game.UI.RefreshOverlay();
        }

        public void Submit(int answer)
        {
            if (!Active || game.Mode != TrainingMode.CalamityChallenge || Remaining <= 0 || answer < 0 || answer >= Question.choices.Length) return;
            if (answer != Question.correctIndex) { FinishQuestion(false, false); return; }
            if (!UsingShortcut && ++stepIndex < Problem.directSteps.Length)
            {
                game.UI.ShowAnswerResult(true);
                game.UI.RefreshOverlay("계산은 맞다. 남은 시간을 살피자.");
                return;
            }
            FinishQuestion(true, false);
        }

        void FinishQuestion(bool correct, bool timedOut)
        {
            game.UI.ShowAnswerResult(correct, timedOut);
            Rage = Mathf.Clamp(Rage + (correct ? -1 : 1), 0, Definition.maximumRage);
            LastEvent = correct ? "정답 · 패턴 완화 " + Rage : (timedOut ? "시간이 끊겼다" : "식이 이어지지 않았다") + " · 패턴 강화 " + Rage;
            if (!correct)
            {
                insightMask |= 1 << (problemIndex % 3);
                LastEvent += "\n다음에는 R로 식 전체의 구조를 살펴보자.";
                Hurt(Definition.wrongDamage, timedOut ? "시간 압축으로 인한 룬 대결 시간 초과" : "룬 대결의 오답 피해", true);
                if (game.Mode == TrainingMode.GameOver) return;
            }
            else
            {
                problemIndex++;
                stepIndex = 0;
                game.ResolveCalamityRune();
                if (!Active) return;
            }
            WindingUp = false;
            questionTimer = Definition.questionInterval;
            attackTimer = Definition.attackInterval;
            ArmHazard(correct);
            game.ShowCalamityMode(TrainingMode.Playing);
            game.Notify(LastEvent + "\n장판 밖으로 이동! 정답이어도 장판 안은 위험하다.", 5);
        }

        void ArmHazard(bool correct)
        {
            HazardRadius = correct && Phase < 3 ? Definition.narrowRadius : Definition.wideRadius;
            HazardX = Mathf.Clamp(game.player.transform.position.x, -Definition.arenaHalfWidth + HazardRadius, Definition.arenaHalfWidth - HazardRadius);
            HazardRemaining = Definition.hazardWarning;
            HazardActive = true;
        }

        public int ReceiveHit(int amount)
        {
            if (!Active || amount <= 0) return 0;
            int floor = Phase == 1 ? Mathf.CeilToInt(Definition.health * Definition.secondPhaseHealth) :
                Phase == 2 ? Mathf.CeilToInt(Definition.health * Definition.thirdPhaseHealth) : 0;
            int dealt = Mathf.Min(amount, Mathf.Max(0, Health - floor));
            Health -= dealt;
            if (Phase < 3 && Health <= floor) phasePending = true;
            if (Health == 0) Win();
            return dealt;
        }

        void NextPhase()
        {
            phasePending = WindingUp = false;
            Phase++;
            SpiritRemaining = SpiritWarningRemaining = 0;
            spiritTimer = Phase == 2 ? 0 : Definition.spiritInterval;
            attackIndex = 0;
            attackTimer = Definition.attackInterval;
            questionTimer = Definition.questionInterval;
            hazardTimer = Phase == 3 ? 0 : Definition.finalHazardInterval;
            game.ShowCalamityMode(TrainingMode.CalamityPhase);
        }

        public void ContinuePhase()
        {
            if (Active && game.Mode == TrainingMode.CalamityPhase) game.ShowCalamityMode(TrainingMode.Playing);
        }

        public void Hurt(int amount, string cause, bool fromQuiz)
        {
            DeathCause = cause;
            game.DamagePlayer(amount, fromQuiz);
        }

        void Win()
        {
            Active = HazardActive = WindingUp = false;
            SpiritRemaining = SpiritWarningRemaining = 0;
            Won = true;
            game.HideCalamityBoss();
            game.ShowCalamityMode(TrainingMode.CalamityVictory);
            game.SaveCalamity(CalamityProgress.Defeated);
        }

        public void RememberVictory()
        {
            if (Won && game.Mode == TrainingMode.CalamityVictory && game.SaveCalamity(CalamityProgress.Defeated))
                game.UI.RefreshOverlay("재앙을 쓰러뜨린 순간을 기억했습니다.");
        }

        public void Reset()
        {
            Active = Won = HazardActive = WindingUp = LowSweep = phasePending = UsingShortcut = TimeCrushed = false;
            Health = Rage = attackIndex = problemIndex = stepIndex = insightMask = 0;
            Phase = 1;
            SpiritRemaining = SpiritWarningRemaining = HazardRemaining = Remaining = 0;
            attackTimer = questionTimer = pressureTimer = spiritTimer = hazardTimer = recoveryTimer = questionElapsed = 0;
            LastEvent = DeathCause = "";
        }
    }
}
