using System;
using System.Drawing;
using System.Windows.Forms;
using MineDotNet.Common;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Services
{
    class GameHandler : IGameHandler
    {
        public event EventHandler<CellClickEventArgs> CellClick;

        public PictureBox Target
        {
            get => _target;
            set
            {
                if (_target != null)
                {
                    _target.MouseUp -= TargetOnClick;
                }

                _target = value;
                _target.MouseUp += TargetOnClick;
            }
        }

        public Map Map { get; set; }
        
        private readonly ICellLocator _cellLocator;

        private PictureBox _target;

        public GameHandler(ICellLocator cellLocator)
        {
            _cellLocator = cellLocator;
        }

        private void TargetOnClick(object sender, MouseEventArgs eventArgs)
        {
            if (!_target.Bounds.Contains(eventArgs.Location))
            {
                return;
            }

            var size = _cellLocator.GetCellSize(Map, _target.Size);
            var coordinate = _cellLocator.GetCellCoordinate(eventArgs.Location, size);
            var args = new CellClickEventArgs(coordinate, eventArgs.Button);
            CellClick?.Invoke(this, args);
        }
    }
}