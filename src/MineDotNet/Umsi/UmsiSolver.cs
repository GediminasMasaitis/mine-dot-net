using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.IO;

namespace MineDotNet.Umsi
{
    public interface IUmsiSolver : ISolver, IDisposable
    {
        
    }

    public interface IUmsiProgram : IDisposable
    {
        void EnsureRunning();
    }

    public class UmsiProgram : IUmsiProgram
    {
        private Process _process;
        private object _sync;

        public UmsiProgram()
        {
            _sync = new object();
        }
        
        public void EnsureRunning()
        {
            lock (_sync)
            {
                if (_process != null)
                {
                    return;
                }
                _process = new Process();
            }
            

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "C:\\Users\\gedim\\AppData\\Local\\Programs\\Python\\Python38\\python.exe";
            startInfo.Arguments = "C:\\Users\\gedim\\Documents\\Projects\\Minesweeper\\mine-dot-py\\mineDotPy.py";
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            
            _process.StartInfo = startInfo;
            _process.Exited += (sender, args) =>
            {
                Stop();
            };
            
            _process.Start();
        }

        public void Stop()
        {
            _process?.Kill();
            _process?.Dispose();
            lock (_sync)
            {
                _process = null;
            }
        }

        public async Task SendAsync(string text)
        {
            _process.StandardInput.WriteLineAsync(text);
            _process.StandardOutput.ReadLineAsync();
        }
        
        public void Dispose()
        {
            Stop();
        }
    }

    public class UmsiSolver : IUmsiSolver
    {
        private readonly IUmsiProgram _program;
        private readonly IStringMapVisualizer _visualizer;

        public UmsiSolver(IUmsiProgram program, IStringMapVisualizer visualizer)
        {
            _program = program;
            _visualizer = visualizer;
        }

        public IDictionary<Coordinate, SolverResult> Solve(IMap map)
        {
            _program.EnsureRunning();
            var str = _visualizer.VisualizeToString(map, false);
            return null;
        }

        public void Dispose()
        {
            _program.Dispose();
        }
    }
}
