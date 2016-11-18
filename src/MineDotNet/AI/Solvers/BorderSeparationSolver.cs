using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MineDotNet.Common;
using MineDotNet.Etc;

namespace MineDotNet.AI.Solvers
{
    public class BorderSeparationSolver : ISolver
    {
        public BorderSeparationSolverSettings Settings { get; set; }

        public BorderSeparationSolver(BorderSeparationSolverSettings settings = null)
        {
            Settings = settings ?? new BorderSeparationSolverSettings();
        }

        public IDictionary<Coordinate, SolverResult> Solve(IMap givenMap)
        {
            // We prepare some intial data we will work with.
            // It's important to deep copy the cells, in order to not modify the original map.
            var map = new BorderSeparationSolverMap(givenMap);
            if (Settings.IgnoreMineCountCompletely)
            {
                map.RemainingMineCount = null;
            }
            map.BuildNeighbourCache();
            var allProbabilities = new Dictionary<Coordinate, double>();
            var allVerdicts = new Dictionary<Coordinate, bool>();
            var borders = new List<Border>();

            // We first attempt trivial solving.
            // If it's all the user wants, we return immediately.
            //Settings.SolveGaussian = false;

            if (Settings.SolveTrivial)
            {
                SolveTrivial(map, allVerdicts);
                if (Settings.StopAfterTrivialSolving || ShouldStopSolving(allVerdicts))
                {
                    return GetFinalResults(null, allVerdicts);
                }
            }

            if (Settings.SolveGaussian)
            {
                SolveGaussian(map, allVerdicts);
                if (Settings.StopAfterGaussianSolving || ShouldStopSolving(allVerdicts))
                {
                    return GetFinalResults(null, allVerdicts);
                }
            }

            // We find a set of all border cells - the common border.
            var commonBorder = FindCommonBorder(map);

            // We separate the common border into multiple, non-connecting borders, which can be solved independently.
            var originalBorderSequence = SeparateBorders(commonBorder, map).OrderBy(x => x.Cells.Count);

            // We iterate over each border, and attempt to solve it,
            // then copy the border's verdicts and probabilities
            foreach (var border in originalBorderSequence)
            {
                var newBorders = SolveBorder(border, map, true);
                borders.AddRange(newBorders);
                foreach (var borderVerdict in border.Verdicts)
                {
                    allVerdicts[borderVerdict.Key] = borderVerdict.Value;
                }
                foreach (var probability in border.Probabilities)
                {
                    allProbabilities[probability.Key] = probability.Value;
                }
                if (ShouldStopSolving(allVerdicts))
                {
                    return GetFinalResults(null, allVerdicts);
                }
            }

            var allHintProbabilities = new Dictionary<Coordinate, IDictionary<int, double>>();
            foreach (var cell in map.AllCells)
            {
                var hints = Enumerable.Range(0, 9).ToDictionary(x => x, x => 0d);
                hints[0] = 1d;
                allHintProbabilities[cell.Coordinate] = hints;
            }
            // If requested, we do additional solving based on the remaining mine count
            if (Settings.SolveByMineCount)
            {
                SolveMapByMineCounts(map, commonBorder, borders, allProbabilities, allVerdicts, allHintProbabilities);
            }

            
            //CalculateAllHintPossibilities(map, allProbabilities, allVerdicts, allHintProbabilities);

            // We get the final results, and return
            var finalResults = GetFinalResults(allProbabilities, allVerdicts, allHintProbabilities);
            return finalResults;
        }

        private void CalculateAllHintPossibilities(IMap map, IDictionary<Coordinate, double> probabilities, IDictionary<Coordinate, bool> verdicts, IDictionary<Coordinate, IDictionary<int, double>> allHintProbabilities)
        {
            foreach (var cell in map.AllCells.Where(x => x.State == CellState.Filled && x.Flag != CellFlag.HasMine))
            {
                var coord = cell.Coordinate;
                var guaranteedMines = 0;
                var neighbourProbabilitySum = 0d;
                var hintProbabilities = new Dictionary<int, double>();
                var hintSyncs = new Dictionary<int, object>();
                for (int i = 0; i <= 8; i++)
                {
                    hintProbabilities[i] = 0;
                    hintSyncs[i] = new object();
                }
                allHintProbabilities[coord] = hintProbabilities;
                var preliminaryNeighbours = map.NeighbourCache[coord].ByState[CellState.Filled];
                var neighbours = new List<Coordinate>();
                foreach (var neighbour in preliminaryNeighbours)
                {
                    if (neighbour.Flag == CellFlag.HasMine)
                    {
                        guaranteedMines++;
                        continue;
                    }
                    var neighbourCoord = neighbour.Coordinate;
                    /*bool neighbourVerdict;
                    if (verdicts.TryGetValue(neighbourCoord, out neighbourVerdict))
                    {
                        if (neighbourVerdict)
                        {
                            guaranteedMines++;
                        }
                        continue;
                    }*/
                    double neighbourMineProbability;
                    if (!probabilities.TryGetValue(neighbourCoord, out neighbourMineProbability))
                    {
                        continue;
                    }
                    neighbourProbabilitySum += neighbourMineProbability;
                    neighbours.Add(neighbour.Coordinate);
                }
                if (neighbours.Count == 0)
                {

                    hintProbabilities[guaranteedMines] = 1;
                    continue;
                }
                var possibleCombinations = 1 << neighbours.Count;
                var combinationSequence = Enumerable.Range(0, possibleCombinations);
                combinationSequence.ForEach(combination =>
                {
                    var totalProbability = 1d;
                    var totalMines = guaranteedMines;
                    for (int i = 0; i < neighbours.Count; i++)
                    {
                        var neighbour = neighbours[i];
                        var hasMine = (combination & (1 << i)) > 0;
                        if (hasMine)
                        {
                            totalProbability *= probabilities[neighbour];
                            totalMines++;
                        }
                        else
                        {
                            totalProbability *= 1d - probabilities[neighbour];
                        }
                    }
                    lock (hintSyncs[totalMines])
                    {
                        hintProbabilities[totalMines] += totalProbability;
                    }
                });
            }
        }

