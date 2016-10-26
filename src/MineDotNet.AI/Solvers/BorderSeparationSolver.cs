using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public class BorderSeparationSolver : SolverBase
    {
        private class Border
        {
            public Border(IList<Cell> cells)
            {
                Cells = cells;
            }
            public IList<Cell> Cells { get; set; }
            public IList<IDictionary<Coordinate, Verdict>> ValidCombinations { get; set; }
            public int SmallestPossibleMineCount { get; set; }
            public IDictionary<Coordinate, decimal> Probabilities { get; set; }
        }

        public override IDictionary<Coordinate,Verdict> Solve(Map map)
        {
            map.BuildNeighbourCache();
            

            var commonBorder = GetBorderCells(map);
            OnDebugLine("Common border calculated, found " + commonBorder.Count + " cells");
            var borders = SeparateBorders(commonBorder).ToList();

            var splitBordersStr = "(" + borders.Select(x=>x.Cells.Count.ToString()).Aggregate((x, n) => x + ", " + n) + ")";
            OnDebugLine("Common border split into " + borders.Count + " separate borders " + splitBordersStr);

            foreach (var border in borders)
            {
                OnDebugLine("Solving " + border.Cells.Count + " cell border");
                OnDebugLine("Attempting " + (1 << border.Cells.Count) + " combinations");

                border.ValidCombinations = FindValidBorderCellCombinations(map, border);

                OnDebugLine("Found " + border.ValidCombinations.Count + " valid combinations");
                foreach (var validCombination in border.ValidCombinations)
                {
                    OnDebugLine(validCombination.Where(x => x.Value == Verdict.HasMine).Select(x => x.Key.ToString()).Aggregate((x,n) => x + ";" + n));
                }
                if (border.ValidCombinations.Count == 0)
                {
                    // TODO Must be invalid map... Handle somehow
                }

                border.SmallestPossibleMineCount = border.ValidCombinations.Min(x => x.Count(y => y.Value == Verdict.HasMine));
            }

            if (map.RemainingMineCount.HasValue)
            {
                TrimValidCombinationsByMineCount(borders, map.RemainingMineCount.Value);
            }

            var allProbabilities = new Dictionary<Coordinate, decimal>();

            foreach (var border in borders)
            {
                border.Probabilities = GetBorderProbabilities(border);
                allProbabilities.AddRange(border.Probabilities);
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

        //private IList<IDictionary<Coordinate, Verdict>> GetCommonValidCombinations()

        private void TrimValidCombinationsByMineCount(IList<Border> borders, int minesRemaining)
        {
            foreach (var border in borders)
            {
                var guaranteedOtherCombinations = borders.Where(x => x != border).Sum(x => x.SmallestPossibleMineCount);
                for (int i = 0; i < border.ValidCombinations.Count; i++)
                {
                    var combination = border.ValidCombinations[i];
                    if (combination.Count(x => x.Value == Verdict.HasMine) + guaranteedOtherCombinations > minesRemaining)
                    {
                        border.ValidCombinations.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        private bool IsCoordinateABorder(Map map, Cell cell)
        {
            if (cell.State != CellState.Filled || cell.Flag == CellFlag.HasMine)
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

        private IEnumerable<Border> SeparateBorders(IEnumerable<Cell> commonBorder)
        {
            var copy = new List<Cell>(commonBorder);
            var map = new Map(copy);

            var visited = new HashSet<Coordinate>();
            //var allCells = new List<IList<Cell>>();
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
                var border = new Border(currentCells);
                yield return border;
            }
        }

        private IList<IDictionary<Coordinate,Verdict>> FindValidBorderCellCombinations(Map map, Border border)
        {
            var totalCombinations = 1 << border.Cells.Count;
            var validPredictions = new ConcurrentBag<IDictionary<Coordinate, Verdict>>();
            var emptyCells = map.AllCells.Where(x => x.State == CellState.Empty).ToList();
            var filledCount = map.AllCells.Count(x => x.State == CellState.Filled);
            var flaggedCount = map.AllCells.Count(x => x.Flag == CellFlag.HasMine);
            var combos = Enumerable.Range(0, totalCombinations);
            Parallel.ForEach(combos, combo =>
            {
                // TODO: optimize to not involve weird binary strings
                var binaryStr = Convert.ToString(combo, 2).PadLeft(border.Cells.Count, '0');
                var binaries = binaryStr.Select(x => x == '1').ToList();
                var predictions = new Dictionary<Coordinate, Verdict>();
                for (var j = 0; j < border.Cells.Count; j++)
                {
                    var coord = border.Cells[j].Coordinate;
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

        private static IDictionary<Coordinate, decimal> GetBorderProbabilities(Border border)
        {
            var probabilities = new Dictionary<Coordinate, decimal>();
            foreach (var cell in border.Cells)
            {
                var mineInCount = border.ValidCombinations.Count(x => x[cell.Coordinate] == Verdict.HasMine);
                var probability = (decimal)mineInCount / border.ValidCombinations.Count;
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