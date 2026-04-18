using System.Collections.Generic;
using System.Windows.Media;

namespace MineDotNet.GUI.Services
{
    internal interface IPaletteProvider
    {
        // Opaque mask colors used for text/labels on dark backgrounds.
        IReadOnlyList<Color> MaskColors { get; }

        // Translucent brushes layered over revealed cells in the board renderer.
        IReadOnlyList<Brush> MaskOverlayBrushes { get; }
    }
}
