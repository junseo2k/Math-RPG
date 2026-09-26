using System;

namespace MathRPG
{
    [Serializable]
    public sealed class ForestProgressData
    {
        public bool inVillage;
        public bool visited;
        public bool questAccepted;
        public bool triangleRelic;
        public int villageIndex;
        public bool secondVisited;
        public bool secondQuestAccepted;
        public bool parallelogramRelic;
        public bool thirdVisited;
        public bool thirdQuestAccepted;
        public bool circleRelic;
        public bool guardianAwakened;
        public bool aggregateDefeated;
        public bool letterWritten;
        public bool outbreakWitnessed;
        public bool guardianHelpUsed;

        public bool CurrentVisited
        {
            get => villageIndex == 0 ? visited : villageIndex == 1 ? secondVisited : thirdVisited;
            set { if (villageIndex == 0) visited = value; else if (villageIndex == 1) secondVisited = value; else thirdVisited = value; }
        }
        public bool CurrentQuestAccepted
        {
            get => villageIndex == 0 ? questAccepted : villageIndex == 1 ? secondQuestAccepted : thirdQuestAccepted;
            set { if (villageIndex == 0) questAccepted = value; else if (villageIndex == 1) secondQuestAccepted = value; else thirdQuestAccepted = value; }
        }
        public bool CurrentRelic
        {
            get => villageIndex == 0 ? triangleRelic : villageIndex == 1 ? parallelogramRelic : circleRelic;
            set { if (villageIndex == 0) triangleRelic = value; else if (villageIndex == 1) parallelogramRelic = value; else circleRelic = value; }
        }
        public int RelicCount => (triangleRelic ? 1 : 0) + (parallelogramRelic ? 1 : 0) + (circleRelic ? 1 : 0);
    }
}
