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
            IDictionary<Coordinate, decimal> _;
            return Solve(map, out _);
        }

        public IDictionary<Coordinate, Verdict> Solve(Map map, out IDictionary<Coordinate, decimal> probabilities)
        {
            map.BuildNeighbourCache();

            var filledCount = map.AllCells.Count(x => x.State == CellState.Filled);
            var flaggedCount = map.AllCells.Count(x => x.Flag == CellFlag.HasMine);
            var undecidedCellsRemaining = filledCount - flaggedCount;

            var commonBorder = GetCommonBorder(map);
            OnDebugLine("Common border calculated, found " + commonBorder.Cells.Count + " cells");
            var borders = SeparateBorders(commonBorder, map).ToList();
            if (borders.Count > 0)
            {
                var splitBordersStr = "(" + borders.Select(x => x.Cells.Count.ToString()).Aggregate((x, n) => x + ", " + n) + ")";
                OnDebugLine("Common border split into " + borders.Count + " separate borders " + splitBordersStr);
            }
            else
            {
                OnDebugLine("No borders found!");
            }

            foreach (var border in borders)
            {
                OnDebugLine("Solving " + border.Cells.Count + " cell border");
                OnDebugLine("Attempting " + (1 << border.Cells.Count) + " combinations");

                border.ValidCombinations = FindValidBorderCellCombinations(map, border, undecidedCellsRemaining);

                OnDebugLine("Found " + border.ValidCombinations.Count + " valid combinations");
                foreach (var validCombination in border.ValidCombinations)
                {
                    OnDebugLine(validCombination.Where(x => x.Value == Verdict.HasMine).Select(x => x.Key.ToString()).Aggregate("", (x, n) => x + ";" + n));
                }
                if (border.ValidCombinations.Count == 0)
                {
                    // TODO Must be invalid map... Handle somehow
                    probabilities = null;
                    return null;
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

            var nonBorderProbabilities = GetNonBorderProbabilitiesByMineCount(map, commonBorder);

            probabilities = commonBorder.Probabilities.Concat(nonBorderProbabilities).ToDictionary(x => x.Key, x => x.Value);

            var commonBorderPredictions = GetBorderPredictions(probabilities);
            OnDebugLine("Found " + commonBorderPredictions.Count + " guaranteed moves.");
            if (commonBorderPredictions.Count == 0)
            {
                var lastRiskyPrediction = commonBorder.Probabilities.MinBy(x => x.Value);
                OnDebugLine("Guessing from a border with " + (1 - lastRiskyPrediction.Value) + " chance of success.");
                return new Dictionary<Coordinate, Verdict> {{lastRiskyPrediction.Key, Verdict.DoesntHaveMine}};
            }
            return commonBorderPredictions;
        }

        private IDictionary<Coordinate, decimal> GetNonBorderProbabilitiesByMineCount(Map map, Border commonBorder)
        {
            var probabilities = new Dictionary<Coordinate, decimal>();
            if (!map.RemainingMineCount.HasValue)
            {
                return probabilities;
            }

            var anyCombinations = commonBorder.ValidCombinations.Count > 0;
            var combinationsMineCount = anyCombinations ? commonBorder.ValidCombinations[0].Count(x => x.Value == Verdict.HasMine) : 0;
            /*if (combinationsMineCount == 0)
            {
                return probabilities;
            }*/
            var sameMineCountForAllCombinations = !anyCombinations || commonBorder.ValidCombinations.All(x => x.Count(y => y.Value == Verdict.HasMine) == combinationsMineCount);
            if (!sameMineCountForAllCombinations)
            {
                return probabilities;
            }
            var commonBorderCoordinateSet = new HashSet<Coordinate>(commonBorder.Cells.Select(x => x.Coordinate));
            var nonBorderFilledCells = map.AllCells.Where(x => !commonBorderCoordinateSet.Contains(x.Coordinate) && x.State == CellState.Filled && x.Flag == CellFlag.None).ToList();
            if (combinationsMineCount == map.RemainingMineCount.Value)
            {
                foreach (var nonBorderFilledCell in nonBorderFilledCells)
                {
                    probabilities[nonBorderFilledCell.Coordinate] = 0;
                }
                return probabilities;
            }
            if (nonBorderFilledCells.Count == map.RemainingMineCount.Value - combinationsMineCount)
            {
                foreach (var nonBorderFilledCell in nonBorderFilledCells)
                {
                    probabilities[nonBorderFilledCell.Coordinate] = 1;
                }
                return probabilities;
            }
            return probabilities;
        }

        private IEnumerable<IDictionary<Coordinate, Verdict>> GetCommonBorderValidCombinations(IList<Border> borders)
        {
            if (borders.Count == 0)
            {
                return new List<IDictionary<Coordinate, Verdict>>();
            }
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
            var commonCoords = new HashSet<Coordinate>(commonBorder.Cells.Select(x => x.Coordinate));
            //var map = new Map(copy);

            var visited = new HashSet<Coordinate>();
            //var allCells = new List<IList<Cell>>();
            while (commonCoords.Count > 0)
            {
                var initialCoord = commonCoords.First();
                var coordQueue = new Queue<Coordinate>();
                var currentCells = new List<Cell>();
                coordQueue.Enqueue(initialCoord);
                while (coordQueue.Count > 0)
                {
                    var coord = coordQueue.Dequeue();
                    var cell = map.Cells[coord.X, coord.Y];
                    var neighbors = map.GetNeighboursOf(cell).Where(x => x.Flag != CellFlag.HasMine && (cell.State == CellState.Filled || x.State == CellState.Filled));
                    if (commonCoords.Contains(coord))
                    {
                        currentCells.Add(cell);
                        commonCoords.Remove(coord);
                    }
                    visited.Add(cell.Coordinate);
                    foreach (var neighbor in neighbors)
                    {
                        if (visited.Add(neighbor.Coordinate))
                        {
                            coordQueue.Enqueue(neighbor.Coordinate);
                        }
                    }
                }
                var border = new Border(currentCells);
                yield return border;
            }
        }

        private IList<IDictionary<Coordinate,Verdict>> FindValidBorderCellCombinations(Map map, Border border, int undecidedCellsRemaining)
        {
            var allRemainingCellsInBorder = undecidedCellsRemaining == border.Cells.Count;
            var totalCombinations = 1 << border.Cells.Count;
            var validPredictions = new ConcurrentBag<IDictionary<Coordinate, Verdict>>();
            var emptyCells = map.AllCells.Where(x => x.State == CellState.Empty).ToList();
            var combos = Enumerable.Range(0, totalCombinations);
            Parallel.ForEach(combos, combo =>
            {
                var bitsSet = SWAR(combo);
                if (map.RemainingMineCount.HasValue)
                {
                    if (bitsSet > map.RemainingMineCount.Value)
                    {
                        return;
                    }
                    
                    if (allRemainingCellsInBorder && bitsSet != map.RemainingMineCount)
                    {
                        return;
                    }
                }
                var predictions = new Dictionary<Coordinate, Verdict>(border.Cells.Count);
                for (var j = 0; j < border.Cells.Count; j++)
                {
                    var coord = border.Cells[j].Coordinate;
                    var hasMine = (combo & (1 << j)) > 0;
                    var verd = hasMine ? Verdict.HasMine : Verdict.DoesntHaveMine;
                    predictions[coord] = verd;
                }
                var valid = IsPredictionValid(map, predictions, emptyCells, undecidedCellsRemaining);
                if (valid)
                {
                    validPredictions.Add(predictions);
                }
            });
            return validPredictions.ToList();
        }

        private int SWAR(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        public bool IsPredictionValid(Map map, IDictionary<Coordinate, Verdict> predictions, IList<Cell> emptyCells, int undecidedCellsRemaining)
        {
            foreach (var cell in emptyCells)
            {
                var neighboursWithMine = 0;
                var neighboursWithoutMine = 0;
                var filledNeighbours = map.NeighbourCache[cell.Coordinate].ByState[CellState.Filled];
                bool foundUnknownCell = false;
                foreach (var neighbour in filledNeighbours)
                {
                    if (neighbour.Flag == CellFlag.HasMine)
                    {
                        neighboursWithMine++;
                    }
                    else
                    {
                        Verdict verdict;
                        var success = predictions.TryGetValue(neighbour.Coordinate, out verdict);
                        if (success)
                        {
                            if (verdict == Verdict.HasMine)
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

        private static IDictionary<Coordinate, Verdict> GetBorderPredictions(IDictionary<Coordinate, decimal> probabilities)
        {
            var commonVerdicts = new Dictionary<Coordinate, Verdict>();
            foreach (var probability in probabilities)
            {
                if (probability.Value == 0)
                {
                    commonVerdicts[probability.Key] = Verdict.DoesntHaveMine;
                }
                else if(probability.Value == 1)
                {
                    commonVerdicts[probability.Key] = Verdict.HasMine;
                }
            }
            return commonVerdicts;
        }
    }
}