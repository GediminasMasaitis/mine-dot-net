using System.Collections.Generic;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    internal class PartialBorderData
    {
        public PartialBorderData(HashSet<Coordinate> partialBorderCoordinates, BorderSeparationSolverMap partialMap, Border partialBorder)
        {
            PartialBorderCoordinates = partialBorderCoordinates;
            PartialMap = partialMap;
            PartialBorder = partialBorder;
        }

        public HashSet<Coordinate> PartialBorderCoordinates { get; set; }
        public BorderSeparationSolverMap PartialMap { get; set; }
        public Border PartialBorder { get; set; }
    }
}