        private void SolveGaussian(BorderSeparationSolverMap map, Dictionary<Coordinate, bool> allVerdicts)
        {
            var gaussianSolvingService = new GaussianSolvingService();

            var parameters = new List<MatrixReductionParameters>
            {
                new MatrixReductionParameters(true),
                new MatrixReductionParameters(false, true, false, true, true),
                new MatrixReductionParameters(false, true, true, true, true),
                new MatrixReductionParameters(false, false, false, true, true),
                new MatrixReductionParameters(false, false, true, true, true),
                //new MatrixReductionParameters(false, true, false, false, true),
                new MatrixReductionParameters(false, false, true, false, true),
                new MatrixReductionParameters(false, true, false, true, false),
                new MatrixReductionParameters(false, true, true, true, false),
            };


            var coordinates = (IList<Coordinate>)map.AllCells.Where(x => x.State == CellState.Filled && x.Flag != CellFlag.HasMine && x.Flag != CellFlag.DoesntHaveMine).Select(x => x.Coordinate).ToList();
            //var coordinates = commonBorder.Cells.Select(x => x.Coordinate).ToList();
            var matrix = gaussianSolvingService.GetMatrixFromMap(map, coordinates, true);
            var sync = new object();
            var gaussianResults = new Dictionary<Coordinate, bool>();
            parameters.ForEach(p =>
            //Parallel.ForEach(parameters, p =>
            {
                var localMatrix = gaussianSolvingService.CloneMatrix(matrix);
                var localCoordinates = (IList<Coordinate>)coordinates.ToList();
                var roundVerdicts = new Dictionary<Coordinate, bool>();
                gaussianSolvingService.ReduceMatrix(ref localCoordinates, ref localMatrix, roundVerdicts, p);
                //gaussianSolvingService.SetVerdictsFromMatrix(ref localCoordinates, ref localMatrix, roundVerdicts);
                lock (sync)
                {
                    foreach (var verdict in roundVerdicts)
                    {
                        gaussianResults[verdict.Key] = verdict.Value;
                    }
                }
            });

            foreach (var verdict in gaussianResults)
            {
                allVerdicts[verdict.Key] = verdict.Value;
            }
            SetCellsByVerdicts(map, gaussianResults);
        }

        private bool ShouldStopSolving(IDictionary<Coordinate, bool> allVerdicts)
        {
            return allVerdicts.Count > 0 && (Settings.StopOnAnyVerdict || (Settings.StopOnNoMineVerdict && allVerdicts.Any(x => !x.Value)));
        }

