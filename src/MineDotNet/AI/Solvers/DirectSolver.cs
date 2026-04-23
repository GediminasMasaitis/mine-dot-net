using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MineDotNet.Common;
using MineDotNet.IO;

namespace MineDotNet.AI.Solvers
{
    // Opt-in alternative to ExtSolver that P/Invokes straight into
    // minedotcpp_direct.dll instead of piping through UMSI's stdio protocol.
    // Same solver code underneath — just no process boundary, no text
    // serialization of results, no line-buffered readback. Useful for
    // benchmark runs where the stdio round-trip dominates per-call cost.
    //
    // Each instance owns one native solver handle (create_solver/destroy_solver
    // in global_wrappers.cpp) with fully independent state — thread pool,
    // OpenCL context, scratch buffers. That's what lets the benchmark runner
    // spawn multiple workers, each with its own DirectSolver, and run them
    // concurrently. The Instance singleton is kept for existing callers
    // (MainWindow etc.); additional instances are constructed directly.
    public sealed class DirectSolver : ISolver, IDisposable
    {
        public const string Alias = "C++ (direct)";

        private const string Dll = "minedotcpp_direct";

        private static readonly Lazy<DirectSolver> InstanceLazy =
            new Lazy<DirectSolver>(() => new DirectSolver());
        public static DirectSolver Instance => InstanceLazy.Value;

        private readonly TextMapVisualizer _visualizer = new TextMapVisualizer();
        private readonly object _lock = new object();
        private IntPtr _handle = IntPtr.Zero;
        private NativeSolverSettings _lastAppliedSettings;
        private bool _settingsApplied;

        // 4096 is comfortably above any realistic board's result count
        // (max board ~200×200 = 40k cells, but only border cells appear in
        // results and they cap well below this). Keep the buffer preallocated
        // to avoid per-call GC pinning cost.
        private const int MaxResults = 8192;
        private readonly NativeSolverResult[] _resultsBuffer = new NativeSolverResult[MaxResults];

        // Public so the benchmark runner (and anyone who wants independent
        // native state) can instantiate workers beyond the shared singleton.
        public DirectSolver() { }

        public void InitSolver(BorderSeparationSolverSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            lock (_lock)
            {
                var ns = ToNative(settings);
                // Skip re-creating the handle when settings haven't changed
                // since last init. Saves ~25+ field copies per benchmark game
                // AND avoids tearing down a fresh solver/thread-pool/OpenCL
                // context only to build an identical one.
                if (_settingsApplied && _handle != IntPtr.Zero && NativeSolverSettings.Equal(ref _lastAppliedSettings, ref ns)) return;
                if (_handle != IntPtr.Zero)
                {
                    destroy_solver(_handle);
                    _handle = IntPtr.Zero;
                }
                _handle = create_solver(ns);
                if (_handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("create_solver returned null handle");
                }
                _lastAppliedSettings = ns;
                _settingsApplied = true;
            }
        }

