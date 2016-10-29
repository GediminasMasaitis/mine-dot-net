using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using MineDotNet.AI;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new TextMapParser();
            Map map;
            using (var file = File.OpenRead("map.txt"))
            {
                map = parser.Parse(file);
            }
            
            var visualizer = new TextMapVisualizer();
            var mapStr = visualizer.VisualizeToString(map);
            Console.WriteLine(mapStr);
            Console.WriteLine();

            var simpleSolver = new SimpleSolver();
            simpleSolver.Debug += AiOnDebug;

            var borderSeparationSolver = new BorderSeparationSolver();
            borderSeparationSolver.Debug += AiOnDebug;

            var solver = new AggregateSolver(simpleSolver, borderSeparationSolver);
            solver.Debug += AiOnDebug;

            var verdicts = solver.Solve(map);
            Console.WriteLine();
            foreach (var verdict in verdicts)
            {
                Console.WriteLine(verdict.Value.ToString());
            }
            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }

        private static void AiOnDebug(string s)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(s);
            Console.ResetColor();
        }
    }
}
