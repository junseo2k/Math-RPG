using System;

namespace MathRPG
{
    public enum RematchProgress { None = 0, Introduction = 1, Lesson = 2, Duel = 3, Victory = 4, Departed = 5 }
    public enum CalamityProgress { None = 0, Introduction = 1, Farewell = 2, Isolated = 3, Defeated = 4 }
    public enum EndingBeat { None = 0, Appointment = 1, Family = 2, King = 3, RivalOne = 4, RivalTwo = 5, Companion = 6, Present = 7, Complete = 8 }
    public enum ChapterFourBeat
    {
        None = 0, Disaster = 1, Commander = 2, Defeat = 3, Alone = 4, Letter = 5, Torn = 6, Reunion = 7,
        Provocation = 8, Collapse = 9, Parents = 10, Anger = 11, Overload = 12,
        InnerWorld = 13, SpiritPast = 14, Consolation = 15, Question = 16,
        Memory = 17, Calm = 18, Awakening = 19
    }

    [Serializable]
    public sealed class ChapterFourProgress
    {
        public ChapterFourBeat beat;
        public int storyOrderVersion;
        public RematchProgress secondRematch;
        public CalamityProgress calamity;
        public EndingBeat ending;
        public bool EndingComplete => ending == EndingBeat.Complete;
        public bool Started => beat != ChapterFourBeat.None;
        public bool LetterOpened => storyOrderVersion == 0 ? beat >= ChapterFourBeat.Letter :
            beat == ChapterFourBeat.Letter || beat == ChapterFourBeat.Torn || beat >= ChapterFourBeat.Parents;
        public bool LetterTorn => storyOrderVersion == 0 ? beat >= ChapterFourBeat.Torn :
            beat == ChapterFourBeat.Torn || beat >= ChapterFourBeat.Parents;
        public ChapterFourBeat NextBeat
        {
            get
            {
                if (storyOrderVersion != 0)
                {
                    switch (beat)
                    {
                        case ChapterFourBeat.Alone: return ChapterFourBeat.Reunion;
                        case ChapterFourBeat.Collapse: return ChapterFourBeat.Letter;
                        case ChapterFourBeat.Torn: return ChapterFourBeat.Parents;
                    }
                }
                return (ChapterFourBeat)((int)beat + 1);
            }
        }
        public bool InInnerWorld => beat >= ChapterFourBeat.InnerWorld && beat <= ChapterFourBeat.Memory;
        public bool SpiritRevealed => beat >= ChapterFourBeat.InnerWorld;
        public bool Awakened => beat >= ChapterFourBeat.Awakening;
    }
}
