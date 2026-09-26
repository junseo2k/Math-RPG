using UnityEngine;

namespace MathRPG
{
    public sealed class RivalChallenge
    {
        readonly float timeLimit, rivalSolveTime;
        public QuizSession Quiz { get; }
        public float Elapsed { get; private set; }
        public float Remaining => Mathf.Max(0, timeLimit - Elapsed);
        public float RivalProgress => Mathf.Clamp01(Elapsed / rivalSolveTime);
        public bool RivalReady => Elapsed >= rivalSolveTime;
        public bool Finished { get; private set; }
        public bool PlayerReady => Quiz.Completed;

        public RivalChallenge(QuestionDefinition question, int hints, float limit, float rivalTime)
        {
            Quiz = new QuizSession(question, hints);
            timeLimit = limit;
            rivalSolveTime = rivalTime;
        }

        public void Tick(float dt)
        {
            if (Finished) return;
            Elapsed += Mathf.Max(0, dt);
            Finished = Remaining <= 0 || (PlayerReady && RivalReady);
        }

        public AnswerResult Answer(int index)
            => Finished ? AnswerResult.Ignored : Quiz.Answer(index);
    }
}