        private void SolveTrivial(BorderSeparationSolverMap map, IDictionary<Coordinate, bool> allVerdicts)
        {
            // Trivial solving works in rounds - it may fail to find a verdict once, but will find it on a second pass.
            // We keep solving until we fail to find anything.
            while (true)
            {
                var currentRoundVerdicts = new Dictionary<Coordinate, bool>();

                // We find all empty cells, and iterate through them
                var emptyCells = map.AllCells.Where(x => x.State == CellState.Empty);
                foreach (var cell in emptyCells)
                {
                    // We find all the cells' filled neighbours.
                    // If there's none of them, or they're all flagged, we can skip this cell.
                    var neighbourEntry = map.NeighbourCache[cell.Coordinate];
                    var filledNeighbours = neighbourEntry.ByState[CellState.Filled];
                    var flaggedNeighbours = neighbourEntry.ByFlag[CellFlag.HasMine];
                    var antiflaggedNeighbours = neighbourEntry.ByFlag[CellFlag.DoesntHaveMine];
                    if (filledNeighbours.Count == flaggedNeighbours.Count + antiflaggedNeighbours.Count)
                    {
                        continue;
                    }

                    // If the hint is equal to however many filled neighbours a cell has,
                    // all the neighbours must have a mine, so we flag them all.
                    if (filledNeighbours.Count == cell.Hint)
                    {
                        var neighboursToFlag = filledNeighbours.Where(x => x.Flag != CellFlag.HasMine && !allVerdicts.ContainsKey(x.Coordinate));
                        foreach (var neighbour in neighboursToFlag)
                        {
                            currentRoundVerdicts[neighbour.Coordinate] = true;
                            allVerdicts[neighbour.Coordinate] = true;
                        }
                    }

                    // If the hint is equal to however many flagged neighbours a cell has,
                    // then the remaining filled non-flagged neighbours have to be empty, so we "click" them all.
                    if (flaggedNeighbours.Count == cell.Hint)
                    {
                        var neighboursToClick = filledNeighbours.Where(x => x.Flag != CellFlag.HasMine && !allVerdicts.ContainsKey(x.Coordinate));
                        foreach (var neighbour in neighboursToClick)
                        {
                            currentRoundVerdicts[neighbour.Coordinate] = false;
                            allVerdicts[neighbour.Coordinate] = false;
                        }
                    }
                }

                // If we didn't find any results this round, we can stop searching and return.
                // Else, we modify the map to have our results, and go for another round.
                if (currentRoundVerdicts.Count == 0)
                {
                    return;
                }
                SetCellsByVerdicts(map, currentRoundVerdicts);
            }
        }

        private void SolveMapByMineCounts(BorderSeparationSolverMap map, Border commonBorder, List<Border> borders, IDictionary<Coordinate, double> allProbabilities, IDictionary<Coordinate, bool> allVerdicts, IDictionary<Coordinate, IDictionary<int, double>> allHintProbabilities)
        {
            if (!map.RemainingMineCount.HasValue)
            {
                return;
            }

            // We make two HashSets of the common border coordinates.
            // One which will stay unmodified - to be able to identify border cells without recalculating,
            // another which will get solved and unfit coordinates removed.
            var originalCommonBorderCoords = new HashSet<Coordinate>(commonBorder.Cells.Select(x => x.Coordinate));
            var nonBorderCells = map.AllCells.Where(x => !originalCommonBorderCoords.Contains(x.Coordinate) && x.State == CellState.Filled && x.Flag != CellFlag.HasMine && x.Flag != CellFlag.DoesntHaveMine).ToList();
            var commonBorderCoords = new HashSet<Coordinate>(originalCommonBorderCoords);
            commonBorderCoords.ExceptWith(allVerdicts.Keys);

            // If the border wasn't solved fully, we can't do later algorithms with it,
            // since we lack a list of valid combinations. So we split the borders into two lists
            // and remove the non-solved border cells from the common border.
            var fullySolvedBorders = borders.Where(x => x.SolvedFully).ToList();
            var unsolvedBorders = borders.Where(x => !x.SolvedFully).ToList();
            foreach (var unsolvedBorder in unsolvedBorders)
            {
                foreach (var unsolvedBorderCell in unsolvedBorder.Cells)
                {
                    commonBorderCoords.Remove(unsolvedBorderCell.Coordinate);
                }
            }

            // We look at each fully solved border, and remove any invalid combinations due to mine count.
            // This step isn't mandatory, since we will look at the common border as a whole later, but
            // it may greatly help cut down on the amount of combinations in the common border later on.
            foreach (var border in fullySolvedBorders)
            {
                var otherBorders = fullySolvedBorders.Where(x => x != border).ToList();
                var guaranteedMines = otherBorders.Sum(x => x.MinMineCount);
                var guaranteedEmpty = otherBorders.Sum(x => x.Cells.Count - x.MaxMineCount);
                TrimValidCombinationsByMineCount(border, map.RemainingMineCount.Value, map.UndecidedCount, guaranteedMines, guaranteedEmpty);
            }

            // We split out fully solved borders into two groups - those that have an exact known mine count,
            // and those that don't. We can remove the ones that have an exact known mine count
            // from the common border, since we don't need to look at the individual combinations,
            // we can just say for certain that this border has X mines, and N-X non-mines.
            var bordersWithExactMineCount = fullySolvedBorders.Where(x => x.MinMineCount == x.MaxMineCount).ToList();
            var bordersWithVariableMineCount = fullySolvedBorders.Where(x => x.MinMineCount != x.MaxMineCount).ToList();
            foreach (var border in bordersWithExactMineCount)
            {
                foreach (var cell in border.Cells)
                {
                    commonBorderCoords.Remove(cell.Coordinate);
                }
            }

            var nonBorderMineCountProbabilities = new Dictionary<int, double>();

            // If we have any borders with a non-constant mine count, to get accurate probabilities 
            // we need to re-caulculate them as a whole.
            if (bordersWithVariableMineCount.Count > 0)
            {
                var exactMineCount = bordersWithExactMineCount.Sum(x => x.MaxMineCount);
                var exactBorderSize = bordersWithExactMineCount.Sum(x => x.Cells.Count);
                var exactNonMineCount = exactBorderSize - exactMineCount;
                GetVariableMineCountBordersProbabilities(map, bordersWithVariableMineCount, map.RemainingMineCount.Value, map.UndecidedCount, nonBorderCells.Count, exactMineCount, exactNonMineCount, allProbabilities, nonBorderMineCountProbabilities, allHintProbabilities);
            }

            // If requested, we calculate the probabilities of mines in non-border cells, and copy them over.
            if (Settings.SolveNonBorderCells)
            {
                var nonBorderProbabilities = GetNonBorderProbabilitiesByMineCount(map, allProbabilities, nonBorderCells);
                foreach (var probability in nonBorderProbabilities)
                {
                    allProbabilities[probability.Key] = probability.Value;
                    //IDictionary<int, double> hintProbabilities = new Dictionary<int, double>();
                    //hintProbabilities[0] = 1 - probability.Value;
                    //hintProbabilities[1] = probability.Value;
                    //var neighbourHintProbabilities = map.NeighbourCache[probability.Key].AllNeighbours.ToDictionary(x => x.Coordinate, x => hintProbabilities);
                    //InsertHintProbabilities(allHintProbabilities, neighbourHintProbabilities);
                }
            }

            // Lastly, we find guaranteed verdicts from the probabilties, and copy them over.
            GetVerdictsFromProbabilities(allProbabilities, allVerdicts);
        }

