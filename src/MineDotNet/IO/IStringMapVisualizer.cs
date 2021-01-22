using System.IO;
using MineDotNet.Common;

namespace MineDotNet.IO
{
    public interface IStringMapVisualizer : IMapVisualizer
    {
        void Visualize(IMap map, Stream outputStream, bool multiline = true);
        string VisualizeToString(IMap map, bool multiline = true);
        string VisualizeCell(Cell cell);
    }
}