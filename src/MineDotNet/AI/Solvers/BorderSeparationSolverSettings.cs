namespace MineDotNet.AI.Solvers
{
    public class BorderSeparationSolverSettings
    {
        public bool StopOnNoMineVerdict { get; set; }
        public bool StopOnAnyVerdict { get; set; }

        public bool SolveTrivial { get; set; }
        public bool StopAfterTrivialSolving { get; set; }

        public bool SolveGaussian { get; set; }
        public bool StopAfterGaussianSolving { get; set; }

        public bool IgnoreMineCountCompletely { get; set; }
        public bool SolveByMineCount { get; set; }
        public bool SolveNonBorderCells { get; set; }

        public bool PartialBorderSolving { get; set; }
        public bool BorderResplitting { get; set; }
        public int PartialBorderSolveFrom { get; set; }
        public int GiveUpFrom { get; set; }
        public int MaxPartialBorderSize { get; set; }
        public bool SetPartiallyCalculatedProbabilities { get; set; }

        public BorderSeparationSolverSettings()
        {
            StopOnNoMineVerdict = false;
            StopOnAnyVerdict = false;

            SolveTrivial = false;
            StopAfterTrivialSolving = false;

            SolveGaussian = true;
            StopAfterGaussianSolving = false;

            IgnoreMineCountCompletely = false;
            SolveByMineCount = true;
            SolveNonBorderCells = true;

            PartialBorderSolving = true;
            BorderResplitting = true;
            PartialBorderSolveFrom = 18;
            GiveUpFrom = 20;
            MaxPartialBorderSize = 14;
            SetPartiallyCalculatedProbabilities = true;
        }
    }
}