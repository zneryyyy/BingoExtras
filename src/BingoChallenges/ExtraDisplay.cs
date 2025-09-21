using RWCustom;

namespace BingoMode.BingoChallenges
{
    public class Range : Word
    {
        public Range(int min, int current, int max) : base()
        {
            display = new FLabel(
                Custom.GetFont(),
                $"[{min}<{current}<{max}]"
            );
        }
    }
}
