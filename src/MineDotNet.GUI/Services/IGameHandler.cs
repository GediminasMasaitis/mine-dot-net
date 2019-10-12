using System;
using System.Windows.Forms;
using MineDotNet.Common;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Services
{
    internal interface IGameHandler
    {
        event EventHandler<CellClickEventArgs> CellClick;

        Map Map { get; set; }
        PictureBox Target { get; set; }
    }
}