        public IDictionary<Coordinate, SolverResult> Solve(IMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            lock (_lock)
            {
                if (!_settingsApplied || _handle == IntPtr.Zero)
                {
                    InitSolver(new BorderSeparationSolverSettings());
                }

                // The C++ text_map_parser reads lines off a stringstream —
                // expects rows separated by '\n' (not UMSI's ';' protocol
                // variant). Strip '\r' to avoid CRLF confusing the parser
                // on Windows, same as the old P/Invoke path.
                var mapText = _visualizer.VisualizeToString(map).Replace("\r", string.Empty);
                var bytes = Encoding.ASCII.GetBytes(mapText + '\0');
                int count = MaxResults;
                int rc;
                unsafe
                {
                    fixed (byte* pText = bytes)
                    fixed (NativeSolverResult* pResults = _resultsBuffer)
                    {
                        rc = solve(_handle, (sbyte*)pText, pResults, &count);
                    }
                }
                if (rc != 1) throw new InvalidOperationException($"Native solve returned {rc} (buffer={MaxResults}, requested={count})");
                // Guard against bogus count values — if the struct layout's
                // off by even a byte the native side may write garbage into
                // buffer_size and we'd march off the end of _resultsBuffer.
                if (count < 0 || count > MaxResults)
                {
                    throw new InvalidOperationException(
                        $"Native solve returned absurd count={count} (buffer={MaxResults}). " +
                        $"Likely a struct-layout mismatch: C# sizeof(NativeSolverSettings)={Marshal.SizeOf<NativeSolverSettings>()}, " +
                        $"sizeof(NativeSolverResult)={Marshal.SizeOf<NativeSolverResult>()}. Expected C++ sizes: 120/24 bytes.");
                }

                var dict = new Dictionary<Coordinate, SolverResult>(count);
                for (var i = 0; i < count; i++)
                {
                    var r = _resultsBuffer[i];
                    // Catch layout-mismatch symptoms at the boundary where
                    // the blame is still obvious — engine.OpenCell() further
                    // up the stack would just raise a generic "index out of
                    // bounds" with no hint that the native side is at fault.
                    if (r.X < 0 || r.X >= map.Width || r.Y < 0 || r.Y >= map.Height)
                    {
                        throw new InvalidOperationException(
                            $"Native solve returned out-of-range coord ({r.X},{r.Y}) for {map.Width}x{map.Height} map at result #{i}/{count}. " +
                            $"Struct sizes: Settings={Marshal.SizeOf<NativeSolverSettings>()}B (expect 120), Result={Marshal.SizeOf<NativeSolverResult>()}B (expect 24).");
                    }
                    bool? verdict = r.Verdict switch
                    {
                        1 => true,   // verdict_has_mine
                        2 => false,  // verdict_doesnt_have_mine
                        _ => (bool?)null
                    };
                    // C++ `point` is {x, y} using column/row; MineDotNet's
                    // Coordinate mirrors x=row, y=col via TextMapVisualizer's
                    // output — i.e. what the solver consumed and produced is
                    // already in the same frame, so this maps 1:1.
                    var coord = new Coordinate(r.X, r.Y);
                    dict[coord] = new SolverResult(coord, r.Probability, verdict);
                }
                return dict;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_handle != IntPtr.Zero)
                {
                    destroy_solver(_handle);
                    _handle = IntPtr.Zero;
                }
                _settingsApplied = false;
            }
        }

