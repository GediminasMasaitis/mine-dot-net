namespace MineDotNet.AI.Solvers
{
    public class BorderSeparationSolverSettings
    {
        public bool OnlyTrivialSolving { get; set; }
        public bool StopOnNoMineVerdict { get; set; }
        public bool StopOnAnyVerdict { get; set; }
        public bool IgnoreMineCountCompletely { get; set; }
        public bool SolveByMineCount { get; set; }
        public bool SolveNonBorderCells { get; set; }
        public int PartialBorderSolveFrom { get; set; }
        public int GiveUpFrom { get; set; }
        public int MaxPartialBorderSize { get; set; }
        public bool SetPartiallyCalculatedProbabilities { get; set; }

        public BorderSeparationSolverSettings()
        {
            OnlyTrivialSolving = false;
            StopOnNoMineVerdict = false;
            StopOnAnyVerdict = false;

            IgnoreMineCountCompletely = false;
            SolveByMineCount = true;
            SolveNonBorderCells = true;

            PartialBorderSolveFrom = 20;
            GiveUpFrom = 20;
            MaxPartialBorderSize = 15;
            SetPartiallyCalculatedProbabilities = true;
        }
    }
}