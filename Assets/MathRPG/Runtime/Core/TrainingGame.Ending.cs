using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public EndingDefinition Ending { get; private set; }
        public bool HasEndingData => Ending != null && Ending.IsValid();
        public bool HasEndingProgress => Progress.chapterFour.ending != EndingBeat.None;
        public bool EndingScreen => Mode == TrainingMode.EndingStory || Mode == TrainingMode.EndingComplete;
        public EndingBeat CurrentEndingBeat { get; private set; }
        public float EndingElapsed { get; private set; }
        public bool EndingReplay { get; private set; }
        public bool CanAdvanceEnding => EndingScreen && HasEndingData &&
            EndingElapsed >= Mathf.Clamp(Ending.Page(CurrentEndingBeat).minimumHold, .2f, 5);

        void InitializeEnding() => Ending = Resources.Load<EndingDefinition>("MathRPG/Ending");

        public void BeginEnding()
        {
            if (Mode != TrainingMode.CalamityVictory || !Calamity.Won) return;
            if (!HasEndingData)
            {
                UI.RefreshOverlay("엔딩 데이터가 없습니다. Math RPG → Open Training Prototype을 다시 열어주세요.");
                return;
            }
            if (Progress.chapterFour.calamity != CalamityProgress.Defeated && !SaveCalamity(CalamityProgress.Defeated)) return;
            if (!HasEndingProgress && !SaveEndingBeat(EndingBeat.Appointment)) return;
            RestoreEnding();
        }

        bool SaveEndingBeat(EndingBeat next)
        {
            var previous = Progress.chapterFour.ending;
            Progress.chapterFour.ending = next;
            if (CheckpointStore.Save(SavePath, Progress, out var error)) return true;
            Progress.chapterFour.ending = previous;
            UI.RefreshOverlay("기억을 남기지 못했습니다: " + error);
            return false;
        }

        void RestoreEnding()
        {
            if (!HasEndingProgress || !HasEndingData) return;
            ResetArena();
            courseView.SetRegionVisible(false);
            enemy.gameObject.SetActive(false);
            player.gameObject.SetActive(false);
            if (instructor != null) instructor.gameObject.SetActive(false);
            if (spirit != null) spirit.gameObject.SetActive(false);
            Ambient = Notice = "";
            ambientPlayed = true;
            EndingReplay = false;
            ShowEndingBeat(Progress.chapterFour.ending);
        }

        void ShowEndingBeat(EndingBeat beat)
        {
            CurrentEndingBeat = beat;
            EndingElapsed = 0;
            SetMode(beat == EndingBeat.Complete ? TrainingMode.EndingComplete : TrainingMode.EndingStory);
        }

        public void AdvanceEnding()
        {
            if (!CanAdvanceEnding || CurrentEndingBeat == EndingBeat.Complete) return;
            var next = (EndingBeat)((int)CurrentEndingBeat + 1);
            if (!EndingReplay && !SaveEndingBeat(next)) return;
            ShowEndingBeat(next);
        }

        public void ReplayEnding()
        {
            if (!CanAdvanceEnding || Mode != TrainingMode.EndingComplete || !Progress.chapterFour.EndingComplete) return;
            EndingReplay = true;
            ShowEndingBeat(EndingBeat.Appointment);
        }

        public void SaveEndingRecord()
        {
            if (!EndingScreen || !HasEndingProgress) return;
            if (SaveEndingBeat(Progress.chapterFour.ending))
                UI.RefreshOverlay(Progress.chapterFour.EndingComplete ? "엔딩 완료 기록을 남겼습니다." : "현재 장면을 기억했습니다.");
        }

        void TickEndingInput(float dt)
        {
            if (!EndingScreen) return;
            EndingElapsed += dt;
            if (GameInput.Pressed(Key.F9)) { LoadCheckpoint(); return; }
            if (GameInput.Pressed(Key.F5)) { SaveEndingRecord(); return; }
            if (GameInput.Pressed(Key.E)) AdvanceEnding();
        }
    }
}
