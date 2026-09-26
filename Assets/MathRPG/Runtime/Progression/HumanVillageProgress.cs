using System;

namespace MathRPG
{
    [Serializable]
    public sealed class HumanVillageProgress
    {
        public bool inVillage;
        public int villageIndex;
        public bool secondVisited;
        public bool secondQuestAccepted;
        public bool secondBossDefeated;
        public bool thirdVisited;
        public bool thirdQuestAccepted;
        public bool thirdBossDefeated;
        public bool letterReceived;
        public bool disasterWitnessed;
        public bool climaxDefeated;
        public bool royalSealObtained;
        public bool climaxVictorySeen;
        public bool sealTaken;
        public bool rivalSecondDefeated;
        public bool chapter3Complete;
        public bool visited;
        public bool questAccepted;
        public bool bossDefeated;
        public bool CurrentVisited { get => villageIndex == 0 ? visited : villageIndex == 1 ? secondVisited : thirdVisited; set { if (villageIndex == 0) visited = value; else if (villageIndex == 1) secondVisited = value; else thirdVisited = value; } }
        public bool CurrentQuestAccepted { get => villageIndex == 0 ? questAccepted : villageIndex == 1 ? secondQuestAccepted : thirdQuestAccepted; set { if (villageIndex == 0) questAccepted = value; else if (villageIndex == 1) secondQuestAccepted = value; else thirdQuestAccepted = value; } }
        public bool CurrentBossDefeated { get => villageIndex == 0 ? bossDefeated : villageIndex == 1 ? secondBossDefeated : thirdBossDefeated; set { if (villageIndex == 0) bossDefeated = value; else if (villageIndex == 1) secondBossDefeated = value; else thirdBossDefeated = value; } }
    }
}
