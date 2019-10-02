using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Services
{
    internal interface IDisplayService : IDisposable
    {
        event EventHandler<CellClickEventArgs> CellClick;
        bool DrawCoordinates { get; set; }
        
        void SetTarget(PictureBox target);
        void DisplayMap(Map map, IList<Mask> masks, IDictionary<Coordinate, SolverResult> results = null);
    }
}