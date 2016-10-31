using MineDotNet.Common;

namespace MineDotNet.AI
{
    public class SolverResult
    {
        public SolverResult(Coordinate coordinate, decimal probability, Verdict? verdict)
        {
            Coordinate = coordinate;
            Probability = probability;
            Verdict = verdict;
        }

        public Coordinate Coordinate { get; }
        public decimal Probability { get; set; }
        public Verdict? Verdict { get; set; }

        public override string ToString()
        {
            return $"Coordinate: {Coordinate}, Probability: {Probability}, Verdict: {Verdict}";
        }
    }
}