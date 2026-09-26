using System;
using System.IO;
using UnityEngine;

namespace MathRPG
{
    [Serializable]
    public sealed class CheckpointData
    {
        public int version = 18;
        public HuntingGroundProgress hunting = new HuntingGroundProgress();
        public ChapterFourProgress chapterFour = new ChapterFourProgress();
        public HumanVillageProgress human = new HumanVillageProgress();
        public ForestProgressData forest = new ForestProgressData();
        public bool instructorCleared;
        public int skillBooks;
        public int deaths;
        public int currentStage;
        public bool rivalDefeated;
        public TrainingStageProgress[] stages = CreateStages();

        public static TrainingStageProgress[] CreateStages()
        {
            var result = new TrainingStageProgress[TrainingCourseDefinition.StageCount];
            for (int i = 0; i < result.Length; i++) result[i] = new TrainingStageProgress();
            return result;
        }
    }

    public static class CheckpointStore
    {
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "math-rpg-training-v1.json");

        public static bool Save(string path, CheckpointData data, out string error)
        {
            try
            {
                Normalize(data);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                // An interrupted write must never destroy the last valid checkpoint.
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { error = e.Message; return false; }
        }

        public static bool Load(string path, out CheckpointData data, out string error)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) { error = "저장된 기억이 없습니다."; return false; }
                string json = File.ReadAllText(path);
                if (!json.Contains("\"version\"") || !json.Contains("\"skillBooks\""))
                    throw new ArgumentException("저장 형식이 올바르지 않습니다.");
                var loaded = JsonUtility.FromJson<CheckpointData>(json);
                if (loaded != null && loaded.version >= 2 && !json.Contains("\"stages\""))
                    throw new ArgumentException("훈련소 기록이 없는 기억입니다.");
                if (loaded != null && loaded.version >= 4 && !json.Contains("\"forest\""))
                    throw new ArgumentException("숲 마을 기록이 없는 기억입니다.");
                if (loaded != null && loaded.version >= 8 && !json.Contains("\"human\""))
                    throw new ArgumentException("인간 마을 기록이 없는 기억입니다.");
                if (loaded != null && loaded.version >= 13 && !json.Contains("\"chapterFour\""))
                    throw new ArgumentException("챕터4 기록이 없는 기억입니다.");
                if (loaded != null && loaded.version >= 15 && !json.Contains("\"secondRematch\""))
                    throw new ArgumentException("라이벌2 리매치 기록이 없는 기억입니다.");
                if (loaded != null && loaded.version >= 16 && !json.Contains("\"calamity\""))
                    throw new ArgumentException("최종보스 기록이 없는 기억입니다.");
                if (loaded != null && loaded.version >= 17 && !json.Contains("\"ending\""))
                    throw new ArgumentException("엔딩 기록이 없는 기억입니다.");
                if (loaded != null && loaded.version >= 18 && !json.Contains("\"hunting\""))
                    throw new ArgumentException("사냥터 기록이 없는 기억입니다.");
                Normalize(loaded);
                data = loaded;
                error = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { error = e.Message; return false; }
        }

        static void Normalize(CheckpointData data)
        {
            if (data == null || data.deaths < 0 || data.version < 1 || data.version > 18)
                throw new ArgumentException("지원하지 않거나 손상된 기억입니다.");
            if (data.version == 1)
            {
                if (data.skillBooks < 0 || data.skillBooks > 2 || data.instructorCleared != (data.skillBooks > 0))
                    throw new ArgumentException("이전 기억의 보상 기록이 올바르지 않습니다.");
                data.stages = CheckpointData.CreateStages();
                data.stages[0].cleared = data.instructorCleared;
                data.stages[0].trialAccepted = data.instructorCleared;
                data.skillBooks = data.instructorCleared ? 1 : 0;
                data.currentStage = 0;
                data.version = 2;
            }
            if (data.version == 2)
            {
                data.rivalDefeated = false;
                data.version = 3;
            }
            if (data.version == 3)
            {
                data.forest = new ForestProgressData();
                data.version = 4;
            }
            if (data.version == 4 && data.forest != null)
            {
                data.forest.villageIndex = 0;
                data.forest.secondVisited = false;
                data.forest.secondQuestAccepted = false;
                data.forest.parallelogramRelic = false;
                data.version = 5;
            }
            if (data.version == 5 && data.forest != null)
            {
                if (data.forest.villageIndex < 0 || data.forest.villageIndex > 1)
                    throw new ArgumentException("이전 기억의 마을 위치가 올바르지 않습니다.");
                data.forest.thirdVisited = false;
                data.forest.thirdQuestAccepted = false;
                data.forest.circleRelic = false;
                data.version = 6;
            }
            if (data.version == 6 && data.forest != null)
            {
                data.forest.guardianAwakened = false;
                data.forest.aggregateDefeated = false;
                data.version = 7;
            }
            if (data.forest == null ||
                (data.forest.guardianAwakened && data.forest.RelicCount != 3) ||
                (data.forest.guardianAwakened && !data.forest.outbreakWitnessed) ||
                (data.forest.guardianHelpUsed && !data.forest.guardianAwakened) ||
                (data.forest.outbreakWitnessed && (!data.forest.letterWritten || !data.forest.circleRelic)) ||
                (data.forest.letterWritten && !data.forest.circleRelic) ||
                (data.forest.aggregateDefeated && !data.forest.guardianAwakened) ||
                data.forest.villageIndex < 0 || data.forest.villageIndex > 2 ||
                ((data.forest.villageIndex == 2 || data.forest.thirdVisited || data.forest.thirdQuestAccepted || data.forest.circleRelic) && !data.forest.parallelogramRelic) ||
                (data.forest.thirdQuestAccepted && !data.forest.thirdVisited) ||
                (data.forest.circleRelic && !data.forest.thirdQuestAccepted) ||
                ((data.forest.villageIndex >= 1 || data.forest.secondVisited || data.forest.secondQuestAccepted || data.forest.parallelogramRelic) && !data.forest.triangleRelic) ||
                (data.forest.secondQuestAccepted && !data.forest.secondVisited) ||
                (data.forest.parallelogramRelic && !data.forest.secondQuestAccepted) ||
                ((data.forest.inVillage || data.forest.visited || data.forest.questAccepted || data.forest.triangleRelic) && !data.rivalDefeated) ||
                (data.forest.questAccepted && !data.forest.visited) ||
                (data.forest.triangleRelic && !data.forest.questAccepted) ||
                (data.forest.inVillage && data.currentStage != 3))
                throw new ArgumentException("숲 마을 기록이 올바르지 않습니다.");
            if (data.version == 7)
            {
                data.human = new HumanVillageProgress();
                data.version = 8;
            }
            if (data.version == 8 && data.human != null)
            {
                data.human.villageIndex = 0;
                data.human.secondVisited = false;
                data.human.secondQuestAccepted = false;
                data.human.secondBossDefeated = false;
                data.version = 9;
            }
            if (data.version == 9 && data.human != null)
            {
                if (data.human.villageIndex < 0 || data.human.villageIndex > 1)
                    throw new ArgumentException("이전 기억의 인간 마을 위치가 올바르지 않습니다.");
                data.human.thirdVisited = false;
                data.human.thirdQuestAccepted = false;
                data.human.thirdBossDefeated = false;
                data.version = 10;
            }
            if (data.version == 10 && data.human != null)
            {
                data.human.letterReceived = false;
                data.human.disasterWitnessed = false;
                data.human.climaxDefeated = false;
                data.human.royalSealObtained = false;
                data.human.climaxVictorySeen = false;
                data.version = 11;
            }
            if (data.version == 11 && data.human != null)
            {
                data.human.sealTaken = false;
                data.human.rivalSecondDefeated = false;
                data.human.chapter3Complete = false;
                data.version = 12;
            }
            if (data.version == 12)
            {
                data.chapterFour = new ChapterFourProgress();
                data.version = 13;
            }
            if (data.version == 13)
            {
                if (data.chapterFour == null || data.chapterFour.beat < ChapterFourBeat.None || data.chapterFour.beat > ChapterFourBeat.Reunion)
                    throw new ArgumentException("이전 기억의 챕터4 진행 기록이 올바르지 않습니다.");
                data.version = 14;
            }
            if (data.version == 14 && data.chapterFour != null)
            {
                data.chapterFour.secondRematch = RematchProgress.None;
                data.version = 15;
            }
            if (data.version == 15 && data.chapterFour != null)
            {
                data.chapterFour.calamity = CalamityProgress.None;
                data.version = 16;
            }
            if (data.version == 16 && data.chapterFour != null)
            {
                data.chapterFour.ending = EndingBeat.None;
                data.version = 17;
            }
            if (data.human == null ||
                (data.human.sealTaken && (!data.human.royalSealObtained || !data.human.climaxVictorySeen)) ||
                (data.human.rivalSecondDefeated && !data.human.sealTaken) ||
                (data.human.chapter3Complete && (!data.human.rivalSecondDefeated || data.human.inVillage || data.forest.inVillage)) ||
                (data.human.sealTaken && !data.human.chapter3Complete && (!data.human.inVillage || data.human.villageIndex != 2)) ||
                (data.human.letterReceived && !data.human.thirdBossDefeated) ||
                (data.human.disasterWitnessed && !data.human.letterReceived) ||
                (data.human.climaxDefeated && !data.human.disasterWitnessed) ||
                (data.human.royalSealObtained != data.human.climaxDefeated) ||
                (data.human.climaxVictorySeen && !data.human.climaxDefeated) ||
                data.human.villageIndex < 0 || data.human.villageIndex > 2 ||
                ((data.human.villageIndex == 2 || data.human.thirdVisited || data.human.thirdQuestAccepted || data.human.thirdBossDefeated) && !data.human.secondBossDefeated) ||
                (data.human.thirdQuestAccepted && !data.human.thirdVisited) ||
                (data.human.thirdBossDefeated && !data.human.thirdQuestAccepted) ||
                ((data.human.villageIndex >= 1 || data.human.secondVisited || data.human.secondQuestAccepted || data.human.secondBossDefeated) && !data.human.bossDefeated) ||
                (data.human.secondQuestAccepted && !data.human.secondVisited) ||
                (data.human.secondBossDefeated && !data.human.secondQuestAccepted) ||
                ((data.human.inVillage || data.human.visited || data.human.questAccepted || data.human.bossDefeated) && !data.forest.aggregateDefeated) ||
                (data.human.inVillage && (data.forest.inVillage || data.currentStage != 3)) ||
                (data.human.questAccepted && !data.human.visited) ||
                (data.human.bossDefeated && !data.human.questAccepted))
                throw new ArgumentException("인간 마을 기록이 올바르지 않습니다.");
            if (data.chapterFour == null ||
                data.chapterFour.beat < ChapterFourBeat.None || data.chapterFour.beat > ChapterFourBeat.Awakening ||
                (data.chapterFour.Started && !data.human.chapter3Complete))
                throw new ArgumentException("챕터4 진행 기록이 올바르지 않습니다.");
            if (data.chapterFour.secondRematch < RematchProgress.None || data.chapterFour.secondRematch > RematchProgress.Departed ||
                (data.chapterFour.secondRematch != RematchProgress.None && !data.chapterFour.Awakened))
                throw new ArgumentException("라이벌2 리매치 진행 기록이 올바르지 않습니다.");
            if (data.chapterFour.calamity < CalamityProgress.None || data.chapterFour.calamity > CalamityProgress.Defeated ||
                (data.chapterFour.calamity != CalamityProgress.None && data.chapterFour.secondRematch != RematchProgress.Departed))
                throw new ArgumentException("최종보스 진행 기록이 올바르지 않습니다.");
            if (data.chapterFour.ending < EndingBeat.None || data.chapterFour.ending > EndingBeat.Complete ||
                (data.chapterFour.ending != EndingBeat.None && data.chapterFour.calamity != CalamityProgress.Defeated))
                throw new ArgumentException("엔딩 진행 기록이 올바르지 않습니다.");
            if (data.stages == null || data.stages.Length != TrainingCourseDefinition.StageCount ||
                data.currentStage < 0 || data.currentStage >= data.stages.Length)
                throw new ArgumentException("훈련소 기록이 올바르지 않습니다.");
            int books = 0, unlocked = 0;
            bool locked = false;
            for (int i = 0; i < data.stages.Length; i++)
            {
                var stage = data.stages[i];
                if (stage == null || stage.huntKills < 0 || stage.openedChestMask < 0 || stage.openedChestMask > 3 ||
                    (i != 1 && stage.huntKills != 0) || (i != 2 && stage.openedChestMask != 0) ||
                    (locked && (stage.cleared || stage.trialAccepted || stage.huntKills > 0 || stage.openedChestMask > 0)))
                    throw new ArgumentException("훈련소 진행 순서가 올바르지 않습니다.");
                if (!locked) unlocked = i;
                if (stage.cleared) books++;
                else locked = true;
            }
            if (data.currentStage > unlocked || data.skillBooks != books)
                throw new ArgumentException("해금 또는 스킬북 기록이 올바르지 않습니다.");
            if (data.rivalDefeated && books != TrainingCourseDefinition.StageCount)
                throw new ArgumentException("라이벌 결투 이전의 훈련 기록이 없습니다.");
            data.instructorCleared = data.stages[0].cleared;
            if (data.version == 17)
            {
                data.hunting = HuntingGroundProgress.FromLegacy(data);
                data.version = 18;
            }
            if (data.hunting == null || !data.hunting.IsValid(data))
                throw new ArgumentException("사냥터 진행 기록이 올바르지 않습니다.");
        }
    }
}
