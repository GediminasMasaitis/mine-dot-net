namespace MineDotNet.Common
{
    public enum CellFlag : byte
    {
        None = 0,
        HasMine = 1,
        DoesntHaveMine = 2,
        NotSure = 3
    }
}