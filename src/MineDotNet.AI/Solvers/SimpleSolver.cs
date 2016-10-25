using System.Collections.Generic;
using System.Linq;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public class SimpleSolver : SolverBase
    {
        public override IDictionary<Coordinate, Verdict> Solve(Map map)
        {
            var verdicts = new Dictionary<Coordinate, Verdict>();
            var initialVerdictCount = -1;
            while (verdicts.Count != initialVerdictCount)
            {
                initialVerdictCount = verdicts.Count;
                var hintedCells = map.AllCells.Where(x => x.Hint != 0);
                foreach (var cell in hintedCells)
                {
                    var cellNeighbours = map.GetNeighboursOf(cell);
                    var filledNeighbours = cellNeighbours.Where(x => x.State == CellState.Filled && (!verdicts.ContainsKey(x.Coordinate) || verdicts[x.Coordinate] != Verdict.DoesntHaveMine)).ToList();
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
            }
            return verdicts;
        }
    }
}