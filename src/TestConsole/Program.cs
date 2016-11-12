﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using MineDotNet.AI;
using MineDotNet.AI.Benchmarking;
using MineDotNet.AI.Guessers;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //SolveMapFromFile();
            BenchmarkSolver();
            Console.ReadKey();
        }

        private static void BenchmarkSolver()
        {
            var solver = new BorderSeparationSolver();
            var guesser = new LowestProbabilityGuesser();
            var testsToRun = 500;
            solver.Settings.OnlyTrivialSolving = true;
            solver.Settings.PartialBorderSolving = true;
            solver.Settings.SolveByMineCount = false;
            solver.Settings.StopOnNoMineVerdict = true;
            var benchmarker = new Benchmarker();
            var benchmarkSequence = benchmarker.BenchmarkMultipleDensities(solver, guesser, 16, 16, 0.01, 0.35, 0.01, testsToRun);
            var csv = new StringBuilder();
            foreach (var densityGroup in benchmarkSequence)
            {
                //var i = 1;
                var entries = new List<BenchmarkEntry>(testsToRun);
                foreach (var entry in densityGroup.Entries)
                {
                    //Console.WriteLine($"[{i++}/{testsToRun}] Solved: {entry.Solved} in {entry.TotalDuration}");
                    entries.Add(entry);
                }
                var successRate = entries.Count(x => x.Solved) / (decimal)entries.Count;
                var averageTime = new TimeSpan((long)entries.Select(x => x.TotalDuration.Ticks).Average());
                var averageSteps = entries.Average(x => x.SolvingDuarations.Count);
                csv.AppendLine($"{densityGroup.Density};{testsToRun};{successRate};{averageSteps};{averageTime.TotalMilliseconds:0.000}");
                Console.WriteLine($"Density: {densityGroup.Density:##0.00%}");
                Console.WriteLine($"Tests ran: {testsToRun}");
                Console.WriteLine($"Success rate: {successRate:##0.00%}");
                Console.WriteLine($"Average steps: {averageSteps}");
                Console.WriteLine($"Average time: {averageTime.TotalMilliseconds:0.000}");
                Console.WriteLine();
            }
            File.WriteAllText("benchmark.csv", csv.Replace(",",".").ToString());
            Console.WriteLine("Benchmarks complete.");
        }

        private static void SolveMapFromFile()
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

            var solver = new BorderSeparationSolver();
#if DEBUG
            solver.Debug += AiOnDebug;
#endif

            var verdicts = solver.Solve(map);
            Console.WriteLine();
            foreach (var verdict in verdicts)
            {
                Console.WriteLine(verdict.Value.ToString());
            }
            Console.WriteLine("Press any key to close...");
            
        }

        private static void AiOnDebug(string s)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(s);
            Console.ResetColor();
        }
    }
}