        //private void 

        private void CalculateHintProbabilitiesForBorder(IMap map, Border border, IDictionary<Coordinate, IDictionary<int, double>> allHintProbabilities)
        {
            foreach (var combination in border.ValidCombinations)
            {
                
            }
        }

        private IList<Border> SolveBorder(Border border, BorderSeparationSolverMap map, bool allowPartialBorderSolving)
        {
            if (Settings.PartialBorderSolving)
            {
                // If the border is too big, we attempt solving by partial borders.
                if (allowPartialBorderSolving && border.Cells.Count > Settings.PartialBorderSolveFrom)
                {
                    TrySolveBorderByPartialBorders(border, map);
                }
                if (ShouldStopSolving(border.Verdicts))
                {
                    return new[] { border };
                }

                // If partial border solving found any guaranteed solutions, we can attempt to re-split the border.
                if (Settings.BorderResplitting && border.Verdicts.Count > 0)
                {
                    var borders = TrySolveBorderByReseparating(border, map);
                    if (borders != null)
                    {
                        return borders;
                    }
                }
            }

            // If even after partial border solving the border is too big, we give up, and return.
            if (border.Cells.Count > Settings.GiveUpFrom)
            {
                border.SolvedFully = false;
                return new[] { border };
            }

            // We find all possible valid combinations for this border. If we didn't find any,
            // this means the map is invalid.
            border.ValidCombinations = FindValidBorderCellCombinations(map, border);
            if (border.ValidCombinations.Count == 0)
            {
                // TODO Must be invalid map... Handle somehow
            }

            // We find the minimum and maximum possible mines in the border
            var currentMineVerdicts = border.Verdicts.Count(x => x.Value);
            border.MinMineCount = border.ValidCombinations.Min(x => x.Count(y => y.Value)) + currentMineVerdicts;
            border.MaxMineCount = border.ValidCombinations.Max(x => x.Count(y => y.Value)) + currentMineVerdicts;

            // We find the border's probabilities, find the verdicts from said probabilities, and modify any cells we found.
            // We then remove all verdicts from the probabilities, and remove the cells from the border.
            GetBorderProbabilities(border, border.Probabilities);
            GetVerdictsFromProbabilities(border.Probabilities, border.Verdicts);
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
            // TODO: try resplitting borders anyway after solving?
            return new[] { border };
        }

