using System;

namespace MathRPG
{
    [Serializable]
    public sealed class HuntingAreaProgress
    {
        public int kills;
        public int lessonsRead;
        public bool completed;
    }

    [Serializable]
    public sealed class HuntingGroundProgress
    {
        public int activeIndex = -1;
        public int chapterIntroductions;
        public HuntingAreaProgress[] areas = CreateAreas();

        static HuntingAreaProgress[] CreateAreas()
        {
            var result = new HuntingAreaProgress[HuntingGroundDefinition.AreaCount];
            for (int i = 0; i < result.Length; i++) result[i] = new HuntingAreaProgress();
            return result;
        }

        public static HuntingGroundProgress FromLegacy(CheckpointData data)
        {
            var result = new HuntingGroundProgress();
            for (int i = 0; i < 3; i++) result.areas[i].completed = data.currentStage > i || data.rivalDefeated;
            result.areas[3].completed = data.forest.triangleRelic;
            result.areas[4].completed = data.forest.parallelogramRelic;
            result.areas[5].completed = data.forest.circleRelic;
            result.areas[6].completed = data.human.bossDefeated;
            result.areas[7].completed = data.human.secondBossDefeated;
            result.areas[8].completed = data.human.thirdBossDefeated;
            for (int i = 9; i < 12; i++) result.areas[i].completed = data.chapterFour.secondRematch != RematchProgress.None;
            return result;
        }

        public bool IsValid(CheckpointData data)
        {
            if (areas == null || areas.Length != HuntingGroundDefinition.AreaCount || activeIndex < -1 || activeIndex >= areas.Length ||
                chapterIntroductions < 0 || chapterIntroductions > 15) return false;
            for (int i = 0; i < areas.Length; i++)
            {
                var area = areas[i];
                if (area == null || area.kills < 0 || area.kills > 1000 || area.lessonsRead < 0 || area.lessonsRead > 8) return false;
                if (i % 3 != 0 && !areas[i - 1].completed && (area.completed || area.kills > 0 || area.lessonsRead > 0 || activeIndex == i)) return false;
            }
            if (activeIndex < 0) return true;
            int local = activeIndex % 3;
            if (activeIndex < 3) return !data.forest.inVillage && !data.human.inVillage && !data.chapterFour.Started &&
                data.currentStage == local && data.stages[local].cleared;
            if (activeIndex < 6) return data.forest.inVillage && data.forest.villageIndex == local &&
                data.forest.CurrentQuestAccepted && !data.forest.CurrentRelic;
            if (activeIndex < 9) return data.human.inVillage && data.human.villageIndex == local &&
                data.human.CurrentQuestAccepted && !data.human.CurrentBossDefeated;
            return data.chapterFour.Awakened && data.chapterFour.secondRematch == RematchProgress.None;
        }
    }
}
