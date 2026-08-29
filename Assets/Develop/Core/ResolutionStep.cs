using System.Collections.Generic;

namespace NeonSeven.Core
{
    public sealed class ResolutionStep
    {
        public ResolutionStep(int wave, IReadOnlyList<BallData> matches, int hiddenHits, int score)
        {
            Wave = wave;
            Matches = matches;
            HiddenHits = hiddenHits;
            Score = score;
        }

        public int Wave { get; }
        public IReadOnlyList<BallData> Matches { get; }
        public int HiddenHits { get; }
        public int Score { get; }
    }
}
