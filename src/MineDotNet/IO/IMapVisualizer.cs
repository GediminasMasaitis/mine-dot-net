using System.IO;
using MineDotNet.Common;

namespace MineDotNet.IO
{
    public interface IMapVisualizer
    {
        void Visualize(IMap map, Stream outputStream);
    }
}