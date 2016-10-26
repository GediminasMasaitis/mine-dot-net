using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MineDotNet.Common
{
    public class TextMapVisualizer
    {
        public void Visualize(Map map, Stream outputStream)
        {
            var writer = new StreamWriter(outputStream);
            for (var i = 0; i < map.Width; i++)
            {
                for (var j = 0; j < map.Height; j++)
                {
                    var cell = map.Cells[i, j];
                    switch (cell?.State)
                    {
                        case null:
                            writer.Write(" ");
                            break;
                        case CellState.Empty:
                            if (cell.Hint != 0)
                            {
                                writer.Write(cell.Hint.ToString());
                            }
                            else
                            {
                                writer.Write(".");
                            }
                            break;
                        case CellState.Filled:
                            switch (cell.Flag)
                            {
                                case CellFlag.None:
                                    writer.Write("#");
                                    break;
                                case CellFlag.HasMine:
                                    writer.Write("!");
                                    break;
                                case CellFlag.NotSure:
                                    writer.Write("?");
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case CellState.Wall:
                            writer.Write("X");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                writer.WriteLine();
            }
            writer.Flush();
        }
    }
}
