using System;

namespace MathRPG
{
    public sealed class InstructorTrials
    {
        readonly TrainingCourseProgress course;
        public event Action Completed;
        public int CompletedQuestions { get; private set; }
        public bool SparringActive { get; private set; }

        public InstructorTrials(TrainingCourseProgress course) { this.course = course; }

        public void Begin()
        {
            if (course.State.cleared || SparringActive) return;
            course.Accept();
            CompletedQuestions = 0;
            if (course.Stage.trialKind == InstructorTrialKind.Sparring) SparringActive = true;
            CheckCollection();
        }

        public void AnswerQuestion()
        {
            if (course.Stage.trialKind != InstructorTrialKind.Questions || !course.State.trialAccepted || course.State.cleared) return;
            CompletedQuestions++;
            if (CompletedQuestions >= course.Stage.questionCount) Finish();
        }

        public void EnemyDefeated()
        {
            if (SparringActive)
            { SparringActive = false; Finish(); return; }
            course.RecordHuntKill();
            CheckCollection();
        }

        public bool OpenChest(int index)
        {
            if (!course.OpenChest(index)) return false;
            CheckCollection();
            return true;
        }

        void CheckCollection()
        {
            if (!course.State.trialAccepted || course.State.cleared) return;
            if ((course.Stage.trialKind == InstructorTrialKind.Hunt && course.State.huntKills >= course.Stage.huntTarget) ||
                (course.Stage.trialKind == InstructorTrialKind.Treasure && course.State.openedChestMask == 3)) Finish();
        }

        void Finish() { if (course.Complete()) Completed?.Invoke(); }
        public void ResetSession() { SparringActive = false; CompletedQuestions = 0; }
    }
}
