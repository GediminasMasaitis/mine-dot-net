using System;
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
using MineDotNet.Game;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestMatrixSolving();
            //SolveMapFromFile();
            //BenchmarkSolver();
            TestGaussianSolving();
            Console.ReadKey();
        }

        private static void TestMatrixSolving()
        {
            var matrixSolving = new GaussianSolvingService();
            //var matrix = matrixSolving.GetMatrixFromMap(map);

            //var matrix = new int[][]
            //{
            //    new int[] {1, 1, 0, 1},
            //    new int[] {1, 1, 1, 2},
            //    new int[] {0, 1, 1, 1},
            //};

            //var matrix = new int[][]
            //{
            //    new int[] {1, 1, 0, 0, 0, 1},
            //    new int[] {1, 1, 1, 0, 0, 2},
            //    new int[] {0, 1, 1, 1, 1, 3},
            //};


            var matrix = new int[][]
            {
                new int[] {1,1,1,0,0,0,0,0,0,0,0,0,1},
                new int[] {0,1,1,1,0,0,0,0,0,0,0,0,2},
                new int[] {0,0,1,1,1,0,0,0,0,0,0,0,1},
                new int[] {0,0,0,1,1,1,0,0,0,0,0,0,2},
                new int[] {0,0,0,0,1,1,1,0,0,0,0,0,1},
                new int[] {0,0,0,0,0,1,1,1,1,1,0,0,2},
                new int[] {0,0,0,0,0,0,0,0,1,1,1,0,1},
                new int[] {0,0,0,0,0,0,0,0,0,1,1,1,2},
                new int[] {0,0,0,0,0,0,0,0,0,0,1,1,1},
            };

            //var matrix = new int[][]
            //{
            //    new int[] {1, 1, 0, 0, 0, 1},
            //    new int[] {1, 1, 1, 0, 0, 2},
            //    new int[] {0, 1, 1, 1, 0, 2},
            //    new int[] {0, 0, 1, 1, 1, 2},
            //    new int[] {0, 0, 0, 1, 1, 1},
            //    new int[] {1, 1, 1, 1, 1, 3},
            //};

            matrixSolving.ReduceMatrix(ref matrix);
        }

        private static void BenchmarkSolver()
        {
            var solver = new BorderSeparationSolver();
            var guesser = new LowestProbabilityGuesser();
            var testsToRun = 500;
            solver.Settings.SolveTrivial = false;
            solver.Settings.StopAfterTrivialSolving = false;
            solver.Settings.SolveGaussian = true;
            solver.Settings.StopAfterGaussianSolving = true;
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

            var verdicts = solver.Solve(map);
            Console.WriteLine();
            foreach (var verdict in verdicts)
            {
                Console.WriteLine(verdict.Value.ToString());
            }
            Console.WriteLine("Press any key to close...");
        }

        private static void TestGaussianSolving()
        {
            var guesser = new LowestProbabilityGuesser();

            var solver = new BorderSeparationSolver();
            solver.Settings.SolveTrivial = false;
            solver.Settings.StopAfterTrivialSolving = false;
            solver.Settings.SolveGaussian = true;
            solver.Settings.StopAfterGaussianSolving = true;

            var secondarySolver = new BorderSeparationSolver();
            secondarySolver.Settings.PartialBorderSolving = false;
            secondarySolver.Settings.GiveUpFrom = 30;
            
            var benchmarker = new Benchmarker();
            benchmarker.OnMissingResult += (map, results) =>
            {
                var mapStr = new TextMapVisualizer().VisualizeToString(map);
                Console.WriteLine(mapStr);
                foreach (var result in results)
                {
                    Console.WriteLine(result.Value);
                }
            };
            var benchmarkResults = benchmarker.BenchmarkMultipleDensities(solver, guesser, 5,5, 0.01, 0.4, 0.01, 20, secondarySolver).Select(x => x.Entries.ToList()).ToList();
        }
    }
}
