using System.IO;
using MineDotNet.Common;

namespace MineDotNet.IO
{
    public interface IMapParser
    {
        Map Parse(Stream stream);
    }
}