using System.Collections.Generic;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public class SolverResult
    {
        public SolverResult(Coordinate coordinate, double probability, bool? verdict)
        {
            Coordinate = coordinate;
            Probability = probability;
            Verdict = verdict;
        }

        public Coordinate Coordinate { get; }
        public double Probability { get; set; }
        public IDictionary<int, double> HintProbabilities { get; set; }
        public bool? Verdict { get; set; }

        public override string ToString()
        {
            return $"Coordinate: {Coordinate}, Probability: {Probability}, Verdict: {Verdict}";
        }
    }
}