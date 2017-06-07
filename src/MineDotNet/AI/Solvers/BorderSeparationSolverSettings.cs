using System.Runtime.InteropServices;

namespace MineDotNet.AI.Solvers
{
    public class BorderSeparationSolverSettings
    {
        public bool TrivialSolve { get; set; } = true;
        public bool TrivialStopOnNoMineVerdict { get; set; } = true;
        public bool TrivialStopOnAnyVerdict { get; set; } = false;
        public bool TrivialStopAlways { get; set; } = false;

        public bool GaussianSolve { get; set; } = true;
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
        public int PartialOptimalSize { get; set; } = 14;
        public bool PartialSetProbabilityGuesses { get; set; } = true;

        public bool ResplitOnPartialVerdict { get; set; } = true;
        public bool ResplitOnCompleteVerdict { get; set; } = false;

        public bool MineCountIgnoreCompletely { get; set; } = false;
        public bool MineCountSolve { get; set; } = true;
        public bool MineCountSolveNonBorder { get; set; } = true;

        public int GiveUpFromSize { get; set; } = 28;

        // TODO
        public bool ValidCombinationSearchOpenCl { get; set; } = true;
        public bool ValidCombinationSearchOpenClAllowLoopBreak { get; set; } = true;
        public int ValidCombinationSearchOpenClUseFromSize { get; set; } = 16;
        public int ValidCombinationSearchOpenClMaxBatchSize { get; set; } = 20;
        public int ValidCombinationSearchOpenClPlatformID { get; set; } = 1;
        public int ValidCombinationSearchOpenClDeviceID { get; set; } = 0;

        public bool ValidCombinationSearchMultithread { get; set; } = true;
        public int ValidCombinationSearchMultithreadUseFromSize { get; set; } = 6; //2097152

        public int VariableMineCountBordersProbabilitiesMultithreadUseFrom { get; set; } = 32;

