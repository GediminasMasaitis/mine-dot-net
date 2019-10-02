using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Services
{
    class MaskConverter : IMaskConverter
    {
        public Mask ConvertToMask(Map map)
        {
            var maskMap = new Mask(map.Width, map.Height);
            foreach (var cell in map.AllCells)
            {
                maskMap.Cells[cell.X, cell.Y] = cell.State == CellState.Filled;
            }
            return maskMap;
        }

        public IEnumerable<Mask> ConvertToMasks(IEnumerable<Map> maps)
        {
            return maps.Select(ConvertToMask);
        }

        public Map ConvertToMap(Mask mask)
        {
            var map = new Map(mask.Width, mask.Height, 0, true);
            for (int i = 0; i < mask.Width; i++)
            {
                for (int j = 0; j < mask.Height; j++)
                {
                    if (mask.Cells[i, j])
                    {
                        map.Cells[i, j].State = CellState.Filled;
                    }
                }
            }

            return map;
        }

        public IEnumerable<Map> ConvertToMaps(IEnumerable<Mask> masks)
        {
            return masks.Select(ConvertToMap);
        }

        public Mask ConvertToMask(IDictionary<Coordinate, SolverResult> results, bool targetVerdict, int width, int height)
        {
            var mask = new Mask(width, height);
            foreach (var result in results)
            {
                if (result.Value.Verdict == targetVerdict)
                {
                    mask.Cells[result.Key.X, result.Key.Y] = true;
                }
            }
            return mask;
        }
    }
}
