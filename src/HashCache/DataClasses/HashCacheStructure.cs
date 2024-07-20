namespace Viola.src.HashCache.DataClasses;

public struct HashCacheStructure
{
    public Dictionary<string, uint> Hashes;
    public HashCacheStructure()
    {
        Hashes=new Dictionary<string, uint>();
    }
}