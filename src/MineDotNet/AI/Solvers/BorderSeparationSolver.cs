﻿using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public class BorderSeparationSolver : ISolver
    {
        public bool OnlyTrivialSolving { get; set; }

        public bool IgnoreMineCountCompletely { get; set; }
        public bool SolveByMineCount { get; set; }
        public bool SolveNonBorderCells { get; set; }

        public int PartialBorderSolveFrom { get; set; }
        public int GiveUpFrom { get; set; }
        public int MaxPartialBorderSize { get; set; }
        public bool SetPartiallyCalculatedProbabilities { get; set; }

        public BorderSeparationSolver()
        {
            OnlyTrivialSolving = false;

            IgnoreMineCountCompletely = false;
            SolveByMineCount = true;
            SolveNonBorderCells = true;

            PartialBorderSolveFrom = 22;
            GiveUpFrom = 25;
            MaxPartialBorderSize = 16;
            SetPartiallyCalculatedProbabilities = true;
        }

#if DEBUG
        public event Action<string> Debug;
#endif

        private void OnDebug(string str)
        {
#if DEBUG
            Debug?.Invoke(str);
#endif
        }

        private void OnDebugLine(string str)
        {
#if DEBUG
            OnDebug(str + Environment.NewLine);
#endif
        }

        public IDictionary<Coordinate, SolverResult> Solve(IMap givenMap)
        {
            // We prepare some intial data we will work with.
            // It's important to deep copy the cells, in order to not modify the original map.
            var map = new BorderSeparationSolverMap(givenMap);
            if (IgnoreMineCountCompletely)
            {
                map.RemainingMineCount = null;
            }
            map.BuildNeighbourCache();
            var allProbabilities = new Dictionary<Coordinate, decimal>();
            var allVerdicts = new Dictionary<Coordinate, bool>();

            // We first attempt trivial solving.
            // If it's all the user wants, we return immediately.
            SolveTrivial(map, allVerdicts);
            if (OnlyTrivialSolving)
            {
                return allVerdicts.ToDictionary(x => x.Key, x => new SolverResult(x.Key, x.Value ? 1 : 0, x.Value));
            }

            // We find a set of all border cells - the common border.
            var commonBorder = FindCommonBorder(map);
            OnDebugLine($"Common border calculated, found {commonBorder.Cells.Count} cells");

            // We separate the common border into multiple, non-connecting borders, which can be solved independently.
            var borders = SeparateBorders(commonBorder, map).OrderBy(x => x.Cells.Count).ToList();
            OnDebugLine($"Common border split into {borders.Count} separate borders.");

            // We iterate over each border, and attempt to solve it,
            // then copy the border's verdicts and probabilities
            foreach (var border in borders)
            {
                SolveBorder(border, map, true);
                foreach (var borderVerdict in border.Verdicts)
                {
                    allVerdicts[borderVerdict.Key] = borderVerdict.Value;
                }
                foreach (var probability in border.Probabilities)
                {
                    allProbabilities[probability.Key] = probability.Value;
                }
            }

            // If requested, we do additional solving based on the remaining mine count
            if (SolveByMineCount)
            {
                SolveMapByMineCounts(map, commonBorder, borders, allProbabilities, allVerdicts);
            }

            // We get the final results, and return
            var finalResults = GetFinalResults(allProbabilities, allVerdicts);
            OnDebugLine("Found " + allVerdicts.Count + " guaranteed moves.");
            return finalResults;
        }

        private void SolveTrivial(BorderSeparationSolverMap map, IDictionary<Coordinate, bool> allVerdicts)
        {
            while (true)
            {
                var currentRoundVerdicts = new Dictionary<Coordinate, bool>();
                var emptyCells = map.AllCells.Where(x => x.State == CellState.Empty);
                foreach (var cell in emptyCells)
                {
                    var neighbourEntry = map.NeighbourCache[cell.Coordinate];
                    var filledNeighbours = neighbourEntry.ByState[CellState.Filled];
                    var markedNeighbours = neighbourEntry.ByFlag[CellFlag.HasMine];
                    if (filledNeighbours.Count == markedNeighbours.Count)
                    {
                        continue;
                    }
                    if (filledNeighbours.Count == cell.Hint)
                    {
                        var neighboursToFlag = filledNeighbours.Where(x => x.Flag != CellFlag.HasMine && !allVerdicts.ContainsKey(x.Coordinate));
                        foreach (var neighbour in neighboursToFlag)
                        {
                            currentRoundVerdicts[neighbour.Coordinate] = true;
                            allVerdicts[neighbour.Coordinate] = true;
                        }
                    }
                    if (markedNeighbours.Count == cell.Hint)
                    {
                        var neighboursToClick = filledNeighbours.Where(x => x.Flag != CellFlag.HasMine && !allVerdicts.ContainsKey(x.Coordinate));
                        foreach (var neighbour in neighboursToClick)
                        {
                            currentRoundVerdicts[neighbour.Coordinate] = false;
                            allVerdicts[neighbour.Coordinate] = false;
                        }
                    }
                }
                if (currentRoundVerdicts.Count == 0)
                {
                    break;
                }
                SetCellsByVerdicts(map, currentRoundVerdicts);
            }
        }

        private void SolveMapByMineCounts(BorderSeparationSolverMap map, Border commonBorder, List<Border> borders, IDictionary<Coordinate, decimal> allProbabilities, IDictionary<Coordinate, bool> allVerdicts)
        {
            if (!map.RemainingMineCount.HasValue)
            {
                return;
            }

            // We make two HashSets of the common border coordinates.
            // One which will stay unmodified - to be able to identify border cells without recalculating,
            // another which will get unfit coordinates removed.
            var originalCommonBorderCoords = new HashSet<Coordinate>(commonBorder.Cells.Select(x => x.Coordinate));
            var commonBorderCoords = new HashSet<Coordinate>(originalCommonBorderCoords);
            commonBorderCoords.ExceptWith(allVerdicts.Keys);

            var fullySolvedBorders = borders.Where(x => x.SolvedFully).ToList();
            var unsolvedBorders = borders.Where(x => !x.SolvedFully).ToList();

            // If the border wasn't solved fully, we can't do later algorithms,
            // since we lack a list of valid combinations. So we remove the cells from the common border.
            foreach (var unsolvedBorder in unsolvedBorders)
            {
                foreach (var unsolvedBorderCell in unsolvedBorder.Cells)
                {
                    commonBorderCoords.Remove(unsolvedBorderCell.Coordinate);
                }
            }

            foreach (var border in fullySolvedBorders)
            {
                var otherBorders = fullySolvedBorders.Where(x => x != border).ToList();
                var otherBorderSizeSum = otherBorders.Sum(x => x.Cells.Count);
                var guaranteedMines = otherBorders.Sum(x => x.MinMineCount);
                var guaranteedEmpty = otherBorderSizeSum - guaranteedMines;
                TrimValidCombinationsByMineCount(border, map.RemainingMineCount.Value, map.UndecidedCount, guaranteedMines, guaranteedEmpty);
            }

            var bordersWithExactMineCount = fullySolvedBorders.Where(x => x.MinMineCount == x.MaxMineCount).ToList();
            var bordersWithVariableMineCount = fullySolvedBorders.Where(x => x.MinMineCount != x.MaxMineCount).ToList();

            foreach (var border in bordersWithExactMineCount)
            {
                foreach (var cell in border.Cells)
                {
                    commonBorderCoords.Remove(cell.Coordinate);
                }
            }

            commonBorder.Cells = commonBorder.Cells.Where(x => commonBorderCoords.Contains(x.Coordinate)).ToList();
            commonBorder.ValidCombinations = GetCommonBorderValidCombinations(bordersWithVariableMineCount);

            var exactMineCount = bordersWithExactMineCount.Sum(x => x.MaxMineCount);
            var exactBorderSize = bordersWithExactMineCount.Sum(x => x.Cells.Count);
            var exactNonMineCount = exactBorderSize - exactMineCount;
            TrimValidCombinationsByMineCount(commonBorder, map.RemainingMineCount.Value, map.UndecidedCount, exactMineCount, exactNonMineCount);

            commonBorder.Probabilities = GetBorderProbabilities(commonBorder);
            foreach (var probability in commonBorder.Probabilities)
            {
                allProbabilities[probability.Key] = probability.Value;
            }
            if (SolveNonBorderCells)
            {
                var nonBorderProbabilities = GetNonBorderProbabilitiesByMineCount(map, allProbabilities, originalCommonBorderCoords);
                foreach (var probability in nonBorderProbabilities)
                {
                    allProbabilities[probability.Key] = probability.Value;
                }
            }
            var verdictsFromProbabilities = GetVerdictsFromProbabilities(allProbabilities);
            foreach (var verdict in verdictsFromProbabilities)
            {
                allVerdicts[verdict.Key] = verdict.Value;
            }
        }

        private void SolveBorder(Border border, BorderSeparationSolverMap map, bool allowPartialBorderSolving)
        {
            OnDebugLine("Solving " + border.Cells.Count + " cell border");
            if (allowPartialBorderSolving && border.Cells.Count > PartialBorderSolveFrom)
            {
                TrySolveBorderByPartialBorders(border, map);
                if (border.SolvedFully)
                {
                    return;
                }
            }

            if (border.Cells.Count > GiveUpFrom)
            {
                border.SolvedFully = false;
                return;
            }

            border.ValidCombinations = FindValidBorderCellCombinations(map, border);

            OnDebugLine("Found " + border.ValidCombinations.Count + " valid combinations");
            if (border.ValidCombinations.Count == 0)
            {
                // TODO Must be invalid map... Handle somehow
            }

            border.MinMineCount = border.ValidCombinations.Min(x => x.Count(y => y.Value));
            border.MaxMineCount = border.ValidCombinations.Max(x => x.Count(y => y.Value));
            border.Probabilities = GetBorderProbabilities(border);
            border.Verdicts = GetVerdictsFromProbabilities(border.Probabilities);
            SetCellsByVerdicts(map, border.Verdicts);
            foreach (var wholeBorderResult in border.Verdicts)
            {
                border.Probabilities.Remove(wholeBorderResult.Key);
                var cell = map[wholeBorderResult.Key];
                border.Cells.Remove(cell);
                foreach (var validCombination in border.ValidCombinations)
                {
                    validCombination.Remove(wholeBorderResult.Key);
                }
            }
            border.SolvedFully = true;
        }

        private void TrySolveBorderByPartialBorders(Border border, BorderSeparationSolverMap map)
        {
            OnDebugLine($"Attempting to solve border via partial borders with max partial border size {MaxPartialBorderSize}");
            IList<PartialBorderData> checkedPartialBorders = new List<PartialBorderData>();
            var verdictsFoundByPartialBorders = 0;
            for (var i = 0; i < border.Cells.Count; i++)
            {
                var targetCoordinate = border.Cells[i].Coordinate;
                var partialBorderData = GetPartialBorder(border, map, targetCoordinate);
                var previousBorderData = checkedPartialBorders.FirstOrDefault(x => x.PartialBorderCoordinates.IsSupersetOf(partialBorderData.PartialBorderCoordinates));
                if (previousBorderData != null)
                {
                    decimal probability;
                    if (previousBorderData.PartialBorder.Probabilities.TryGetValue(targetCoordinate, out probability))
                    {
                        border.Probabilities[targetCoordinate] = probability;
                    }
                    continue;
                }

                partialBorderData.PartialMap.BuildNeighbourCache();
                var partialBorder = partialBorderData.PartialBorder;
                SolveBorder(partialBorder, partialBorderData.PartialMap, false);
                SetCellsByVerdicts(map, partialBorder.Verdicts);
                foreach (var verdict in partialBorder.Verdicts)
                {
                    var cell = map[verdict.Key];
                    border.Verdicts[verdict.Key] = verdict.Value;
                    var cellIndex = border.Cells.IndexOf(cell);
                    if (cellIndex <= i)
                    {
                        i--;
                    }
                    border.Cells.RemoveAt(cellIndex);
                }
                verdictsFoundByPartialBorders += partialBorder.Verdicts.Count;
                checkedPartialBorders.Add(partialBorderData);
                if (SetPartiallyCalculatedProbabilities && !partialBorder.Verdicts.ContainsKey(targetCoordinate))
                {
                    border.Probabilities[targetCoordinate] = partialBorder.Probabilities[targetCoordinate];
                }
            }

            if (verdictsFoundByPartialBorders > 0)
            {
                TrySolveBorderByReseparating(border, map);
            }
        }

        private void TrySolveBorderByReseparating(Border border, BorderSeparationSolverMap map)
        {
            var resplitBorders = SeparateBorders(border, map).ToList();
            if (resplitBorders.Count > 1)
            {
                foreach (var resplitBorder in resplitBorders)
                {
                    SolveBorder(resplitBorder, map, false);
                    foreach (var resplitBorderVerdict in resplitBorder.Verdicts)
                    {
                        border.Verdicts[resplitBorderVerdict.Key] = resplitBorderVerdict.Value;
                    }
                    if (resplitBorder.SolvedFully)
                    {
                        foreach (var probability in resplitBorder.Probabilities)
                        {
                            border.Probabilities[probability.Key] = probability.Value;
                        }
                    }
                }
                if (resplitBorders.All(x => x.SolvedFully))
                {
                    border.ValidCombinations = GetCommonBorderValidCombinations(resplitBorders);
                    border.SolvedFully = true;
                }
            }
        }

        private void SetCellsByVerdicts(BorderSeparationSolverMap map, IDictionary<Coordinate, bool> verdicts)
        {
            var coordsToUpdate = new HashSet<Coordinate>();
            foreach (var result in verdicts)
            {
                var cell = map[result.Key];
                switch (result.Value)
                {
                    case true:
                        cell.Flag = CellFlag.HasMine;
                        map.FlaggedCount++;
                        if (map.RemainingMineCount.HasValue)
                        {
                            map.RemainingMineCount--;
                        }
                        break;
                    case false:
                        cell.State = CellState.Wall;
                        map.FilledCount--;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(result));
                }
                var neighbours = map.NeighbourCache[result.Key].AllNeighbours.Select(x => x.Coordinate);
                coordsToUpdate.UnionWith(neighbours);
            }
            if (coordsToUpdate.Count > 0)
            {
                UpdateMapCache(map, coordsToUpdate);
            }
        }

        private void UpdateMapCache(IMap map, IEnumerable<Coordinate> updateForCoordinates)
        {
            foreach (var coordinate in updateForCoordinates)
            {
                // We assume that we will never update a wall into something,
                // so it's safe to use the outdated neighbour cache.
                var entry = map.NeighbourCache[coordinate];

                // All we have to do is remove the entries with a wall, and update the by-x lists.
                entry.AllNeighbours = entry.AllNeighbours.Where(x => x.State != CellState.Wall).ToList();
                var allStates = entry.ByState.Keys.ToList();
                foreach (var cellState in allStates)
                {
                    entry.ByState[cellState] = entry.AllNeighbours.Where(x => x.State == cellState).ToList();
                }
                var allFlags = entry.ByFlag.Keys.ToList();
                foreach (var cellFlag in allFlags)
                {
                    entry.ByFlag[cellFlag] = entry.AllNeighbours.Where(x => x.Flag == cellFlag).ToList();
                }
            }
        }

        private IDictionary<Coordinate, decimal> GetNonBorderProbabilitiesByMineCount(IMap map, IDictionary<Coordinate, decimal> commonBorderProbabilities, HashSet<Coordinate> originalCommonBorder)
        {
            var probabilities = new Dictionary<Coordinate, decimal>();
            if (!map.RemainingMineCount.HasValue)
            {
                return probabilities;
            }

            //var combinationsMineCount = commonBorder.ValidCombinations.Count > 0 ? commonBorder.ValidCombinations.Sum(x => x.Count(y => y.Value == Verdict.HasMine))/(decimal) commonBorder.ValidCombinations.Count : 0;
            //var commonBorderCoordinateSet = new HashSet<Coordinate>(commonBorder.Cells.Select(x => x.Coordinate));
            var probabilitySum = commonBorderProbabilities.Sum(x => x.Value);
            var combinationsMineCount = Convert.ToInt32(probabilitySum*1000000)/(decimal)1000000;
            var nonBorderUndecidedCells = map.AllCells.Where(x => !originalCommonBorder.Contains(x.Coordinate) && x.State == CellState.Filled && x.Flag == CellFlag.None).ToList();
            if (nonBorderUndecidedCells.Count == 0)
            {
                return probabilities;
            }
            var nonBorderProbability = (map.RemainingMineCount.Value - combinationsMineCount)/nonBorderUndecidedCells.Count;
            foreach (var nonBorderFilledCell in nonBorderUndecidedCells)
            {
                probabilities[nonBorderFilledCell.Coordinate] = nonBorderProbability;
            }

            return probabilities;
        }

        private IList<IDictionary<Coordinate, bool>> GetCommonBorderValidCombinations(ICollection<Border> borders)
        {
            if (borders.Count == 0)
            {
                return new List<IDictionary<Coordinate, bool>>();
            }
            var combinationCombinations = borders.Select(x => x.ValidCombinations);
            var combinationsCartesianProduct = combinationCombinations.MultiCartesian(x => (IDictionary<Coordinate,bool>)x.SelectMany(y => y).ToDictionary(y => y.Key, y => y.Value)).ToList();
            return combinationsCartesianProduct;
        }

        private void TrimValidCombinationsByMineCount(Border border, int minesRemaining, int undecidedCellsRemaining, int minesElsewhere, int nonMineCountElsewhere)
        {
            for (var i = 0; i < border.ValidCombinations.Count; i++)
            {
                var combination = border.ValidCombinations[i];
                var minePredictionCount = combination.Count(x => x.Value);
                if (minePredictionCount + minesElsewhere > minesRemaining)
                {
                    border.ValidCombinations.RemoveAt(i);
                    i--;
                    continue;
                }
                if (minesRemaining - minePredictionCount > undecidedCellsRemaining - combination.Count - nonMineCountElsewhere)
                {
                    border.ValidCombinations.RemoveAt(i);
                    i--;
                    continue;
                }
                //if (undecidedCellsRemaining == combination.Count && minePredictionCount != minesRemaining)
                //{
                //    border.ValidCombinations.RemoveAt(i);
                //    i--;
                //}
            }
        }

        private bool IsCoordinateABorder(IMap map, Cell cell)
        {
            if (cell.State != CellState.Filled || cell.Flag == CellFlag.HasMine)
            {
                return false;
            }
            var neighbours = map.NeighbourCache[cell.Coordinate].ByState[CellState.Empty];
            var hasOpenedNeighbour = neighbours.Count > 0;
            return hasOpenedNeighbour;
        }

        private Border FindCommonBorder(IMap map)
        {
            var borderCells = map.AllCells.Where(x => IsCoordinateABorder(map, x)).ToList();
            var border = new Border(borderCells);
            return border;
        }

        private PartialBorderData GetPartialBorder(Border border, BorderSeparationSolverMap map, Coordinate targetCoordinate)
        {
            Border partialBorder = null;
            BorderSeparationSolverMap partialMap = null;
            var partialBorderCells = new List<Cell>();
            var partialBorderSequence = GetPartialBorderCellSequence(border, map, targetCoordinate);
            foreach (var cell in partialBorderSequence)
            {
                partialBorderCells.Add(cell);
                if (partialBorderCells.Count < MaxPartialBorderSize)
                {
                    continue;
                }
                var partialBorderCandidate = new Border(partialBorderCells);
                var partialMapCandidate = CalculatePartialMapAndTrimPartialBorder(partialBorderCandidate, map);
                if (partialBorderCandidate.Cells.Count > MaxPartialBorderSize)
                {
                    break;
                }
                partialBorder = partialBorderCandidate;
                partialMap = partialMapCandidate;
            }
            var coordSet = new HashSet<Coordinate>(partialBorder.Cells.Select(x => x.Coordinate));
            var partialBorderData = new PartialBorderData(coordSet, partialMap, partialBorder);
            return partialBorderData;
        }

        private BorderSeparationSolverMap CalculatePartialMapAndTrimPartialBorder(Border border, IMap parentMap)
        {
            var borderCoordinateSet = new HashSet<Coordinate>(border.Cells.Select(x => x.Coordinate));
            var allSurroundingEmpty = borderCoordinateSet.SelectMany(x => parentMap.NeighbourCache[x].ByState[CellState.Empty]).Distinct();
            var onlyInfluencingBorder = allSurroundingEmpty.Where(x => parentMap.NeighbourCache[x.Coordinate].ByState[CellState.Filled].All(y => borderCoordinateSet.Contains(y.Coordinate))).ToList();
            var onlyInflucencingBorderSet = new HashSet<Coordinate>(onlyInfluencingBorder.Select(x => x.Coordinate));
            var newNonBorderCells = border.Cells.Where(x => parentMap.NeighbourCache[x.Coordinate].ByState[CellState.Empty].Any(y => onlyInflucencingBorderSet.Contains(y.Coordinate))).ToList();
            border.Cells = newNonBorderCells;
            if (border.Cells.Count == 0)
            {
                return null;
            }
            var allCoordinateSet = new HashSet<Coordinate>(border.Cells.Select(x => x.Coordinate).Concat(onlyInflucencingBorderSet));
            var newWidth = allCoordinateSet.Max(x => x.X) + 1;
            var newHeight = allCoordinateSet.Max(x => x.Y) + 1;
            var partialMap = new Map(newWidth, newHeight, null, true, CellState.Wall);
            foreach (var cell in border.Cells)
            {
                partialMap[cell.Coordinate] = cell;
            }
            foreach (var cell in onlyInfluencingBorder)
            {
                partialMap[cell.Coordinate] = cell;
            }
            var solverMap = new BorderSeparationSolverMap(partialMap);
            return solverMap;
        }

        private IEnumerable<Cell> GetPartialBorderCellSequence(Border commonBorder, IMap map, Coordinate targetCoordinate)
        {
            var commonCoords = new HashSet<Coordinate>(commonBorder.Cells.Select(x => x.Coordinate));
            var coordQueue = new Queue<Coordinate>();
            coordQueue.Enqueue(targetCoordinate);
            commonCoords.Remove(targetCoordinate);
            while (coordQueue.Count > 0)
            {
                var coord = coordQueue.Dequeue();
                var cell = map[coord];
                yield return cell;
                var neighbors = map.NeighbourCache[coord].ByState[CellState.Filled].Where(x => commonCoords.Contains(x.Coordinate));
                foreach (var neighbor in neighbors)
                {
                    commonCoords.Remove(neighbor.Coordinate);
                    coordQueue.Enqueue(neighbor.Coordinate);
                }
            }
        }

        private IEnumerable<Border> SeparateBorders(Border commonBorder, IMap map)
        {
            var commonCoords = new HashSet<Coordinate>(commonBorder.Cells.Select(x => x.Coordinate));
            var visited = new HashSet<Coordinate>();
            while (commonCoords.Count > 0)
            {
                var initialCoord = commonCoords.First();
                var coordQueue = new Queue<Coordinate>();
                var currentCells = new List<Cell>();
                coordQueue.Enqueue(initialCoord);
                while (coordQueue.Count > 0)
                {
                    var coord = coordQueue.Dequeue();
                    var cell = map[coord];
                    if (commonCoords.Contains(coord))
                    {
                        currentCells.Add(cell);
                        commonCoords.Remove(coord);
                    }
                    visited.Add(cell.Coordinate);
                    var unflaggedNeighbours = map.NeighbourCache[coord].ByFlag[CellFlag.None];
                    var neighbors = unflaggedNeighbours.Where(x => (cell.State == CellState.Filled && x.State == CellState.Empty) || (cell.State == CellState.Empty && x.State == CellState.Filled));
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

        private IList<IDictionary<Coordinate, bool>> FindValidBorderCellCombinations(BorderSeparationSolverMap map, Border border)
        {
            var borderLength = border.Cells.Count;
            const int maxSize = 31;
            if (borderLength > maxSize)
            {
                throw new InvalidDataException($"Border with {borderLength} cells is too large, maximum {maxSize} cells allowed");
            }
            var totalCombinations = 1 << borderLength;
            OnDebugLine("Attempting " + (1 << border.Cells.Count) + " combinations");
            var allRemainingCellsInBorder = map.UndecidedCount == borderLength;
            var validPredictions = new ConcurrentBag<IDictionary<Coordinate, bool>>();
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
                var predictions = new Dictionary<Coordinate, bool>(borderLength);
                for (var j = 0; j < borderLength; j++)
                {
                    var coord = border.Cells[j].Coordinate;
                    var hasMine = (combo & (1 << j)) > 0;
                    predictions[coord] = hasMine;
                }
                var valid = IsPredictionValid(map, predictions, emptyCells);
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
            return (((i + (i >> 4)) & 0x0F0F0F0F)*0x01010101) >> 24;
        }

        public bool IsPredictionValid(IMap map, IDictionary<Coordinate, bool> predictions, IList<Cell> emptyCells)
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
                        bool verdict;
                        var success = predictions.TryGetValue(neighbour.Coordinate, out verdict);
                        if (success)
                        {
                            if (verdict)
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

        private IDictionary<Coordinate, decimal> GetBorderProbabilities(Border border)
        {
            var probabilities = new Dictionary<Coordinate, decimal>();
            if (border.ValidCombinations.Count == 0)
            {
                return probabilities;
            }
            foreach (var cell in border.Cells)
            {
                var mineInCount = border.ValidCombinations.Count(x => x[cell.Coordinate]);
                var probability = (decimal) mineInCount/border.ValidCombinations.Count;
                probabilities.Add(cell.Coordinate, probability);
            }
            return probabilities;
        }

        private IDictionary<Coordinate, bool> GetVerdictsFromProbabilities(IDictionary<Coordinate, decimal> probabilities)
        {
            var verdicts = new Dictionary<Coordinate, bool>();
            foreach (var probability in probabilities)
            {
                bool verdict;
                if (probability.Value == 0)
                {
                    verdict = false;
                }
                else if (probability.Value == 1)
                {
                    verdict = true;
                }
                else
                {
                    continue;
                }
                verdicts[probability.Key] = verdict;
            }
            return verdicts;
        }

        private IDictionary<Coordinate, SolverResult> GetFinalResults(IDictionary<Coordinate, decimal> probabilities, IDictionary<Coordinate, bool> verdicts)
        {
            var results = new Dictionary<Coordinate, SolverResult>();
            foreach (var probability in probabilities)
            {
                results[probability.Key] = new SolverResult(probability.Key, probability.Value, null);
            }
            foreach (var verdict in verdicts)
            {
                results[verdict.Key] = new SolverResult(verdict.Key, verdict.Value ? 1 : 0, verdict.Value);
            }
            return results;
        }
    }
}