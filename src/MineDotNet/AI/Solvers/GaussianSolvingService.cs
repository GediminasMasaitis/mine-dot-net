using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public class GaussianSolvingService
    {
        public IList<int[]> GetMatrixFromMap(IMap map, IList<Coordinate> coordinates, bool allUndecidedCoordinatesProvided)
        {
            var hintCoords = new HashSet<Coordinate>();
            var indices = new Dictionary<Coordinate, int>();
            for (var i = 0; i < coordinates.Count; i++)
            {
                var coordinate = coordinates[i];
                hintCoords.UnionWith(map.NeighbourCache[coordinate].ByState[CellState.Empty].Select(x => x.Coordinate));
                indices[coordinate] = i;
            }

            var hintCells = hintCoords.Select(x => map[x]).ToList();
            var matrix = new List<int[]>();
            for (var i = 0; i < hintCells.Count; i++)
            {
                var hintCell = hintCells[i];
                var remainingHint = hintCell.Hint - map.NeighbourCache[hintCell.Coordinate].ByFlag[CellFlag.HasMine].Count;
                var undecidedNeighbours = map.NeighbourCache[hintCell.Coordinate].ByState[CellState.Filled].Where(x => x.Flag != CellFlag.HasMine);
                var row = new int[coordinates.Count + 1];
                foreach (var undecidedNeighbour in undecidedNeighbours)
                {
                    var index = indices[undecidedNeighbour.Coordinate];
                    row[index] = 1;
                }
                row[row.Length - 1] = remainingHint;
                matrix.Add(row);
                //matrix[i] = row;
            }
            if (allUndecidedCoordinatesProvided && map.RemainingMineCount.HasValue)
            {
                var row = new int[coordinates.Count + 1];
                for (var i = 0; i < coordinates.Count; i++)
                {
                    row[i] = 1;
                }
                row[row.Length - 1] = map.RemainingMineCount.Value;
                matrix.Add(row);
            }
            return matrix;
        }

        public class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
        {
            public bool Equals(T[] x, T[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(T[] obj)
            {
                int hash = 19;
                foreach (var o in obj)
                {
                    hash = hash * 31 + o.GetHashCode();
                }
                return hash;
            }
        }

        public void SetVerdictsFromMatrix(ref IList<Coordinate> coordinates, ref IList<int[]> matrix, IDictionary<Coordinate, bool> allVerdicts)
        {
            
            var remainingMatrix = new List<int[]>();
            var indicesToRemove = new HashSet<int>();
            foreach (var row in matrix)
            {
                var index = -1;
                var skip = false;
                for (var i = 0; i < row.Length - 1; i++)
                {
                    if (row[i] != 0)
                    {
                        if (index >= 0)
                        {
                            skip = true;
                            break;
                        }
                        else
                        {
                            index = i;
                        }
                    }
                }
                if (skip)
                {
                    remainingMatrix.Add(row);
                    continue;
                }
                indicesToRemove.Add(index);
                var verdict = row[index] == row[row.Length - 1];
                var coordinate = coordinates[index];
                allVerdicts[coordinate] = verdict;
            }
            for (int i = 0; i < remainingMatrix.Count; i++)
            {
                var row = remainingMatrix[i];
                var newRow = new int[row.Length - indicesToRemove.Count];
                var index = 0;
                for (int j = 0; j < row.Length; j++)
                {
                    if (!indicesToRemove.Contains(j))
                    {
                        newRow[index++] = row[j];
                    }
                }
                remainingMatrix[i] = newRow;
            }
            matrix = remainingMatrix.ToArray();
            coordinates = coordinates.Where((x, i) => !indicesToRemove.Contains(i)).ToList();
#if DEBUG
            Debug.WriteLine(MatrixToString(matrix));
#endif
        }



        public void ReduceMatrix(ref IList<Coordinate> coordinates, ref IList<int[]> matrix, IDictionary<Coordinate, bool> allVerdicts, MatrixReductionParameters parameters = null)
        {
            if (matrix.Count == 0)
            {
                return;
            }
            matrix = matrix.Where(x => Array.FindIndex(x, y => y != 0) != -1).Distinct(new ArrayEqualityComparer<int>()).OrderBy(x => Array.FindIndex(x, y => y != 0)).ToList();
            if (matrix.Count == 0)
            {
                return;
            }
            var splitsMade = false;
            parameters = parameters ?? new MatrixReductionParameters();
            if (!parameters.SkipReduction)
            {
#if DEBUG
                Debug.WriteLine(MatrixToString(matrix));
#endif
                var rows = matrix.Count;
                var cols = matrix[0].Length;

                var rowsRemaining = new HashSet<int>(Enumerable.Range(0, rows));
                var m = matrix;
                var columnsData = Enumerable.Range(0, cols - 1).Select(x => new {Index = x, Cnt = m.Count(y => y[x] != 0)}).Where(x => x.Cnt > 1);
                IEnumerable<int> columns;
                if (parameters.ReverseColumns)
                {
                    columns = columnsData.OrderByDescending(x => parameters.OrderColumns ? x.Cnt : x.Index).Select(x => x.Index);
                }
                else
                {
                    columns = columnsData.OrderBy(x => parameters.OrderColumns ? x.Cnt : x.Index).Select(x => x.Index);
                }

                //for (var col = 0; col < cols - 1; col++)
                foreach (var col in columns)
                {
                    var row = -1;
                    if (parameters.ReverseRows)
                    {

                        for (var i = rows - 1; i >= 0; i--)
                        {
                            var candidateNum = matrix[i][col];
                            if ((candidateNum == 1 || candidateNum == -1) && (!parameters.UseUniqueRows || rowsRemaining.Remove(i)))
                            {
                                row = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < rows; i++)
                        {
                            var candidateNum = matrix[i][col];
                            if ((candidateNum == 1 || candidateNum == -1) && (!parameters.UseUniqueRows || rowsRemaining.Remove(i)))
                            {
                                row = i;
                                break;
                            }
                        }
                    }
                    if (row == -1)
                    {
                        continue;
                    }

                    var targetNum = matrix[row][col];
                    if (targetNum == -1)
                    {
                        for (int i = 0; i < cols; i++)
                        {
                            matrix[row][i] = -matrix[row][i];
                        }
                    }
                    targetNum = matrix[row][col];
//#if DEBUG
//                Debug.WriteLine($"Row: {row}");
//                Debug.WriteLine($"Col: {col}");
//                Debug.WriteLine(MatrixToString(matrix));
//#endif
                    if (targetNum != 1)
                    {
                        File.WriteAllText("badmatrix.txt", MatrixToString(matrix));
                        throw new Exception("coef!=1");
                        for (int i = 0; i < cols; i++)
                        {
                            matrix[row][i] = matrix[row][i]/targetNum;
                        }
                    }

                    for (var i = 0; i < rows; i++)
                    {
                        //var newRows = SeparateRow(matrix[i]).ToList();
                        //if (newRows.Count > 1)
                        //{
                        //    splitsMade = true;
                        //    matrix.RemoveAt(i);
                        //    foreach (var newRow in newRows)
                        //    {
                        //        matrix.Insert(i, newRow);
                        //    }
                        //    rows += newRows.Count - 1;
                        //    i += newRows.Count - 1;
                        //}
                        if (i == row)
                        {
                            continue;
                        }
                        var num = matrix[i][col];
                        if (num != 0)
                        {
                            for (var j = 0; j < cols; j++)
                            {
                                matrix[i][j] -= matrix[row][j]*num;
                            }
                            //var newRowsAgain = SeparateRow(matrix[i]).ToList();
                            //if (newRowsAgain.Count > 1)
                            //{
                            //    splitsMade = true;
                            //    matrix.RemoveAt(i);
                            //    foreach (var newRow in newRowsAgain)
                            //    {
                            //        matrix.Insert(i, newRow);
                            //    }
                            //    rows += newRowsAgain.Count - 1;
                            //    i += newRowsAgain.Count - 1;
                            //}
                        }
                    }
                }

                //matrix = matrix.Where(x => Array.FindIndex(x, y => y != 0) != -1).OrderBy(x => Array.FindIndex(x, y => y != 0)).ToList();
            }
            var cellsToRemove = new List<RowSeparationResult>();
            var rowList = new List<int[]>();
            for (int i = 0; i < matrix.Count; i++)
            {
                var row = matrix[i];
                var rowResults = SeparateRow(row).ToList();
                if (rowResults.Count > 0)
                {
                    //splitsMade = true;
                    cellsToRemove.AddRange(rowResults);
                }
                else
                {
                    rowList.Add(row);
                }
            }
            var columnsToRemove = new HashSet<int>(cellsToRemove.Select(x => x.ColumnIndex));
            foreach (var separationResult in cellsToRemove)
            {
                var col = separationResult.ColumnIndex;
                for (int i = 0; i < matrix.Count; i++)
                {
                    var num = matrix[i][col];
                    if (num != 0)
                    {
                        //matrix[i][col] -= matrix[row][col]*num;
                        matrix[i][matrix[0].Length-1] -= separationResult.Constant * num;
                    }
                }
                allVerdicts[coordinates[col]] = separationResult.Constant == 1;
            }
            for (int i = 0; i < rowList.Count; i++)
            {
                rowList[i] = rowList[i].Where((x, index) => !columnsToRemove.Contains(index)).ToArray();
            }
            matrix = rowList.Where(x => Array.FindIndex(x, y => y != 0) != -1).OrderBy(x => Array.FindIndex(x, y => y != 0)).ToList();
            coordinates = coordinates.Where((x, i) => !columnsToRemove.Contains(i)).ToList();
            //#if DEBUG
            //            Debug.WriteLine(MatrixToString(matrix));
            //#endif
            if (matrix.Count == 0)
            {
                return;
            }
            if (splitsMade)
            {
                ReduceMatrix(ref coordinates, ref matrix, allVerdicts, parameters);
            }
        }

        private struct RowSeparationResult
        {
            public RowSeparationResult(int columnIndex, int constant)
            {
                ColumnIndex = columnIndex;
                Constant = constant;
            }

            public int ColumnIndex { get; }
            public int Constant { get; }
        }

        private IEnumerable<RowSeparationResult> SeparateRow(int[] row)
        {
            var constantIndex = row.Length - 1;
            var constant = row[constantIndex];

            /*if (constant < 0)
            {
                for (var i = 0; i < row.Length; i++)
                {
                    row[i] = -row[i];
                }
            }*/

            var positiveSum = 0;
            var negativeSum = 0;
            for (int i = 0; i < row.Length - 1; i++)
            {
                var num = row[i];
                if (num == 0)
                {
                    continue;
                }
                if (num > 0)
                {
                    positiveSum += num;
                }
                else
                {
                    negativeSum += num;
                }
            }

            if (constant == positiveSum || constant == negativeSum)
            {
                int forPositive;
                int forNegative;
                if (constant == positiveSum)
                {
                    forPositive = 1;
                    forNegative = 0;
                }
                else
                {
                    forPositive = 0;
                    forNegative = 1;
                }
                for (var i = 0; i < row.Length - 1; i++)
                {
                    if (row[i] != 0)
                    {
                        var newConstant = row[i] > 0 ? forPositive : forNegative;
                        yield return new RowSeparationResult(i, newConstant);
                    }
                }
                //splitsMade = true;
            }
        }


        private bool KindaEquals(double d, double e) => Math.Abs(d - e) < 0.0000001;

        private string MatrixToString(IList<int[]> matrix)
        {
            if (matrix.Count == 0)
            {
                return "Matrix empty" + Environment.NewLine;
            }
            var sb = new StringBuilder();
            for (int i = 0; i < matrix[0].Length-1; i++)
            {
                sb.Append(i.ToString().PadLeft(3, ' '));
            }
            sb.AppendLine("  |  C");
            for (int i = 0; i < matrix[0].Length - 1; i++)
            {
                sb.Append("---");
            }
            sb.AppendLine("--+---");
            for (int i = 0; i < matrix.Count; i++)
            {
                for (int j = 0; j < matrix[i].Length - 1; j++)
                {
                    sb.Append(matrix[i][j].ToString(CultureInfo.InvariantCulture).PadLeft(3, ' '));
                }
                sb.Append("  |");
                sb.AppendLine(matrix[i][matrix[i].Length-1].ToString(CultureInfo.InvariantCulture).PadLeft(3, ' '));
            }
            return sb.ToString();
        }

        public IList<int[]> CloneMatrix(IList<int[]> matrix)
        {
            return matrix.Select(x => (int[]) x.Clone()).ToList();
        }
    }
}
