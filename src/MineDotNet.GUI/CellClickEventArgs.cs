using System;
using System.Windows.Forms;
using MineDotNet.Common;

namespace MineDotNet.GUI
{
    public class CellClickEventArgs : EventArgs
    {
        public CellClickEventArgs(Coordinate coordinate, MouseButtons buttons)
        {
            Coordinate = coordinate;
            Buttons = buttons;
        }

        public Coordinate Coordinate { get; }
        public MouseButtons Buttons { get; }
    }
}