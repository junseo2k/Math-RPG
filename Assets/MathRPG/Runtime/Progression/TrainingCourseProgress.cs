using System;

namespace MathRPG
{
    [Serializable]
    public sealed class TrainingStageProgress
    {
        public bool cleared;
        public bool trialAccepted;
        public int huntKills;
        public int openedChestMask;
        public bool ChestOpen(int index) => index >= 0 && index < 2 && (openedChestMask & (1 << index)) != 0;
    }

    public sealed class TrainingCourseProgress
    {
        public event Action Changed;
        public TrainingCourseDefinition Definition { get; }
        public CheckpointData Data { get; private set; }
        public int Index => Data.currentStage;
        public TrainingStageDefinition Stage => Definition.stages[Index];
        public TrainingStageProgress State => Data.stages[Index];
        public bool Finished => Data.stages[TrainingCourseDefinition.StageCount - 1].cleared;

        public TrainingCourseProgress(TrainingCourseDefinition definition, CheckpointData data)
        { Definition = definition; Data = data; }

        public void Restore(CheckpointData data) { Data = data; Changed?.Invoke(); }

        public bool Move(int direction)
        {
            if (direction != -1 && direction != 1) return false;
            int next = Index + direction;
            if (next < 0 || next >= Definition.stages.Length || (direction > 0 && !State.cleared)) return false;
            Data.currentStage = next;
            Changed?.Invoke();
            return true;
        }

        public void Accept()
        {
            if (State.cleared || State.trialAccepted) return;
            State.trialAccepted = true;
            Changed?.Invoke();
        }

        public void RecordHuntKill()
        {
            if (Stage.trialKind != InstructorTrialKind.Hunt || !State.trialAccepted || State.cleared) return;
            State.huntKills = Math.Min(State.huntKills + 1, Stage.huntTarget);
            Changed?.Invoke();
        }

        public bool OpenChest(int index)
        {
            if (Stage.trialKind != InstructorTrialKind.Treasure || index < 0 || index >= 2 || State.ChestOpen(index)) return false;
            State.openedChestMask |= 1 << index;
            Changed?.Invoke();
            return true;
        }

        public bool Complete()
        {
            if (State.cleared) return false;
            State.cleared = true;
            State.trialAccepted = true;
            Data.skillBooks++;
            Data.instructorCleared = Data.stages[0].cleared;
            Changed?.Invoke();
            return true;
        }
    }
}
