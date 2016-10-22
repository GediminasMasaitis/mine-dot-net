using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MineDotNet.Common
{
    public class TextMapParser
    {
        public TextMapParser()
        {
            
        }

        public Map Parse(Stream stream)
        {
            var reader = new StreamReader(stream);
            var cells = new List<Cell>();
            var lines = new List<string>();
            while (!reader.EndOfStream)
            {
                lines.Add(reader.ReadLine());
            }

            for (var i = 0; i < lines.Count; i++)
            {
                for (var j = 0; j < lines[i].Length; j++)
                {
                    Cell cell = null;
                    switch (lines[i][j])
                    {
                        case '.':
                            cell = new Cell(i, j, CellState.Empty, CellFlag.None, 0);
                            break;
                        case '#':
                            cell = new Cell(i, j, CellState.Filled, CellFlag.None, 0);
                            break;
                        case '!':
                            cell = new Cell(i, j, CellState.Filled, CellFlag.HasMine, 0);
                            break;
                        default:
                            int hint;
                            var success = int.TryParse(lines[i][j].ToString(), out hint);
                            if (success)
                            {
                                cell = new Cell(i, j, CellState.Empty, CellFlag.None, hint);
                            }
                            else
                            {
                                throw new InvalidDataException("Char " + lines[i][j] + " not recognised at line " + i + ", char " + j);
                            }
                            break;
                    }
                    cells.Add(cell);
                }
            }
            var map = new Map(cells);
            return map;
        }
    }
}
