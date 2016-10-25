using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public class BorderSeparationSolver : SolverBase
    {
        public override IDictionary<Coordinate,Verdict> Solve(Map map)
        {
            map.BuildNeighbourCache();
            var allProbabilities = new Dictionary<Coordinate, decimal>();

            var commonBorder = GetBorderCells(map);
            OnDebugLine("Common border calculated, found " + commonBorder.Count + " cells");
            var splitBorders = SeparateBorders(commonBorder);
            var splitBordersStr = "(" + splitBorders.Select(x=>x.Count.ToString()).Aggregate((x, n) => x + ", " + n) + ")";
            OnDebugLine("Common border split into " + splitBorders.Count + " separate borders " + splitBordersStr);
            foreach (var splitBorder in splitBorders)
            {
                OnDebugLine("Solving " + splitBorder.Count + " cell border");
                OnDebugLine("Attempting " + (1 << splitBorder.Count) + " combinations");
                var validCombinations = GetValidBorderCombinations(map, splitBorder);
                OnDebugLine("Found " + validCombinations.Count + " valid combinations");
                foreach (var validCombination in validCombinations)
                {
                    OnDebugLine(validCombination.Where(x => x.Value == Verdict.HasMine).Select(x => x.Key.ToString()).Aggregate((x,n) => x + ";" + n));
                }
                if (validCombinations.Count == 0)
                {
                    // TODO Must be invalid map... Handle somehow
                }
                var probabilities = GetBorderProbabilities(splitBorder, validCombinations);
                allProbabilities.AddRange(probabilities);
            }

            var commonBorderPredictions = GetCommonBorderPredictions(allProbabilities);
            OnDebugLine("Found " + commonBorderPredictions.Count + " guaranteed moves.");
            if (commonBorderPredictions.Count == 0)
            {
                var lastRiskyPrediction = allProbabilities.MinBy(x => x.Value);
                OnDebugLine("Guessing from a border with " + (1 - lastRiskyPrediction.Value) + " chance of success.");
                return new Dictionary<Coordinate, Verdict> { { lastRiskyPrediction.Key, Verdict.DoesntHaveMine } };
            }
            return commonBorderPredictions;
        }

        private bool IsCoordinateABorder(Map map, Cell cell)
        {
            if (cell.State == CellState.Empty || cell.Flag == CellFlag.HasMine)
            {
                return false;
            }
            var neighbours = map.GetNeighboursOf(cell);
            var hasOpenedNeighbour = neighbours.Any(x => x.State == CellState.Empty);
            return hasOpenedNeighbour;
        }

        private IList<Cell> GetBorderCells(Map map)
        {
            var borderCells = map.AllCells.Where(x => IsCoordinateABorder(map, x)).ToList();
            return borderCells;
        }

        private IList<IList<Cell>> SeparateBorders(IEnumerable<Cell> commonBorder)
        {
            var copy = new List<Cell>(commonBorder);
            var map = new Map(copy);

            var visited = new HashSet<Coordinate>();
            var allCells = new List<IList<Cell>>();
            while (copy.Count > 0)
            {
                var initialCoord = copy[0];
                var cells = new Queue<Cell>();
                var currentCells = new List<Cell>();
                cells.Enqueue(initialCoord);
                while (cells.Count > 0)
                {
                    var cell = cells.Dequeue();
                    var neighbors = map.GetNeighboursOf(cell);
                    currentCells.Add(cell);
                    copy.Remove(cell);
                    visited.Add(cell.Coordinate);
                    foreach (var neighbor in neighbors)
                    {
                        if (visited.Add(neighbor.Coordinate))
                        {
                            cells.Enqueue(neighbor);
                        }
                    }
                }
                allCells.Add(currentCells);
            }
            return allCells;
        }

        private IList<IDictionary<Coordinate, Verdict>> GetValidBorderCombinations(Map map, IList<Cell> splitBorder)
        {
            var totalCombinations = 1 << splitBorder.Count;
            var validPredictions = new ConcurrentBag<IDictionary<Coordinate, Verdict>>();
            var emptyCells = map.AllCells.Where(x => x.State == CellState.Empty).ToList();
            var filledCount = map.AllCells.Count(x => x.State == CellState.Filled);
            var flaggedCount = map.AllCells.Count(x => x.Flag == CellFlag.HasMine);
            var combos = Enumerable.Range(0, totalCombinations);
            Parallel.ForEach(combos, combo =>
            {
                var binaryStr = Convert.ToString(combo, 2).PadLeft(splitBorder.Count, '0');
                var binaries = binaryStr.Select(x => x == '1').ToList();
                var predictions = new Dictionary<Coordinate, Verdict>();
                for (var j = 0; j < splitBorder.Count; j++)
                {
                    var coord = splitBorder[j].Coordinate;
                    var verd = binaries[j] ? Verdict.HasMine : Verdict.DoesntHaveMine;
                    predictions.Add(coord, verd);
                }
                var valid = IsPredictionValid(map, predictions, emptyCells, filledCount, flaggedCount);
                if (valid)
                {
                    validPredictions.Add(predictions);
                }
            });
            return validPredictions.ToList();
        }

        public bool IsPredictionValid(Map map, IDictionary<Coordinate, Verdict> predictions, IList<Cell> emptyCells, int filledCount, int flaggedCount)
        {
            if (!CheckBorderMineCount(map, predictions, filledCount, flaggedCount))
            {
                return false;
            }
            
            foreach (var cell in emptyCells)
            {
                var neighboursWithMine = 0;
                var neighboursWithoutMine = 0;
                var neighbours = map.GetNeighboursOf(cell);
                var filledNeighbours = neighbours.Where(x => x.State == CellState.Filled).ToList();
                bool foundUnknownCell = false;
                foreach (var neighbour in filledNeighbours)
                {
                    if (neighbour.Flag == CellFlag.HasMine)
                    {
                        neighboursWithMine++;
                    }
                    else if (predictions.ContainsKey(neighbour.Coordinate))
                    {
                        if (predictions[neighbour.Coordinate] == Verdict.HasMine)
                        {
                            neighboursWithMine++;
                        }
                        else
                        {
                            neighboursWithoutMine++;
                        }
                    }
                    else
                    {
                        foundUnknownCell = true;
                    }
                }
                if (neighboursWithMine > cell.Hint)
                    return false;
                if (filledNeighbours.Count - neighboursWithoutMine < cell.Hint)
                    return false;
                if (foundUnknownCell)
                    continue;
                if (cell.Hint != neighboursWithMine)
                    return false;
            }
            return true;
        }

        private static bool CheckBorderMineCount(Map map, IDictionary<Coordinate, Verdict> predictions, int filledCount, int flaggedCount)
        {
            // TODO: Doesn't work fully. If the map contains multiple borders, it will check one border at a time and prediction counts will be wrong. For end-game cases there should be an additional check from all borders using a cartesian product from all borders...
            if (map.RemainingMineCount.HasValue)
            {
                var minePredictionCount = predictions.Count(x => x.Value == Verdict.HasMine);
                if (/*flaggedCount + */minePredictionCount > map.RemainingMineCount)
                {
                    return false;
                }
                if (filledCount == flaggedCount + predictions.Count)
                {
                    if (/*flaggedCount + */minePredictionCount != map.RemainingMineCount)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static IDictionary<Coordinate, decimal> GetBorderProbabilities(IList<Cell> splitBorder, IList<IDictionary<Coordinate, Verdict>> validCombinations)
        {
            var probabilities = new Dictionary<Coordinate, decimal>();
            foreach (var cell in splitBorder)
            {
                var mineInCount = validCombinations.Count(x => x[cell.Coordinate] == Verdict.HasMine);
                var probability = (decimal)mineInCount / validCombinations.Count;
                probabilities.Add(cell.Coordinate, probability);
            }
            return probabilities;
        }

        private static Dictionary<Coordinate, Verdict> GetCommonBorderPredictions(IDictionary<Coordinate, decimal> probabilities)
        {
            var commonVerdicts = new Dictionary<Coordinate, Verdict>();
            foreach (var probability in probabilities)
            {
                if(probability.Value == 0)
                    commonVerdicts.Add(probability.Key, Verdict.DoesntHaveMine);
                else if(probability.Value == 1)
                    commonVerdicts.Add(probability.Key, Verdict.HasMine);
            }
            return commonVerdicts;
        }

        private static Dictionary<Coordinate, Verdict> GetCommonBorderPredictions(IList<Cell> splitBorder, IList<IDictionary<Coordinate, Verdict>> validCombinations)
        {
            var commonVerdicts = new Dictionary<Coordinate, Verdict>();
            foreach (var cell in splitBorder)
            {
                var firstVerdict = validCombinations[0][cell.Coordinate];
                if (validCombinations.All(x => x[cell.Coordinate] == firstVerdict))
                {
                    commonVerdicts.Add(cell.Coordinate, firstVerdict);
                }
            }
            return commonVerdicts;
        }
    }
}