        // Mirrors minedotcpp::solvers::solver_settings exactly. Field order,
        // types, and alignment must match the C++ struct — the MSVC default
        // layout for `bool` is 1 byte, `int` is 4, `long long` is 8, with
        // natural alignment between. [MarshalAs(U1)] forces C#'s 4-byte bool
        // marshalling down to 1 byte to match.
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeSolverSettings
        {
            [MarshalAs(UnmanagedType.U1)] public bool TrivialSolve;
            [MarshalAs(UnmanagedType.U1)] public bool TrivialStopOnNoMineVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool TrivialStopOnAnyVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool TrivialStopAlways;
            [MarshalAs(UnmanagedType.U1)] public bool GaussianSolve;
            [MarshalAs(UnmanagedType.U1)] public bool GaussianResolveOnSuccess;
            [MarshalAs(UnmanagedType.U1)] public bool GaussianSingleStopOnNoMineVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool GaussianSingleStopOnAnyVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool GaussianSingleStopAlways;
            [MarshalAs(UnmanagedType.U1)] public bool GaussianAllStopOnNoMineVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool GaussianAllStopOnAnyVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool GaussianAllStopAlways;
            [MarshalAs(UnmanagedType.U1)] public bool SeparationSolve;
            [MarshalAs(UnmanagedType.U1)] public bool SeparationOrderBordersBySize;
            [MarshalAs(UnmanagedType.U1)] public bool SeparationOrderBordersBySizeDescending;
            [MarshalAs(UnmanagedType.U1)] public bool SeparationSingleBorderStopOnNoMineVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool SeparationSingleBorderStopOnAnyVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool SeparationSingleBorderStopAlways;
            [MarshalAs(UnmanagedType.U1)] public bool PartialSolve;
            [MarshalAs(UnmanagedType.U1)] public bool PartialSingleStopOnNoMineVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool PartialSingleStopOnAnyVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool PartialAllStopOnNoMineVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool PartialAllStopOnAnyVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool PartialStopAlways;
            public int PartialSolveFromSize;
            public int PartialOptimalSize;
            [MarshalAs(UnmanagedType.U1)] public bool PartialSetProbabilityGuesses;
            [MarshalAs(UnmanagedType.U1)] public bool ResplitOnPartialVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool ResplitOnCompleteVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool MineCountIgnoreCompletely;
            [MarshalAs(UnmanagedType.U1)] public bool MineCountSolve;
            [MarshalAs(UnmanagedType.U1)] public bool MineCountSolveNonBorder;
            public int GiveUpFromSize;
            [MarshalAs(UnmanagedType.U1)] public bool ValidCombinationSearchOpenCl;
            [MarshalAs(UnmanagedType.U1)] public bool ValidCombinationSearchOpenClAllowLoopBreak;
            public int ValidCombinationSearchOpenClUseFromSize;
            public int ValidCombinationSearchOpenClMaxBatchSize;
            public int ValidCombinationSearchOpenClPlatformId;
            public int ValidCombinationSearchOpenClDeviceId;
            [MarshalAs(UnmanagedType.U1)] public bool ValidCombinationSearchMultithread;
            public int ValidCombinationSearchMultithreadUseFromSize;
            public int ValidCombinationSearchMultithreadThreadCount;
            [MarshalAs(UnmanagedType.U1)] public bool CombinationSearchGaussianReduction;
            [MarshalAs(UnmanagedType.U1)] public bool CombinationSearchGaussianBacktracking;
            [MarshalAs(UnmanagedType.U1)] public bool PartialSolveOnlyWhenGivingUp;
            [MarshalAs(UnmanagedType.U1)] public bool PrintTrace;
            public int PrintTraceMinEffectiveSize;
            public long PrintTraceMinSolveUs;
            public int VariableMineCountBordersProbabilitiesMultithreadUseFrom;
            public int VariableMineCountBordersProbabilitiesGiveUpFrom;
            [MarshalAs(UnmanagedType.U1)] public bool GuessIfNoNoMineVerdict;
            [MarshalAs(UnmanagedType.U1)] public bool GuessIfNoVerdict;
            public int DebugSetting1;
            public int DebugSetting2;
            public int DebugSetting3;

            public static bool Equal(ref NativeSolverSettings a, ref NativeSolverSettings b)
            {
                // Byte-identical compare — OK because the struct is blittable
                // once [MarshalAs(U1)] forces bools to single bytes.
                var sz = Marshal.SizeOf<NativeSolverSettings>();
                unsafe
                {
                    fixed (NativeSolverSettings* pa = &a)
                    fixed (NativeSolverSettings* pb = &b)
                    {
                        var aB = (byte*)pa;
                        var bB = (byte*)pb;
                        for (var i = 0; i < sz; i++)
                            if (aB[i] != bB[i]) return false;
                    }
                }
                return true;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeSolverResult
        {
            public int X;
            public int Y;
            public double Probability;
            public int Verdict;
        }

        private static NativeSolverSettings ToNative(BorderSeparationSolverSettings s) => new NativeSolverSettings
        {
            TrivialSolve = s.TrivialSolve,
            TrivialStopOnNoMineVerdict = s.TrivialStopOnNoMineVerdict,
            TrivialStopOnAnyVerdict = s.TrivialStopOnAnyVerdict,
            TrivialStopAlways = s.TrivialStopAlways,
            GaussianSolve = s.GaussianSolve,
            GaussianResolveOnSuccess = s.GaussianResolveOnSuccess,
            GaussianSingleStopOnNoMineVerdict = s.GaussianSingleStopOnNoMineVerdict,
            GaussianSingleStopOnAnyVerdict = s.GaussianSingleStopOnAnyVerdict,
            GaussianSingleStopAlways = s.GaussianSingleStopAlways,
            GaussianAllStopOnNoMineVerdict = s.GaussianStopOnNoMineVerdict,
            GaussianAllStopOnAnyVerdict = s.GaussianStopOnAnyVerdict,
            GaussianAllStopAlways = s.GaussianStopAlways,
            SeparationSolve = s.SeparationSolve,
            SeparationOrderBordersBySize = s.SeparationOrderBordersBySize,
            SeparationOrderBordersBySizeDescending = s.SeparationOrderBordersBySizeDescending,
            SeparationSingleBorderStopOnNoMineVerdict = s.SeparationSingleBorderStopOnNoMineVerdict,
            SeparationSingleBorderStopOnAnyVerdict = s.SeparationSingleBorderStopOnAnyVerdict,
            SeparationSingleBorderStopAlways = s.SeparationSingleBorderStopAlways,
            PartialSolve = s.PartialSolve,
            PartialSingleStopOnNoMineVerdict = s.PartialSingleStopOnNoMineVerdict,
            PartialSingleStopOnAnyVerdict = s.PartialSingleStopOnAnyVerdict,
            PartialAllStopOnNoMineVerdict = s.PartialAllStopOnNoMineVerdict,
            PartialAllStopOnAnyVerdict = s.PartialAllStopOnAnyVerdict,
            PartialStopAlways = s.PartialStopAlways,
            PartialSolveFromSize = s.PartialSolveFromSize,
            PartialOptimalSize = s.PartialOptimalSize,
            PartialSetProbabilityGuesses = s.PartialSetProbabilityGuesses,
            ResplitOnPartialVerdict = s.ResplitOnPartialVerdict,
            ResplitOnCompleteVerdict = s.ResplitOnCompleteVerdict,
            MineCountIgnoreCompletely = s.MineCountIgnoreCompletely,
            MineCountSolve = s.MineCountSolve,
            MineCountSolveNonBorder = s.MineCountSolveNonBorder,
            GiveUpFromSize = s.GiveUpFromSize,
            ValidCombinationSearchOpenCl = s.ValidCombinationSearchOpenCl,
            ValidCombinationSearchOpenClAllowLoopBreak = s.ValidCombinationSearchOpenClAllowLoopBreak,
            ValidCombinationSearchOpenClUseFromSize = s.ValidCombinationSearchOpenClUseFromSize,
            ValidCombinationSearchOpenClMaxBatchSize = s.ValidCombinationSearchOpenClMaxBatchSize,
            ValidCombinationSearchOpenClPlatformId = s.ValidCombinationSearchOpenClPlatformID,
            ValidCombinationSearchOpenClDeviceId = s.ValidCombinationSearchOpenClDeviceID,
            ValidCombinationSearchMultithread = s.ValidCombinationSearchMultithread,
            ValidCombinationSearchMultithreadUseFromSize = s.ValidCombinationSearchMultithreadUseFromSize,
            ValidCombinationSearchMultithreadThreadCount = s.ValidCombinationSearchMultithreadThreadCount,
            CombinationSearchGaussianReduction = s.CombinationSearchGaussianReduction,
            CombinationSearchGaussianBacktracking = s.CombinationSearchGaussianBacktracking,
            PartialSolveOnlyWhenGivingUp = s.PartialSolveOnlyWhenGivingUp,
            PrintTrace = s.PrintTrace,
            PrintTraceMinEffectiveSize = s.PrintTraceMinEffectiveSize,
            PrintTraceMinSolveUs = s.PrintTraceMinSolveUs,
            VariableMineCountBordersProbabilitiesMultithreadUseFrom = s.VariableMineCountBordersProbabilitiesMultithreadUseFrom,
            VariableMineCountBordersProbabilitiesGiveUpFrom = s.VariableMineCountBordersProbabilitiesGiveUpFrom,
            GuessIfNoNoMineVerdict = s.GuessIfNoNoMineVerdict,
            GuessIfNoVerdict = s.GuessIfNoVerdict,
            DebugSetting1 = s.DebugSetting1,
            DebugSetting2 = s.DebugSetting2,
            DebugSetting3 = s.DebugSetting3
        };

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create_solver(NativeSolverSettings settings);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern void destroy_solver(IntPtr handle);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int solve(IntPtr handle, sbyte* map_str, NativeSolverResult* results_buffer, int* buffer_size);
    }
}
