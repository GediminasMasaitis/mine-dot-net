using System;
using System.ComponentModel;
using System.Windows.Controls;
using MineDotNet.GUI.Services;

namespace MineDotNet.GUI.Controls
{
    // XAML-friendly wrapper so MainWindow can drop the board in with a parameterless
    // ctor while the board itself still pulls its dependencies from the IoC container.
    // Exposes the inner board via Board once loaded.
    internal sealed class MinesweeperBoardHost : ContentControl
    {
        public MinesweeperBoard Board { get; private set; }

        public MinesweeperBoardHost()
        {
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
            VerticalContentAlignment = System.Windows.VerticalAlignment.Stretch;
            if (DesignerProperties.GetIsInDesignMode(this)) return;
            var tiles = IOCC.GetService<ITileSource>();
            var palette = IOCC.GetService<IPaletteProvider>();
            Board = new MinesweeperBoard(tiles, palette);
            Content = Board;
        }
    }
}
