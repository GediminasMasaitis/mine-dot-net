using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Services
{
    internal interface IDisplayService
    {
        PictureBox Target { get; set; }
        bool DrawCoordinates { get; set; }
        
        void DisplayMap(Map map, IList<Mask> masks, IDictionary<Coordinate, SolverResult> results = null);
    }
}