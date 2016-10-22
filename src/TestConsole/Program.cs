﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using MineDotNet.AI;
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
            using (var consoleOut = Console.OpenStandardOutput())
            {
                visualizer.Visualize(map, consoleOut);
            }
            Console.WriteLine();
            var ai = new Analyzer();
            ai.Debug += AiOnDebug;

            var verdicts = ai.SolveComplex(map);
            Console.WriteLine();
            foreach (var verdict in verdicts)
            {
                Console.WriteLine(verdict.Key + ": " + verdict.Value);
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