        private void TrySolveBorderByPartialBorders(Border border, BorderSeparationSolverMap map)
        {
            IList<PartialBorderData> checkedPartialBorders = new List<PartialBorderData>();
            for (var i = 0; i < border.Cells.Count; i++)
            {
                var targetCoordinate = border.Cells[i].Coordinate;
                var partialBorderData = GetPartialBorder(border, map, targetCoordinate);
                var previousBorderData = checkedPartialBorders.FirstOrDefault(x => x.PartialBorderCoordinates.IsSupersetOf(partialBorderData.PartialBorderCoordinates));
                if (previousBorderData != null)
                {
                    double probability;
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
                if (border.Verdicts.Count > 0 && (Settings.StopOnAnyVerdict || (Settings.StopOnNoMineVerdict && border.Verdicts.Any(x => !x.Value))))
                {
                    return;
                }
                checkedPartialBorders.Add(partialBorderData);
                double partialProbability;
                if (Settings.SetPartiallyCalculatedProbabilities && !partialBorder.Verdicts.ContainsKey(targetCoordinate) && partialBorder.Probabilities.TryGetValue(targetCoordinate, out partialProbability))
                {
                    border.Probabilities[targetCoordinate] = partialProbability;
                }
            }
        }

        private IList<Border> TrySolveBorderByReseparating(Border border, BorderSeparationSolverMap map)
        {
            var resplitBorders = SeparateBorders(border, map).ToList();
            if (resplitBorders.Count < 2)
            {
                return null;
            }
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
            return resplitBorders;
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
                        //cell.State = CellState.Wall;
                        cell.Flag = CellFlag.DoesntHaveMine;
                        map.AntiFlaggedCount++;
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
                //entry.AllNeighbours = entry.AllNeighbours.Where(x => x.State != CellState.Wall).ToList();
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

        private IDictionary<Coordinate, double> GetNonBorderProbabilitiesByMineCount(IMap map, IDictionary<Coordinate, double> commonBorderProbabilities, ICollection<Cell> nonBorderUndecidedCells)
        {
            var probabilities = new Dictionary<Coordinate, double>();
            if (!map.RemainingMineCount.HasValue)
            {
                return probabilities;
            }

            var probabilitySum = commonBorderProbabilities.Sum(x => x.Value);
            var combinationsMineCount = probabilitySum;
            if (nonBorderUndecidedCells.Count == 0)
            {
                return probabilities;
            }
            var nonBorderProbability = (map.RemainingMineCount.Value - combinationsMineCount) / nonBorderUndecidedCells.Count;
            foreach (var nonBorderFilledCell in nonBorderUndecidedCells)
            {
                probabilities[nonBorderFilledCell.Coordinate] = nonBorderProbability;
            }

            return probabilities;
        }

        private void TrimValidCombinationsByMineCount(Border border, int minesRemaining, int undecidedCellsRemaining, int minesElsewhere, int nonMineCountElsewhere)
        {
            for (var i = 0; i < border.ValidCombinations.Count; i++)
            {
                var combination = border.ValidCombinations[i];
                var minePredictionCount = combination.Count(x => x.Value);
                var isValid = IsPredictionValidByMineCount(minePredictionCount, combination.Count, minesRemaining, undecidedCellsRemaining, minesElsewhere, nonMineCountElsewhere);
                if (!isValid)
                {
                    border.ValidCombinations.RemoveAt(i);
                    i--;
                }
            }
        }

        private bool IsCoordinateABorder(IMap map, Cell cell)
        {
            if (cell.State != CellState.Filled || cell.Flag == CellFlag.HasMine || cell.Flag == CellFlag.DoesntHaveMine)
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
            var allFlaggedCoordinates = new HashSet<Coordinate>(map.AllCells.Where(x => x.Flag == CellFlag.HasMine || x.Flag == CellFlag.DoesntHaveMine).Select(x => x.Coordinate));
            var borderCoordinates = border.Cells.Select(x => x.Coordinate);
            var partialBorderSequence = GetPartialBorderCellSequence(borderCoordinates, map, targetCoordinate);
            foreach (var cell in partialBorderSequence)
            {
                partialBorderCells.Add(cell);
                if (partialBorderCells.Count < Settings.MaxPartialBorderSize)
                {
                    continue;
                }
                var partialBorderCandidate = new Border(partialBorderCells);
                var partialMapCandidate = CalculatePartialMapAndTrimPartialBorder(partialBorderCandidate, map, allFlaggedCoordinates);
                if (partialBorderCandidate.Cells.Count > Settings.MaxPartialBorderSize)
                {
                    break;
                }
                partialBorder = partialBorderCandidate;
                partialMap = partialMapCandidate;
            }
            //Debugging.Visualize(map, border, partialBorder);
            if (partialBorder == null)
            {
                partialBorder = new Border(partialBorderCells);
                partialMap = CalculatePartialMapAndTrimPartialBorder(partialBorder, map, allFlaggedCoordinates);
            }
            var coordSet = new HashSet<Coordinate>(partialBorder.Cells.Select(x => x.Coordinate));
            var partialBorderData = new PartialBorderData(coordSet, partialMap, partialBorder);
            return partialBorderData;
        }

        private BorderSeparationSolverMap CalculatePartialMapAndTrimPartialBorder(Border border, BorderSeparationSolverMap parentMap, HashSet<Coordinate> allFlaggedCoordinates)
        {
            var borderCoordinateSet = new HashSet<Coordinate>(border.Cells.Select(x => x.Coordinate));
            var allSurroundingEmpty = borderCoordinateSet.SelectMany(x => parentMap.NeighbourCache[x].ByState[CellState.Empty]).Distinct();
            var onlyInfluencingBorder = allSurroundingEmpty.Where(x => parentMap.NeighbourCache[x.Coordinate].ByState[CellState.Filled].All(y => borderCoordinateSet.Contains(y.Coordinate) || allFlaggedCoordinates.Contains(y.Coordinate))).ToList();
            var onlyInflucencingBorderSet = new HashSet<Coordinate>(onlyInfluencingBorder.Select(x => x.Coordinate));
            var newNonBorderCells = border.Cells.Where(x => parentMap.NeighbourCache[x.Coordinate].ByState[CellState.Empty].Any(y => onlyInflucencingBorderSet.Contains(y.Coordinate))).ToList();
            border.Cells = newNonBorderCells;
            if (border.Cells.Count == 0)
            {
                return null;
            }
            var newWidth = parentMap.Width;
            var newHeight = parentMap.Height;
            var partialMap = new Map(newWidth, newHeight, null, true, CellState.Wall);
            foreach (var cell in border.Cells)
            {
                partialMap[cell.Coordinate] = cell;
            }
            foreach (var cell in onlyInfluencingBorder)
            {
                partialMap[cell.Coordinate] = cell;
            }
            foreach (var flaggedCoordinate in allFlaggedCoordinates)
            {
                partialMap[flaggedCoordinate] = parentMap[flaggedCoordinate];
            }
            var solverMap = new BorderSeparationSolverMap(partialMap);
            return solverMap;
        }

        private IEnumerable<Cell> GetPartialBorderCellSequence(IEnumerable<Coordinate> allowedCoordinates, IMap map, Coordinate targetCoordinate)
        {
            var commonCoords = new HashSet<Coordinate>(allowedCoordinates);//new HashSet<Coordinate>(commonBorder.Cells.Select(x => x.Coordinate));
            var coordQueue = new Queue<Coordinate>();
            coordQueue.Enqueue(targetCoordinate);
            var visited = new HashSet<Coordinate>();
            //commonCoords.Remove(targetCoordinate);
            while (coordQueue.Count > 0)
            {
                var coord = coordQueue.Dequeue();
                var cell = map[coord];
                if (commonCoords.Remove(coord))
                {
                    yield return cell;
                }
                visited.Add(coord);
                //var neighbors = map.NeighbourCache[coord].ByState[CellState.Filled].Where(x => commonCoords.Contains(x.Coordinate));
                var unflaggedNeighbours = map.NeighbourCache[coord].ByFlag[CellFlag.None];
                var neighbors = unflaggedNeighbours.Where(x => (cell.State == CellState.Filled && x.State == CellState.Empty) || (cell.State == CellState.Empty && x.State == CellState.Filled));
                foreach (var neighbor in neighbors)
                {
                    //commonCoords.Remove(neighbor.Coordinate);
                    if (visited.Add(neighbor.Coordinate))
                    {
                        coordQueue.Enqueue(neighbor.Coordinate);
                    }
                }
            }
        }

        private IEnumerable<Border> SeparateBorders(Border commonBorder, IMap map)
        {
            var commonCoords = new HashSet<Coordinate>(commonBorder.Cells.Select(x => x.Coordinate));
            while (commonCoords.Count > 0)
            {
                var initialCoord = commonCoords.First();
                var cells = GetPartialBorderCellSequence(commonCoords, map, initialCoord).ToList();
                var border = new Border(cells);
                yield return border;
                commonCoords.ExceptWith(cells.Select(x => x.Coordinate));
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
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        public bool IsPredictionValid(IMap map, IDictionary<Coordinate, bool> prediction, IList<Cell> emptyCells)
        {
            foreach (var cell in emptyCells)
            {
                var neighboursWithMine = 0;
                var neighboursWithoutMine = 0;
                var filledNeighbours = map.NeighbourCache[cell.Coordinate].ByState[CellState.Filled];
                bool foundUnknownCell = false;
                foreach (var neighbour in filledNeighbours)
                {
                    switch (neighbour.Flag)
                    {
                        case CellFlag.HasMine:
                            neighboursWithMine++;
                            break;
                        case CellFlag.DoesntHaveMine:
                            neighboursWithoutMine++;
                            break;
                        default:
                            bool verdict;
                            var success = prediction.TryGetValue(neighbour.Coordinate, out verdict);
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
                            break;
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

        private void GetBorderProbabilities(Border border, IDictionary<Coordinate, double> targetProbabilities)
        {
            if (border.ValidCombinations.Count == 0)
            {
                return;
            }
            foreach (var cell in border.Cells)
            {
                var mineInCount = border.ValidCombinations.Count(x => x[cell.Coordinate]);
                var probability = (double)mineInCount / border.ValidCombinations.Count;
                targetProbabilities[cell.Coordinate] = probability;
            }
        }

        private void GetVariableMineCountBordersProbabilities(IMap map, IList<Border> borders, int minesRemaining, int undecidedCellsRemaining, int nonBorderCellCount, int minesElsewhere, int nonMineCountElsewhere, IDictionary<Coordinate, double> targetProbabilities, IDictionary<int, double> nonBorderMineCountProbabilities, IDictionary<Coordinate, IDictionary<int, double>> allHintProbabilities)
        {
            var alreadyFoundMines = borders.Sum(x => x.Verdicts.Count(y => y.Value));
            var minMinesInNonBorder = minesRemaining - minesElsewhere - borders.Sum(x => x.MaxMineCount) + alreadyFoundMines;
            if (minMinesInNonBorder < 0)
            {
                minMinesInNonBorder = 0;
            }
            var maxMinesInNonBorder = minesRemaining - minesElsewhere - borders.Sum(x => x.MinMineCount) + alreadyFoundMines;
            if (maxMinesInNonBorder > nonBorderCellCount)
            {
                maxMinesInNonBorder = nonBorderCellCount;
            }
            var ratios = new Dictionary<int, double>();
            var nonBorderMineCounts = new Dictionary<int, double>();
            double currentRatio = 1;
            for (var i = minMinesInNonBorder; i <= maxMinesInNonBorder; i++)
            {
                currentRatio *= Maths.CombinationRatio(nonBorderCellCount, i);
                ratios[i] = currentRatio;
                nonBorderMineCounts[i] = 0;
            }
            var totalCombinationLength = borders.Sum(x => x.Cells.Count);
            var mineCounts = borders.SelectMany(x => x.Cells).ToDictionary(x => x.Coordinate, x => 0d);
            var countLocks = mineCounts.ToDictionary(x => x.Key, x => new object());
            var commonLock = new object();
            //double totalValidCombinations = 0;
            var cartesianSequence = borders.Select(x => x.ValidCombinations).MultiCartesian();
            //var neighbourHintCounts = new Dictionary<Coordinate, IDictionary<int, double>>();
            var neighbourHintMineCounts = map.AllCells.ToDictionary(x => x.Coordinate, x => (IDictionary<int, double>)Enumerable.Range(0,9).ToDictionary(y => y, y => 0d));
            var neighbourHintTotalCounts = map.AllCells.ToDictionary(x => x.Coordinate, x => (IDictionary<int, double>)Enumerable.Range(0, 9).ToDictionary(y => y, y => 0d));
            var neighbourHintLocks = neighbourHintTotalCounts.ToDictionary(x => x.Key, x => new object());
            //Parallel.ForEach(cartesianSequence, combinationArr =>
            cartesianSequence.ForEach(combinationArr =>
            {
                var minePredictionCount = combinationArr.SelectMany(x => x).Count(x => x.Value);
                var minesInNonBorder = minesRemaining - minesElsewhere - minePredictionCount;
                if (minesInNonBorder < 0)
                {
                    return;
                }
                if (minesInNonBorder > nonBorderCellCount)
                {
                    return;
                }
                var weight = ratios[minesInNonBorder];
                lock (commonLock)
                {
                    //totalValidCombinations += ratio;
                    nonBorderMineCounts[minesInNonBorder] += weight;
                }
                var isValid = IsPredictionValidByMineCount(minePredictionCount, totalCombinationLength, minesRemaining, undecidedCellsRemaining, minesElsewhere, nonMineCountElsewhere);
                if (!isValid)
                {
                    throw new Exception("temp");
                }
                var neighbourMineCounts = new Dictionary<Coordinate, int>();
                var borderCellsAroundNeighbours = new Dictionary<Coordinate, int>();
                foreach (var combination in combinationArr)
                {
                    foreach (var verdict in combination)
                    {
                        var neighbours = map.NeighbourCache[verdict.Key].AllNeighbours;
                        foreach (var neighbour in neighbours)
                        {
                            int existingCount;
                            borderCellsAroundNeighbours.TryGetValue(neighbour.Coordinate, out existingCount);
                            borderCellsAroundNeighbours[neighbour.Coordinate] = existingCount + 1;
                        }
                        if (verdict.Value)
                        {
                            foreach (var neighbour in neighbours)
                            {
                                int existingCount;
                                neighbourMineCounts.TryGetValue(neighbour.Coordinate, out existingCount);
                                neighbourMineCounts[neighbour.Coordinate] = existingCount + 1;
                            }
                            lock (countLocks[verdict.Key])
                            {
                                mineCounts[verdict.Key] += weight;
                            }
                        }
                    }
                    foreach (var neighbourMineCount in neighbourMineCounts)
                    {
                        var totalCellCount = borderCellsAroundNeighbours[neighbourMineCount.Key];
                        lock (neighbourHintLocks[neighbourMineCount.Key])
                        {
                            neighbourHintMineCounts[neighbourMineCount.Key][neighbourMineCount.Value] += weight;
                            neighbourHintTotalCounts[neighbourMineCount.Key][neighbourMineCount.Value] += totalCellCount*weight;
                        }
                    }

                }
            });
            var totalValidCombinations = nonBorderMineCounts.Values.Sum();
            foreach (var nonBorderMineCount in nonBorderMineCounts.Where(x => x.Value > 0.0000000001))
            {
                nonBorderMineCountProbabilities[nonBorderMineCount.Key] = nonBorderMineCount.Value/totalValidCombinations;
            }
            foreach (var count in mineCounts)
            {
                var coord = count.Key;
                var probability = count.Value / totalValidCombinations;
                targetProbabilities[coord] = probability;
            }
            var neighbourHintProbabilities = new Dictionary<Coordinate, IDictionary<int, double>>();
            foreach (var count in neighbourHintMineCounts)
            {
                var coord = count.Key;
                var totalCounts = neighbourHintMineCounts[count.Key];
                var totalCount = totalCounts.Values.Sum();
                if (totalCount < 0.000000001d)
                {
                    continue;
                }
                neighbourHintProbabilities[coord] = count.Value.ToDictionary(x => x.Key, x => x.Value / totalCount);
            }
            InsertHintProbabilities(allHintProbabilities, neighbourHintProbabilities);
        }

        private void InsertHintProbabilities(IDictionary<Coordinate, IDictionary<int, double>> allParentHintProbabilities, Coordinate coord, IDictionary<int, double> otherHintProbabilities)
        {
            IDictionary<int, double> parentHintProbabilities;
            if (!allParentHintProbabilities.TryGetValue(coord, out parentHintProbabilities))
            {
                allParentHintProbabilities[coord] = otherHintProbabilities;
                return;
            }
            //var parentHintProbabilities = allParentHintProbabilities[otherHintProbabilities.Key];
            var newHintProbabilities = Enumerable.Range(0, 9).ToDictionary(x => x, x => 0d);
            //foreach (var parentHintProbability in parentHintProbabilities)
            for(var i = 0; i <= 8; i++)
            {
                double parentHintProbability;
                parentHintProbabilities.TryGetValue(i, out parentHintProbability);
                //foreach (var otherHintProbability in otherHintProbabilities)
                for(var j = 0; j <= 8; j++)
                {
                    double otherHintProbability;
                    otherHintProbabilities.TryGetValue(j, out otherHintProbability);
                    var newHint = i + j;
                    var newProbability = parentHintProbability * otherHintProbability;
                    if (newHint > 8)
                    {
                        if (newProbability > 0.000000001)
                        {
                            throw new Exception("This shouldn't happen");
                        }
                        continue;
                    }
                    newHintProbabilities[newHint] += newProbability;
                }
            }
            allParentHintProbabilities[coord] = newHintProbabilities;
        }

        private void InsertHintProbabilities(IDictionary<Coordinate, IDictionary<int, double>> allParentHintProbabilities, IDictionary<Coordinate, IDictionary<int, double>> allOtherHintProbabilities)
        {
            foreach (var otherHintProbabilities in allOtherHintProbabilities)
            {
                InsertHintProbabilities(allParentHintProbabilities, otherHintProbabilities.Key, otherHintProbabilities.Value);
            }
        }

        private bool IsPredictionValidByMineCount(int minePredictionCount, int totalCombinationLength, int minesRemaining, int undecidedCellsRemaining, int minesElsewhere, int nonMineCountElsewhere)
        {
            if (minePredictionCount + minesElsewhere > minesRemaining)
            {
                return false;
            }
            if (minesRemaining - minePredictionCount > undecidedCellsRemaining - totalCombinationLength - nonMineCountElsewhere)
            {
                return false;
            }
            return true;
        }

        private void GetVerdictsFromProbabilities(IDictionary<Coordinate, double> probabilities, IDictionary<Coordinate, bool> targetVerdicts)
        {
            foreach (var probability in probabilities)
            {
                bool verdict;
                if (Math.Abs(probability.Value) < 0.0000001)
                {
                    verdict = false;
                }
                else if (Math.Abs(probability.Value - 1) < 0.0000001)
                {
                    verdict = true;
                }
                else
                {
                    continue;
                }
                targetVerdicts[probability.Key] = verdict;
            }
        }

        private IDictionary<Coordinate, SolverResult> GetFinalResults(IDictionary<Coordinate, double> probabilities, IDictionary<Coordinate, bool> verdicts, IDictionary<Coordinate, IDictionary<int, double>> hintProbabilities = null)
        {
            var results = new Dictionary<Coordinate, SolverResult>();
            if (probabilities != null)
            {
                foreach (var probability in probabilities)
                {
                    results[probability.Key] = new SolverResult(probability.Key, probability.Value, null);
                }
            }
            if (verdicts != null)
            {
                foreach (var verdict in verdicts)
                {
                    var solverResult = new SolverResult(verdict.Key, verdict.Value ? 1 : 0, verdict.Value);
                    results[verdict.Key] = solverResult;
                }
            }
            if (hintProbabilities != null)
            {
                foreach (var hintProbability in hintProbabilities)
                {
                    SolverResult result;
                    if (!results.TryGetValue(hintProbability.Key, out result))
                    {
                        continue;
                    }
                    result.HintProbabilities = hintProbability.Value;
                }
            }
            return results;
        }




    }
}