        public bool GuessIfNoNoMineVerdict { get; set; } = true;
        public bool GuessIfNoVerdict { get; set; } = false;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ExternalBorderSeparationSolverSettings
    {
        [MarshalAs(UnmanagedType.U1)]
        public bool TrivialSolve;
        [MarshalAs(UnmanagedType.U1)]
        public bool TrivialStopOnNoMineVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool TrivialStopOnAnyVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool TrivialStopAlways;

        [MarshalAs(UnmanagedType.U1)]
        public bool GaussianSolve;
        [MarshalAs(UnmanagedType.U1)]
        public bool GaussianStopOnNoMineVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool GaussianStopOnAnyVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool GaussianStopAlways;

        [MarshalAs(UnmanagedType.U1)]
        public bool SeparationSolve;
        [MarshalAs(UnmanagedType.U1)]
        public bool SeparationOrderBordersBySize;
        [MarshalAs(UnmanagedType.U1)]
        public bool SeparationOrderBordersBySizeDescending;
        [MarshalAs(UnmanagedType.U1)]
        public bool SeparationSingleBorderStopOnNoMineVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool SeparationSingleBorderStopOnAnyVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool SeparationSingleBorderStopAlways;

        [MarshalAs(UnmanagedType.U1)]
        public bool PartialSolve;
        [MarshalAs(UnmanagedType.U1)]
        public bool PartialSingleStopOnNoMineVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool PartialSingleStopOnAnyVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool PartialAllStopOnNoMineVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool PartialAllStopOnAnyVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool PartialStopAlways;
        public int PartialSolveFromSize;
        public int PartialOptimalSize;
        [MarshalAs(UnmanagedType.U1)]
        public bool PartialSetProbabilityGuesses;

        [MarshalAs(UnmanagedType.U1)]
        public bool ResplitOnPartialVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool ResplitOnCompleteVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool MineCountIgnoreCompletely;
        [MarshalAs(UnmanagedType.U1)]
        public bool MineCountSolve;
        [MarshalAs(UnmanagedType.U1)]
        public bool MineCountSolveNonBorder;

        public int GiveUpFromSize;

        // TODO
        [MarshalAs(UnmanagedType.U1)]
        public bool ValidCombinationSearchOpenCl;
        [MarshalAs(UnmanagedType.U1)]
        public bool ValidCombinationSearchOpenClAllowLoopBreak;
        public int ValidCombinationSearchOpenClUseFromSize;
        public int ValidCombinationSearchOpenClMaxBatchSize;
        public int ValidCombinationSearchOpenClPlatformID;
        public int ValidCombinationSearchOpenClDeviceID;

        [MarshalAs(UnmanagedType.U1)]
        public bool ValidCombinationSearchMultithread;
        public int ValidCombinationSearchMultithreadUseFromSize; //2097152

        public int VariableMineCountBordersProbabilitiesMultithreadUseFrom;

        [MarshalAs(UnmanagedType.U1)]
        public bool GuessIfNoNoMineVerdict;
        [MarshalAs(UnmanagedType.U1)]
        public bool GuessIfNoVerdict;

        public ExternalBorderSeparationSolverSettings(BorderSeparationSolverSettings originalSettings)
        {
            TrivialSolve = originalSettings.TrivialSolve;
            TrivialStopOnNoMineVerdict = originalSettings.TrivialStopOnNoMineVerdict;
            TrivialStopOnAnyVerdict = originalSettings.TrivialStopOnAnyVerdict;
            TrivialStopAlways = originalSettings.TrivialStopAlways;

            GaussianSolve = originalSettings.GaussianSolve;
            GaussianStopOnNoMineVerdict = originalSettings.GaussianStopOnNoMineVerdict;
            GaussianStopOnAnyVerdict = originalSettings.GaussianStopOnAnyVerdict;
            GaussianStopAlways = originalSettings.GaussianStopAlways;

            SeparationSolve = originalSettings.SeparationSolve;
            SeparationOrderBordersBySize = originalSettings.SeparationOrderBordersBySize;
            SeparationOrderBordersBySizeDescending = originalSettings.SeparationOrderBordersBySizeDescending;
            SeparationSingleBorderStopOnNoMineVerdict = originalSettings.SeparationSingleBorderStopOnNoMineVerdict;
            SeparationSingleBorderStopOnAnyVerdict = originalSettings.SeparationSingleBorderStopOnAnyVerdict;
            SeparationSingleBorderStopAlways = originalSettings.SeparationSingleBorderStopAlways;

            PartialSolve = originalSettings.PartialSolve;
            PartialSingleStopOnNoMineVerdict = originalSettings.PartialSingleStopOnNoMineVerdict;
            PartialSingleStopOnAnyVerdict = originalSettings.PartialSingleStopOnAnyVerdict;
            PartialAllStopOnNoMineVerdict = originalSettings.PartialAllStopOnNoMineVerdict;
            PartialAllStopOnAnyVerdict = originalSettings.PartialAllStopOnAnyVerdict;
            PartialStopAlways = originalSettings.PartialStopAlways;
            PartialSolveFromSize = originalSettings.PartialSolveFromSize;
            PartialOptimalSize = originalSettings.PartialOptimalSize;
            PartialSetProbabilityGuesses = originalSettings.PartialSetProbabilityGuesses;

            ResplitOnPartialVerdict = originalSettings.ResplitOnPartialVerdict;
            ResplitOnCompleteVerdict = originalSettings.ResplitOnCompleteVerdict;

            MineCountIgnoreCompletely = originalSettings.MineCountIgnoreCompletely;
            MineCountSolve = originalSettings.MineCountSolve;
            MineCountSolveNonBorder = originalSettings.MineCountSolveNonBorder;

            GiveUpFromSize = originalSettings.GiveUpFromSize;

            // TODO
            ValidCombinationSearchOpenCl = originalSettings.ValidCombinationSearchOpenCl;
            ValidCombinationSearchOpenClAllowLoopBreak = originalSettings.ValidCombinationSearchOpenClAllowLoopBreak;
            ValidCombinationSearchOpenClUseFromSize = originalSettings.ValidCombinationSearchOpenClUseFromSize;
            ValidCombinationSearchOpenClMaxBatchSize = originalSettings.ValidCombinationSearchOpenClMaxBatchSize;
            ValidCombinationSearchOpenClPlatformID = originalSettings.ValidCombinationSearchOpenClPlatformID;
            ValidCombinationSearchOpenClDeviceID = originalSettings.ValidCombinationSearchOpenClDeviceID;

            ValidCombinationSearchMultithread = originalSettings.ValidCombinationSearchMultithread;
            ValidCombinationSearchMultithreadUseFromSize = originalSettings.ValidCombinationSearchMultithreadUseFromSize; //2097152

            VariableMineCountBordersProbabilitiesMultithreadUseFrom = originalSettings.VariableMineCountBordersProbabilitiesMultithreadUseFrom;

            GuessIfNoNoMineVerdict = originalSettings.GuessIfNoNoMineVerdict;
            GuessIfNoVerdict = originalSettings.GuessIfNoVerdict;
        }
    }
}