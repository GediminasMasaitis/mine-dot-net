using System.Collections.Generic;
using System.Windows.Media;
using MineDotNet.Common;

namespace MineDotNet.GUI.Services
{
    internal interface ITileSource
    {
        IReadOnlyDictionary<int, ImageSource> Hints { get; }
        IReadOnlyDictionary<CellState, ImageSource> States { get; }
        IReadOnlyDictionary<CellFlag, ImageSource> Flags { get; }
        ImageSource UnrevealedMine { get; }
    }
}
