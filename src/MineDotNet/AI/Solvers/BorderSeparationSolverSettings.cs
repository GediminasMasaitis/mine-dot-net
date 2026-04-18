namespace MineDotNet.AI.Solvers
{
    public class BorderSeparationSolverSettings
    {
        public bool TrivialSolve { get; set; } = true;
        public bool TrivialStopOnNoMineVerdict { get; set; } = true;
        public bool TrivialStopOnAnyVerdict { get; set; } = false;
        public bool TrivialStopAlways { get; set; } = false;

        public bool GaussianSolve { get; set; } = true;
        public bool GaussianResolveOnSuccess { get; set; } = true;
        public bool GaussianSingleStopOnNoMineVerdict { get; set; } = true;
        public bool GaussianSingleStopOnAnyVerdict { get; set; } = false;
        public bool GaussianSingleStopAlways { get; set; } = false;
        public bool GaussianStopOnNoMineVerdict { get; set; } = true;
        public bool GaussianStopOnAnyVerdict { get; set; } = false;
        public bool GaussianStopAlways { get; set; } = false;

        public bool SeparationSolve { get; set; } = true;
        public bool SeparationOrderBordersBySize { get; set; } = true;
        public bool SeparationOrderBordersBySizeDescending { get; set; } = false;
        public bool SeparationSingleBorderStopOnNoMineVerdict { get; set; } = true;
        public bool SeparationSingleBorderStopOnAnyVerdict { get; set; } = false;
        public bool SeparationSingleBorderStopAlways { get; set; } = false;

        public bool PartialSolve { get; set; } = true;
        public bool PartialSingleStopOnNoMineVerdict { get; set; } = false;
        public bool PartialSingleStopOnAnyVerdict { get; set; } = false;
        public bool PartialAllStopOnNoMineVerdict { get; set; } = true;
        public bool PartialAllStopOnAnyVerdict { get; set; } = false;
        public bool PartialStopAlways { get; set; } = false;
        public int PartialSolveFromSize { get; set; } = 24;
        public int PartialOptimalSize { get; set; } = 20;
        public bool PartialSetProbabilityGuesses { get; set; } = true;

        public bool ResplitOnPartialVerdict { get; set; } = true;
        public bool ResplitOnCompleteVerdict { get; set; } = false;

        public bool MineCountIgnoreCompletely { get; set; } = false;
        public bool MineCountSolve { get; set; } = true;
        public bool MineCountSolveNonBorder { get; set; } = true;

        public int GiveUpFromSize { get; set; } = 28;

        public bool ValidCombinationSearchOpenCl { get; set; } = true;
        public bool ValidCombinationSearchOpenClAllowLoopBreak { get; set; } = false;
        public int ValidCombinationSearchOpenClUseFromSize { get; set; } = 16;
        public int ValidCombinationSearchOpenClMaxBatchSize { get; set; } = 31;
        public int ValidCombinationSearchOpenClPlatformID { get; set; } = 0;
        public int ValidCombinationSearchOpenClDeviceID { get; set; } = 0;

        public bool ValidCombinationSearchMultithread { get; set; } = true;
        public int ValidCombinationSearchMultithreadUseFromSize { get; set; } = 8;
        public int ValidCombinationSearchMultithreadThreadCount { get; set; } = 16;

        public bool CombinationSearchGaussianReduction { get; set; } = true;
        public bool CombinationSearchGaussianBacktracking { get; set; } = true;

        // When true, partial_solve is skipped for borders that the full
        // enumeration will handle anyway. Empirically a small net negative on
        // expert boards, so defaults to false (always run partial).
        public bool PartialSolveOnlyWhenGivingUp { get; set; } = false;

        // Master switch for [trace] output in the C++ solver. Not exposed via
        // UMSI (its printf output would corrupt the protocol channel), so
        // setting this from the client does nothing — tracing is force-off
        // in the engine.
        public bool PrintTrace { get; set; } = false;
        public int PrintTraceMinEffectiveSize { get; set; } = 20;
        public long PrintTraceMinSolveUs { get; set; } = 100000;

        public int VariableMineCountBordersProbabilitiesMultithreadUseFrom { get; set; } = 128;
        public int VariableMineCountBordersProbabilitiesGiveUpFrom { get; set; } = 131072;

        public bool GuessIfNoNoMineVerdict { get; set; } = true;
        public bool GuessIfNoVerdict { get; set; } = false;

        public int DebugSetting1 { get; set; } = 0;
        public int DebugSetting2 { get; set; } = 0;
        public int DebugSetting3 { get; set; } = 0;
    }
}
