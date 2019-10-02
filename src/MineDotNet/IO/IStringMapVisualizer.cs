using MineDotNet.Common;

namespace MineDotNet.IO
{
    public interface IStringMapVisualizer : IMapVisualizer
    {
        string VisualizeToString(IMap map);
        string VisualizeCell(Cell cell);
    }
}