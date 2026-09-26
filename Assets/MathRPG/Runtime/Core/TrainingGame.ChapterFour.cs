using UnityEngine;
using UnityEngine.InputSystem;

namespace MathRPG
{
    public sealed partial class TrainingGame
    {
        public ChapterFourDefinition ChapterFour { get; private set; }
        public bool HasChapterFourData => ChapterFour != null && ChapterFour.IsValid();
        public bool InChapterFour => Progress.chapterFour.Started;
        public bool ChapterFourCanAdvance => Mode == TrainingMode.ChapterFourStory && chapterFourDelay <= 0;
        public float ChapterFourElapsed { get; private set; }
        float chapterFourDelay;
        AudioSource chapterFourAudio;
        AudioClip generatedPaperTear;

        void InitializeChapterFour()
        {
            ChapterFour = Resources.Load<ChapterFourDefinition>("MathRPG/ChapterFour");
            chapterFourAudio = gameObject.AddComponent<AudioSource>();
            chapterFourAudio.playOnAwake = false;
        }

        public void BeginChapterFour()
        {
            if (Mode != TrainingMode.HumanChapterComplete || !Progress.human.chapter3Complete) return;
            if (!HasChapterFourData)
            {
                UI.RefreshOverlay("챕터4 데이터가 없습니다. Math RPG → Open Training Prototype에서 콘텐츠를 생성해주세요.");
                return;
            }
            if (!InChapterFour)
            {
                int previousVersion = Progress.chapterFour.storyOrderVersion;
                Progress.chapterFour.storyOrderVersion = 1;
                if (!SaveChapterFourBeat(ChapterFourBeat.Disaster))
                {
                    Progress.chapterFour.storyOrderVersion = previousVersion;
                    return;
                }
            }
            RestoreChapterFour();
        }

        bool SaveChapterFourBeat(ChapterFourBeat next)
        {
            var previous = Progress.chapterFour.beat;
            Progress.chapterFour.beat = next;
            if (CheckpointStore.Save(SavePath, Progress, out var error)) return true;
            Progress.chapterFour.beat = previous;
            UI.RefreshOverlay("기억을 남기지 못했습니다: " + error);
            return false;
        }

        public void AdvanceChapterFour()
        {
            if (!ChapterFourCanAdvance || !InChapterFour || !HasChapterFourData) return;
            if (Progress.chapterFour.Awakened)
            {
                for (int i = 0; i < 3; i++)
                    if (RequireHuntingGround(4, i)) return;
                SecondRematch.Begin();
                return;
            }
            var next = Progress.chapterFour.NextBeat;
            if (!SaveChapterFourBeat(next)) return;
            ShowChapterFourBeat();
        }

        void RestoreChapterFour()
        {
            if (!InChapterFour) return;
            if (HasEndingProgress) { RestoreEnding(); return; }
            if (InCalamity) { Calamity.Restore(); return; }
            if (InSecondRematch) { SecondRematch.Restore(); return; }
            ResetArena();
            courseView.SetRegionVisible(false);
            enemy.gameObject.SetActive(false);
            player.gameObject.SetActive(false);
            if (instructor != null) instructor.gameObject.SetActive(false);
            if (spirit != null) spirit.gameObject.SetActive(false);
            Ambient = Notice = "";
            ambientPlayed = true;
            ShowChapterFourBeat();
        }

        void ShowChapterFourBeat()
        {
            ChapterFourElapsed = 0;
            chapterFourDelay = ChapterFour.HoldDuration(Progress.chapterFour.beat);
            SetMode(TrainingMode.ChapterFourStory);
            if (Progress.chapterFour.beat == ChapterFourBeat.Torn) PlayChapterFourPaper();
        }

        void TickChapterFourInput(float dt)
        {
            if (Mode != TrainingMode.ChapterFourStory) return;
            ChapterFourElapsed += dt;
            chapterFourDelay = Mathf.Max(0, chapterFourDelay - dt);
            if (GameInput.Pressed(Key.F9)) { LoadCheckpoint(); return; }
            if (GameInput.Pressed(Key.F5))
            {
                if (SaveChapterFourBeat(Progress.chapterFour.beat)) UI.RefreshOverlay("이 순간을 기억했습니다.");
                return;
            }
            if (GameInput.Pressed(Key.E)) AdvanceChapterFour();
        }

        void PlayChapterFourPaper()
        {
            var clip = ChapterFour.paperTear;
            if (clip == null)
            {
                if (generatedPaperTear == null)
                {
                    const int rate = 22050;
                    float duration = Mathf.Clamp(ChapterFour.paperDuration, .1f, 5f);
                    var samples = new float[Mathf.CeilToInt(rate * duration)];
                    var noise = new System.Random(41);
                    float previous = 0;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        float t = (float)i / samples.Length;
                        float current = (float)noise.NextDouble() * 2 - 1;
                        samples[i] = (current - previous) * Mathf.Sin(t * Mathf.PI) * (.15f + .1f * Mathf.Sin(t * 117));
                        previous = current;
                    }
                    generatedPaperTear = AudioClip.Create("Paper tear", samples.Length, 1, rate, false);
                    generatedPaperTear.SetData(samples, 0);
                }
                clip = generatedPaperTear;
            }
            chapterFourAudio.PlayOneShot(clip, ChapterFour.paperVolume);
        }

        void DisposeChapterFour()
        {
            if (generatedPaperTear != null) Destroy(generatedPaperTear);
        }
    }
}
