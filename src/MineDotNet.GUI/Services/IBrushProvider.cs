using System.Collections.Generic;
using System.Drawing;

namespace MineDotNet.GUI.Services
{
    internal interface IBrushProvider
    {
        IReadOnlyList<SolidBrush> Brushes { get; }
        SolidBrush EmptyBrush { get; }
    }
}