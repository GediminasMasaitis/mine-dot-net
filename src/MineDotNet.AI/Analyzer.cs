using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MineDotNet.Common;

namespace MineDotNet.AI
{
    public class Analyzer
    {
        public event Action<string> Debug;
        public virtual void OnDebug(string s) => Debug?.Invoke(s);
        public virtual void OnDebugLine(string s) => OnDebug(s + Environment.NewLine);

        private Stopwatch sw { get; set; } = new Stopwatch();

        private void Tick()
        {
            sw.Start();
        }

        private double Tock()
        {
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        private string TockStr()
        {
            return Tock().ToString("#.##");
        }

        public IDictionary<Coordinate, Verdict> Solve(Map map)
        {
            OnDebugLine("Solving " + map.Width + "x" + map.Height + " map.");
            OnDebugLine("Attempting simple solver");
            Tick();
            var simpleResults = SolveSimple(map);
            var simpleTime = TockStr();
            if (simpleResults.Count > 0)
            {
                OnDebugLine("Simple solver succeeded, found " + simpleResults.Count + " results in " + simpleTime + " ms.");
                return simpleResults;
            }
            OnDebugLine("Simple solver failed in " + simpleTime + " ms.");
            OnDebugLine("Attempting complex solver");
            var complexResults = SolveComplex(map);
            return complexResults;
        }

        public IDictionary<Coordinate,Verdict> SolveSimple(Map map)
        {
            var verdicts = new Dictionary<Coordinate, Verdict>();
            SolveSimpleInner(map, verdicts, true);
            return verdicts;
        }

        private void SolveSimpleInner(Map map, IDictionary<Coordinate, Verdict> verdicts, bool recurseFurther)
        {
            while (true)
            {
                var initialVerdictCount = verdicts.Count;
                var hintedCells = map.AllCells.Where(x => x.Hint != 0);
                foreach (var cell in hintedCells)
                {
                    var cellNeighbours = map.GetNeighboursOf(cell);
                    var filledNeighbours = cellNeighbours.Where(x => x.State == CellState.Filled).ToList();
                    var markedNeighbours = filledNeighbours.Where(x => x.Flag == CellFlag.HasMine || (verdicts.ContainsKey(x.Coordinate) && verdicts[x.Coordinate] == Verdict.HasMine)).ToList();
                    if (filledNeighbours.Count == markedNeighbours.Count)
                    {
                        continue;
                    }
                    if (filledNeighbours.Count == cell.Hint)
                    {
                        var neighboursToFlag = filledNeighbours.Where(x => x.Flag != CellFlag.HasMine && !verdicts.ContainsKey(x.Coordinate));
                        foreach (var neighbour in neighboursToFlag)
                        {
                            verdicts.Add(neighbour.Coordinate, Verdict.HasMine);
                        }
                    }
                    if (markedNeighbours.Count == cell.Hint)
                    {
                        var unmarkedNeighbours = filledNeighbours.Where(x => x.Flag != CellFlag.HasMine);
                        var neighboursToClick = unmarkedNeighbours.Where(x => !verdicts.ContainsKey(x.Coordinate));
                        foreach (var neighbour in neighboursToClick)
                        {
                            verdicts.Add(neighbour.Coordinate, Verdict.DoesntHaveMine);
                        }
                    }
                }

                if (!recurseFurther || verdicts.Count <= initialVerdictCount)
                {
                    return;
                }
            }
        }

        public IDictionary<Coordinate,Verdict> SolveComplex(Map map)
        {
            map.BuildNeighbourCache();
            var allProbabilities = new Dictionary<Coordinate, decimal>();

            var commonBoundry = GetBoundryCells(map);
            OnDebugLine("Common boundary calculated, found " + commonBoundry.Count + " cells");
            var splitBoundaries = SeggregateBoundries(commonBoundry);
            var splitBoundariesStr = "(" + splitBoundaries.Select(x=>x.Count.ToString()).Aggregate((x, n) => x + ", " + n) + ")";
            OnDebugLine("Common boundary split into " + splitBoundaries.Count + " separate boundaries " + splitBoundariesStr);
            foreach (var splitBoundary in splitBoundaries)
            {
                OnDebugLine("Solving " + splitBoundary.Count + " cell boundary");
                OnDebugLine("Attempting " + (1 << splitBoundary.Count) + " combinations");
                var validCombinations = GetValidBoundaryCombinations(map, splitBoundary);
                OnDebugLine("Found " + validCombinations.Count + " valid combinations");

                if (validCombinations.Count == 0)
                {
                    // TODO Must be invalid map... Handle somehow
                }
                var probabilities = GetBoundaryProbabilities(splitBoundary, validCombinations);
                allProbabilities.AddRange(probabilities);
            }

            var commonBoundaryPredictions = GetCommonBoundaryPredictions(allProbabilities);
            OnDebugLine("Complex solver found " + commonBoundaryPredictions.Count + " guaranteed moves.");
            if (commonBoundaryPredictions.Count == 0)
            {
                var lastRiskyPrediction = allProbabilities.MinBy(x => x.Value);
                OnDebugLine("Guessing from a boundary with " + (1 - lastRiskyPrediction.Value) + " chance of success.");
                return new Dictionary<Coordinate, Verdict> { { lastRiskyPrediction.Key, Verdict.DoesntHaveMine } };
            }
            return commonBoundaryPredictions;
        }

        private bool IsCoordinateABoundry(Map map, Cell cell)
        {
            if (cell.State == CellState.Empty || cell.Flag == CellFlag.HasMine)
            {
                return false;
            }
            var neighbours = map.GetNeighboursOf(cell);
            var hasOpenedNeighbour = neighbours.Any(x => x.State == CellState.Empty);
            return hasOpenedNeighbour;
        }

        private IList<Cell> GetBoundryCells(Map map)
        {
            var boundryCells = map.AllCells.Where(x => IsCoordinateABoundry(map, x)).ToList();
            return boundryCells;
        }

        private IList<IList<Cell>> SeggregateBoundries(IEnumerable<Cell> commonBoundry)
        {
            var copy = new List<Cell>(commonBoundry);
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

        private IList<IDictionary<Coordinate, Verdict>> GetValidBoundaryCombinations(Map map, IList<Cell> splitBoundary)
        {
            var totalCombinations = 1 << splitBoundary.Count;
            var validPredictions = new ConcurrentBag<IDictionary<Coordinate, Verdict>>();
            var emptyCells = map.AllCells.Where(x => x.State == CellState.Empty).ToList();
            var filledCount = map.AllCells.Count(x => x.State == CellState.Filled);
            var flaggedCount = map.AllCells.Count(x => x.Flag == CellFlag.HasMine);
            var combos = Enumerable.Range(0, totalCombinations);
            Parallel.ForEach(combos, combo =>
            {
                var binaryStr = Convert.ToString(combo, 2).PadLeft(splitBoundary.Count, '0');
                var binaries = binaryStr.Select(x => x == '1').ToList();
                var predictions = new Dictionary<Coordinate, Verdict>();
                for (var j = 0; j < splitBoundary.Count; j++)
                {
                    var coord = splitBoundary[j].Coordinate;
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
            if (!CheckBoundaryMineCount(map, predictions, filledCount, flaggedCount))
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

        private static bool CheckBoundaryMineCount(Map map, IDictionary<Coordinate, Verdict> predictions, int filledCount, int flaggedCount)
        {
            // TODO: Doesn't work fully. If the map contains multiple borders, it will check one border at a time and prediction counts will be wrong. For end-game cases there should be an additional check from all borders using a cartesian product from all borders...
            if (map.RemainingMineCount.HasValue)
            {
                var minePredictionCount = predictions.Count(x => x.Value == Verdict.HasMine);
                if (flaggedCount + minePredictionCount > map.RemainingMineCount)
                {
                    return false;
                }
                if (filledCount == flaggedCount + predictions.Count)
                {
                    if (flaggedCount + minePredictionCount != map.RemainingMineCount)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static IDictionary<Coordinate, decimal> GetBoundaryProbabilities(IList<Cell> splitBoundary, IList<IDictionary<Coordinate, Verdict>> validCombinations)
        {
            var probabilities = new Dictionary<Coordinate, decimal>();
            foreach (var cell in splitBoundary)
            {
                var mineInCount = validCombinations.Count(x => x[cell.Coordinate] == Verdict.HasMine);
                var probability = (decimal)mineInCount / validCombinations.Count;
                probabilities.Add(cell.Coordinate, probability);
            }
            return probabilities;
        }

        private static Dictionary<Coordinate, Verdict> GetCommonBoundaryPredictions(IDictionary<Coordinate, decimal> probabilities)
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

        private static Dictionary<Coordinate, Verdict> GetCommonBoundaryPredictions(IList<Cell> splitBoundary, IList<IDictionary<Coordinate, Verdict>> validCombinations)
        {
            var commonVerdicts = new Dictionary<Coordinate, Verdict>();
            foreach (var cell in splitBoundary)
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