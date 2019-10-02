using System;
using System.IO;
using System.Text;
using MineDotNet.Common;

namespace MineDotNet.IO
{
    public class TextMapVisualizer : IStringMapVisualizer
    {
        public string VisualizeToString(IMap map)
        {
            using (var ms = new MemoryStream())
            {
                Visualize(map, ms);
                var str = Encoding.ASCII.GetString(ms.ToArray());
                return str;
            }
        }

        public void Visualize(IMap map, Stream outputStream)
        {
            var writer = new StreamWriter(outputStream);
            for (var i = 0; i < map.Width; i++)
            {
                for (var j = 0; j < map.Height; j++)
                {
                    var cell = map.Cells[i, j];
                    var cellStr = VisualizeCell(cell);
                    writer.Write(cellStr);
                }
                writer.WriteLine();
            }
            if (map.RemainingMineCount.HasValue)
            {
                writer.WriteLine($"m{map.RemainingMineCount.Value}");
            }
            writer.Flush();
        }

        public string VisualizeCell(Cell cell)
        {
            switch (cell?.State)
            {
                case null:
                    return " ";
                case CellState.Empty:
                    if (cell.Hint != 0)
                    {
                        return cell.Hint.ToString();
                    }
                    else
                    {
                        return ".";
                    }
                case CellState.Filled:
                    switch (cell.Flag)
                    {
                        case CellFlag.None:
                            return "#";
                        case CellFlag.HasMine:
                            return "!";
                        case CellFlag.DoesntHaveMine:
                            return "v";
                        case CellFlag.NotSure:
                            return "?";
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case CellState.Wall:
                    return "X";
                case CellState.Mine:
                    return "*";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
