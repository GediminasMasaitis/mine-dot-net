using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
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
            /*var parser = new TextMapParser();
            var mapStr = File.ReadAllText("C:/Temp/test_map.txt");
            //var mapStr = "####\n123#";
            var map = parser.Parse(mapStr);

            var settings = new BorderSeparationSolverSettings();
            settings.GuessIfNoNoMineVerdict = false;
            settings.SeparationSolve = false;
            var solver = new BorderSeparationSolver(settings);
            var extSolver = new ExtSolver(settings);

            var a = solver.Solve(map);
            var b = extSolver.Solve(map);

            Console.WriteLine(mapStr);*/


            //TestMatrixSolving();
            //SolveMapFromFile();
            BenchmarkSolver();
            //SolveMapFromFile();
            //TestGaussianSolving();
            Console.ReadKey();
        }

        private static void TimeIt(Action a, string name = null)
        {
            name = name ?? "That";
            var sw = new Stopwatch();
            sw.Start();
            a.Invoke();
            sw.Stop();
            Console.WriteLine($"{name} took {sw.Elapsed.TotalMilliseconds:#.000} ms.");
        }

        private static T TimeItReturning<T>(Func<T> a, string name = null)
        {
            name = name ?? "That";
            var sw = new Stopwatch();
            sw.Start();
            var result = a.Invoke();
            sw.Stop();
            Console.WriteLine($"{name} took {sw.Elapsed.TotalMilliseconds:#.000} ms.");
            return result;
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


            var matrix = (IList<int[]>)new List<int[]>
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

            //matrixSolving.ReduceMatrix(ref matrix);
        }

        private static void BenchmarkSolver()
        {
            var settings = new BorderSeparationSolverSettings();
            settings.TrivialSolve = true;
            settings.GaussianSolve = true;
            settings.GuessIfNoNoMineVerdict = false;
            var solver = new ExtSolver(settings);
            var secondarySolver = new BorderSeparationSolver(settings);
            var guesser = new LowestProbabilityGuesser();
            var testsToRun = 500;
            var visualizer = new TextMapVisualizer();
            var benchmarker = new Benchmarker();
            benchmarker.MissingFromPrimary += (map, results) =>
            {
                Console.WriteLine("Missing from primary");
                var mapStr = visualizer.VisualizeToString(map);
                Console.WriteLine(mapStr);
                foreach (var result in results)
                {
                    Console.WriteLine(result.Value);
                }
            };
            benchmarker.MissingFromSecondary += (map, results) =>
            {
                Console.WriteLine("Missing from secondary");
                var mapStr = visualizer.VisualizeToString(map);
                Console.WriteLine(mapStr);
                foreach (var result in results)
                {
                    Console.WriteLine(result.Value);
                }
            };
            var benchmarkSequence = benchmarker.BenchmarkMultipleDensities(solver, guesser, 16, 16, 0.01, 0.35, 0.01, testsToRun);
            var csv = new StringBuilder();
            var csvpath = @"C:\temp\benchmark.csv";
            File.Delete(csvpath);
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
                File.AppendAllText(csvpath, $"{densityGroup.Density};{testsToRun};{successRate};{averageSteps};{averageTime.TotalMilliseconds:0.000}{Environment.NewLine}");
                Console.WriteLine($"Density: {densityGroup.Density:##0.00%}");
                Console.WriteLine($"Tests ran: {testsToRun}");
                Console.WriteLine($"Success rate: {successRate:##0.00%}");
                Console.WriteLine($"Average steps: {averageSteps}");
                Console.WriteLine($"Average time: {averageTime.TotalMilliseconds:0.000}");
                Console.WriteLine();
            }
            Console.WriteLine("Benchmarks complete.");
        }

        private static void SolveMapFromFile()
        {
            var parser = new TextMapParser();
            Map map;
            using (var file = File.OpenRead("C:/Temp/test_map.txt"))
            {
                map = parser.Parse(file);
            }

            var visualizer = new TextMapVisualizer();
            var mapStr = visualizer.VisualizeToString(map);
            Console.WriteLine(mapStr);
            Console.WriteLine();

            var settings = new BorderSeparationSolverSettings();
            settings.TrivialSolve = false;
            settings.GaussianSolve = true;
            settings.GaussianStopAlways = true;
            settings.GuessIfNoNoMineVerdict = false;
            var solver = new ExtSolver(settings);
            //var solver = new BorderSeparationSolver(settings);

            var verdicts = TimeItReturning(() => solver.Solve(map));

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
            solver.Settings.TrivialSolve = false;
            solver.Settings.GaussianSolve = true;
            solver.Settings.GaussianStopAlways = true;

            var secondarySolver = new BorderSeparationSolver();
            secondarySolver.Settings.PartialSolve = false;
            secondarySolver.Settings.GiveUpFromSize = 30;
            
            var benchmarker = new Benchmarker();


            var benchmarkResults = benchmarker.BenchmarkMultipleDensities(solver, guesser, 5,5, 0.01, 0.4, 0.01, 20, secondarySolver).Select(x => x.Entries.ToList()).ToList();
        }
    }
}
