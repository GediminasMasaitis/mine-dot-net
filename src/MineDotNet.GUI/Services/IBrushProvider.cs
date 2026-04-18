using System.Collections.Generic;
using System.Drawing;

namespace MineDotNet.GUI.Services
{
    internal interface IBrushProvider
    {
        // Semi-transparent fills used on top of revealed cells in DisplayService.
        IReadOnlyList<SolidBrush> Brushes { get; }

        // Opaque, high-contrast equivalents used for mask text/labels where
        // transparency would wash out against a dark background.
        IReadOnlyList<Color> LabelColors { get; }

        SolidBrush EmptyBrush { get; }
    }
}
