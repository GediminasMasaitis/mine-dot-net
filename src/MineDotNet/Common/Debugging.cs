using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MineDotNet.AI.Solvers;

namespace MineDotNet.Common
{
    public static class Debugging
    {
        internal static void Visualize(IMap mainMap)
        {
            Visualize(mainMap, (IEnumerable<Coordinate>[])null);
        }

        internal static void Visualize(IMap mainMap, params Border[] borders)
        {
            Visualize(mainMap, borders.Select(x => x.Cells.Select(y => y.Coordinate)).ToArray());
        }

        public static void Visualize(IMap mainMap, params IEnumerable<Cell>[] regions)
        {
            Visualize(mainMap, regions.Select(x => x.Select(y => y.Coordinate)).ToArray());
        }

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (var item in enumeration)
            {
                action(item);
            }
        }

        public static void Visualize(IMap mainMap, params IEnumerable<Coordinate>[] regions)
        {
            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#if DEBUG
            var visualizerPath = currentPath + @"\..\..\..\MineDotNet.GUI\bin\Debug\MineDotNet.GUI.exe";
#elif TEST
            var visualizerPath =  currentPath + @"\..\..\..\MineDotNet.GUI\bin\Test\MineDotNet.GUI.exe";
#else
            var visualizerPath =  currentPath + @"\..\..\..\MineDotNet.GUI\bin\Release\MineDotNet.GUI.exe";
#endif
            var visualizer = new TextMapVisualizer();
            var maps = new List<IMap>();
            maps.Add(mainMap);
            if (regions != null)
            {
                foreach (var region in regions)
                {
                    var maskMap = new Map(mainMap.Width, mainMap.Height, null, true);
                    foreach (var coordinate in region)
                    {
                        maskMap[coordinate].State = CellState.Filled;
                        ;
                    }
                    maps.Add(maskMap);
                }
            }
            var parameterStr = maps.Select(x => visualizer.VisualizeToString(x).Replace(Environment.NewLine, ";")).Aggregate((x, n) => x + " " + n);
            var startInfo = new ProcessStartInfo(visualizerPath);
            startInfo.Arguments = parameterStr;
            Process.Start(startInfo);
        }
    }
}
