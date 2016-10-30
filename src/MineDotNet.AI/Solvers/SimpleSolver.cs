using System.Collections.Generic;
using System.Linq;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public class SimpleSolver : SolverBase
    {
        public override IDictionary<Coordinate, SolverResult> Solve(IMap map, IDictionary<Coordinate, SolverResult> previousResults = null)
        {
            var results = new Dictionary<Coordinate, SolverResult>();
            var initialVerdictCount = -1;
            while (results.Count != initialVerdictCount)
            {
                initialVerdictCount = results.Count;
                var allCells = map.AllCells.Where(x => x.State == CellState.Empty);
                foreach (var cell in allCells)
                {
                    var cellNeighbours = map.GetNeighboursOf(cell);
                    var filledNeighbours = cellNeighbours.Where(x => x.State == CellState.Filled && (!results.ContainsKey(x.Coordinate) || results[x.Coordinate].Verdict != Verdict.DoesntHaveMine)).ToList();
                    var markedNeighbours = filledNeighbours.Where(x => x.Flag == CellFlag.HasMine || (results.ContainsKey(x.Coordinate) && results[x.Coordinate].Verdict == Verdict.HasMine)).ToList();
                    if (filledNeighbours.Count == markedNeighbours.Count)
                    {
                        continue;
                    }
                    if (filledNeighbours.Count == cell.Hint)
                    {
                        var neighboursToFlag = filledNeighbours.Where(x => x.Flag != CellFlag.HasMine && !results.ContainsKey(x.Coordinate));
                        foreach (var neighbour in neighboursToFlag)
                        {
                            var result = new SolverResult(neighbour.Coordinate, 1, Verdict.HasMine);
                            results.Add(neighbour.Coordinate, result);
                        }
                    }
                    if (markedNeighbours.Count == cell.Hint)
                    {
                        var unmarkedNeighbours = filledNeighbours.Where(x => x.Flag != CellFlag.HasMine);
                        var neighboursToClick = unmarkedNeighbours.Where(x => !results.ContainsKey(x.Coordinate));
                        foreach (var neighbour in neighboursToClick)
                        {
                            var result = new SolverResult(neighbour.Coordinate, 0, Verdict.DoesntHaveMine);
                            results.Add(neighbour.Coordinate, result);
                        }
                    }
                }
            }
            return results;
        }
    }
}