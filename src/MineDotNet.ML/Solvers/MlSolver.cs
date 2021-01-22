using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ML;
using Microsoft.ML.Data;
using MineDotNet.Common;
using MineDotNet.Game.Models;

namespace MineDotNet.ML.Solvers
{
    public class MlSolver
    {
        public void Run(IEnumerable<GameMap> maps)
        {
            var datas = TransformMaps(maps).ToList();
            
            var context = new MLContext(0);
            var data = context.Data.LoadFromEnumerable(datas);
            var trainTest = context.Data.TrainTestSplit(data);

            var pipeline = context.Transforms.CopyColumns("Label", "Mine")
                .Append(context.Transforms.Concatenate("Features", "Hints", "Filled"))
                .Append(context.Regression.Trainers.FastForest());

            var model = pipeline.Fit(trainTest.TrainSet);

        }

        private IEnumerable<MlData> TransformMaps(IEnumerable<GameMap> maps)
        {
            foreach (var gameMap in maps)
            {
                yield return TransformMap(gameMap);
            }
        }

        private MlData TransformMap(GameMap map)
        {
            var data = new MlData();
            data.Hints = new float[MlConstants.SliceCellCount];
            data.Filled = new float[MlConstants.SliceCellCount];
            data.Mine = map[new Coordinate(2, 2)].HasMine ? 1f : 0f;

            var index = 0;
            foreach (var cell in map.AllCells)
            {
                data.Hints[index] = cell.Hint;
                data.Filled[index] = cell.State == CellState.Filled ? 1f : 0f;
                index++;
            }

            return data;
        }
    }

    public class MlData
    {
        [VectorType(MlConstants.SliceCellCount)]
        public float[] Hints { get; set; }
        
        [VectorType(MlConstants.SliceCellCount)]
        public float[] Filled { get; set; }

        public float Mine { get; set; }
    }

    public static class MlConstants
    {
        public const int SliceSize = 5;
        public const int SliceCellCount = SliceSize * SliceSize;
    }
}
