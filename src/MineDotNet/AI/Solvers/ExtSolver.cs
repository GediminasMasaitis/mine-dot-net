using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MineDotNet.Common;
using MineDotNet.IO;

namespace MineDotNet.AI.Solvers
{
    public class ExtSolver : ISolver
    {
        public const string Alias = "C++";
        private static ExtSolver InnerInstance { get; set; }

        public static ExtSolver Instance => InnerInstance ?? (InnerInstance = new ExtSolver());

        private struct ExtPt
        {
            public int x;
            public int y;

            public override string ToString()
            {
                return $"{nameof(x)}: {x}, {nameof(y)}: {y}";
            }
        }

        private struct ExtSolverResult
        {
            public ExtPt pt;
            public double probability;
            public uint verdict;

            public override string ToString()
            {
                return $"{nameof(pt)}: {pt}, {nameof(probability)}: {probability}, {nameof(verdict)}: {verdict}";
            }
        }

        [DllImport("minedotcpp.dll", EntryPoint = "init_solver")]
        private static extern void ExtInitSolver(ExternalBorderSeparationSolverSettings settings);

        [DllImport("minedotcpp.dll", EntryPoint = "solve")]
        private static extern int ExtSolve([MarshalAs(UnmanagedType.LPStr)]string mapStr, IntPtr results_buffer, ref int buffer_size);

        private TextMapVisualizer Visualizer;
        private BorderSeparationSolverSettings Settings { get; set; }

        private ExtSolver()
        {
            Visualizer = new TextMapVisualizer();
        }

        public void InitSolver(BorderSeparationSolverSettings settings)
        {
            Settings = settings;
            var extSettings = new ExternalBorderSeparationSolverSettings(settings);
            ExtInitSolver(extSettings);
        }

        public IDictionary<Coordinate, SolverResult> Solve(IMap map)
        {
            if (Settings == null)
            {
                InitSolver(new BorderSeparationSolverSettings());
            }
            var str = Visualizer.VisualizeToString(map);
            str = str.Replace("\r", string.Empty);
            var buffer_size = map.Width * map.Height;
            var structSize = Marshal.SizeOf(typeof(ExtSolverResult));
            var buffer = Marshal.AllocHGlobal(structSize * buffer_size);
            ExtSolve(str, buffer, ref buffer_size);
            var results = new Dictionary<Coordinate, SolverResult>();
            for (var i = 0; i < buffer_size; i++)
            {
                var ptr = new IntPtr(structSize * i + buffer.ToInt64());
                var extRes = (ExtSolverResult)Marshal.PtrToStructure(ptr, typeof(ExtSolverResult));
                var coord = new Coordinate(extRes.pt.x, extRes.pt.y);
                bool? verdict;
                switch (extRes.verdict)
                {
                    case 0:
                        verdict = null;
                        break;
                    case 1:
                        verdict = true;
                        break;
                    case 2:
                        verdict = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var res = new SolverResult(coord, extRes.probability, verdict);
                results.Add(res.Coordinate, res);
            }
            Marshal.FreeHGlobal(buffer);
            return results;
        }
    }
}
