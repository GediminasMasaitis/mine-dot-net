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

            var filledCount = map.AllCells.Count(x => x.State == CellState.Filled);
            var flaggedCount = map.AllCells.Count(x => x.Flag == CellFlag.HasMine);
            var undecidedCellsRemaining = filledCount - flaggedCount;

            var commonBorder = GetCommonBorder(map);
            OnDebugLine("Common border calculated, found " + commonBorder.Cells.Count + " cells");
            var borders = SeparateBorders(commonBorder, map).ToList();

            var splitBordersStr = "(" + borders.Select(x=>x.Cells.Count.ToString()).Aggregate((x, n) => x + ", " + n) + ")";
            OnDebugLine("Common border split into " + borders.Count + " separate borders " + splitBordersStr);

            foreach (var border in borders)
            {
                OnDebugLine("Solving " + border.Cells.Count + " cell border");
                OnDebugLine("Attempting " + (1 << border.Cells.Count) + " combinations");

                border.ValidCombinations = FindValidBorderCellCombinations(map, border, undecidedCellsRemaining);

                OnDebugLine("Found " + border.ValidCombinations.Count + " valid combinations");
                foreach (var validCombination in border.ValidCombinations)
                {
                    OnDebugLine(validCombination.Where(x => x.Value == Verdict.HasMine).Select(x => x.Key.ToString()).Aggregate("", (x,n) => x + ";" + n));
                }
                if (border.ValidCombinations.Count == 0)
                {
                    // TODO Must be invalid map... Handle somehow
                }

                border.SmallestPossibleMineCount = border.ValidCombinations.Min(x => x.Count(y => y.Value == Verdict.HasMine));
            }

            if (map.RemainingMineCount.HasValue)
            {
                foreach (var border in borders)
                {
                    var guaranteedOtherCombinations = borders.Where(x => x != border).Sum(x => x.SmallestPossibleMineCount);
                    TrimValidCombinationsByMineCount(border, map.RemainingMineCount.Value, undecidedCellsRemaining, guaranteedOtherCombinations);
                }
            }

            commonBorder.ValidCombinations = GetCommonBorderValidCombinations(borders).ToList();

            if (map.RemainingMineCount.HasValue)
            {
                TrimValidCombinationsByMineCount(commonBorder, map.RemainingMineCount.Value, undecidedCellsRemaining, 0);
            }

            commonBorder.Probabilities = GetBorderProbabilities(commonBorder);

            var commonBorderPredictions = GetBorderPredictions(commonBorder);
            OnDebugLine("Found " + commonBorderPredictions.Count + " guaranteed moves.");
            if (commonBorderPredictions.Count == 0)
            {
                var lastRiskyPrediction = commonBorder.Probabilities.MinBy(x => x.Value);
                OnDebugLine("Guessing from a border with " + (1 - lastRiskyPrediction.Value) + " chance of success.");
                return new Dictionary<Coordinate, Verdict> { { lastRiskyPrediction.Key, Verdict.DoesntHaveMine } };
            }
            return commonBorderPredictions;
        }

        private IEnumerable<IDictionary<Coordinate, Verdict>> GetCommonBorderValidCombinations(IEnumerable<Border> borders)
        {
            var commorBorder = borders.Select(x => x.ValidCombinations).MultiCartesian(x => x.SelectMany(y => y).ToDictionary(y => y.Key, y => y.Value));
            return commorBorder;
        }

        private void TrimValidCombinationsByMineCount(Border border, int minesRemaining, int undecidedCellsRemaining, int minesElsewhere)
        {
            for (int i = 0; i < border.ValidCombinations.Count; i++)
            {
                var combination = border.ValidCombinations[i];
                var minePredictionCount = combination.Count(x => x.Value == Verdict.HasMine);
                if (minePredictionCount + minesElsewhere > minesRemaining)
                {
                    border.ValidCombinations.RemoveAt(i);
                    i--;
                    continue;
                }
                if (undecidedCellsRemaining == combination.Count && minePredictionCount != minesRemaining)
                {
                    border.ValidCombinations.RemoveAt(i);
                    i--;
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

        private Border GetCommonBorder(Map map)
        {
            var borderCells = map.AllCells.Where(x => IsCoordinateABorder(map, x)).ToList();
            var border = new Border(borderCells);
            return border;
        }

        private IEnumerable<Border> SeparateBorders(Border commonBorder, Map map)
        {
            var copy = new List<Cell>(commonBorder.Cells);
            //var map = new Map(copy);

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
                    var neighbors = map.GetNeighboursOf(cell).Where(x => x.Flag != CellFlag.HasMine && (cell.State == CellState.Filled || x.State == CellState.Filled));
                    if (cell.State == CellState.Filled)
                    {
                        currentCells.Add(cell);
                        copy.Remove(cell);
                    }
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

        private IList<IDictionary<Coordinate,Verdict>> FindValidBorderCellCombinations(Map map, Border border, int undecidedCellsRemaining)
        {
            var totalCombinations = 1 << border.Cells.Count;
            var validPredictions = new ConcurrentBag<IDictionary<Coordinate, Verdict>>();
            var emptyCells = map.AllCells.Where(x => x.State == CellState.Empty).ToList();
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
                var valid = IsPredictionValid(map, predictions, emptyCells, undecidedCellsRemaining);
                if (valid)
                {
                    validPredictions.Add(predictions);
                }
            });
            return validPredictions.ToList();
        }

        public bool IsPredictionValid(Map map, IDictionary<Coordinate, Verdict> predictions, IList<Cell> emptyCells, int undecidedCellsRemaining)
        {
            if (!CheckBorderMineCount(map, predictions, undecidedCellsRemaining))
            {
                // TODO: Is this still needed? Benchmark whether it helps or hurts
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

        private static bool CheckBorderMineCount(Map map, IDictionary<Coordinate, Verdict> predictions, int undecidedCellsRemaining)
        {
            if (map.RemainingMineCount.HasValue)
            {
                var minePredictionCount = predictions.Count(x => x.Value == Verdict.HasMine);
                if (minePredictionCount > map.RemainingMineCount)
                {
                    return false;
                }
                if (undecidedCellsRemaining == predictions.Count)
                {
                    if (minePredictionCount != map.RemainingMineCount)
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

        private static Dictionary<Coordinate, Verdict> GetBorderPredictions(Border border)
        {
            var commonVerdicts = new Dictionary<Coordinate, Verdict>();
            foreach (var cell in border.Cells)
            {
                var firstVerdict = border.ValidCombinations[0][cell.Coordinate];
                if (border.ValidCombinations.All(x => x[cell.Coordinate] == firstVerdict))
                {
                    commonVerdicts.Add(cell.Coordinate, firstVerdict);
                }
            }
            return commonVerdicts;
        }
    }
}