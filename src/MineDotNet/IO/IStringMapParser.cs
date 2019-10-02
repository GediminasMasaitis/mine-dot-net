using MineDotNet.Common;

namespace MineDotNet.IO
{
    public interface IStringMapParser : IMapParser
    {
        Map Parse(string str);
    }
}