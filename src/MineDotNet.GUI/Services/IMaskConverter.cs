using System.Collections.Generic;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Services
{
    internal interface IMaskConverter
    {
        Mask ConvertToMask(Map map);
        Mask ConvertToMask(IDictionary<Coordinate, SolverResult> results, bool targetVerdict, int width, int height);
        IEnumerable<Mask> ConvertToMasks(IEnumerable<Map> maps);
        Map ConvertToMap(Mask mask);
        IEnumerable<Map> ConvertToMaps(IEnumerable<Mask> masks);
    }
}