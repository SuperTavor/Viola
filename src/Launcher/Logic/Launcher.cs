using Viola.DumpNS;
using Viola.CLINS;
using Viola.PackNS;
using Viola.MergeNS;
using Viola.HashCacheNS;
namespace Viola.NSLauncher;

class Launcher
{
    public delegate void BeginMode();
    private ParsedArguments _options;

    private Dictionary<eMode, BeginMode> _modeFuncs;
    public Launcher(ParsedArguments parsedArgs)
    {
        _options = parsedArgs;
        _modeFuncs = new()
        {
            {eMode.Pack, new BeginMode(BeginPack) },
            {eMode.Merge, new BeginMode(BeginMerge) },
            {eMode.Dump, new BeginMode(BeginDump) },
            {eMode.Cache,new BeginMode(BeginCache) },
        };
    }
    public void Launch()
    {
        _modeFuncs[_options.Mode]();
        Console.WriteLine("Finished.");
    }
    void BeginPack()
    {
        Pack packer = new Pack(_options);
        packer.PackMod();
    }
    void BeginMerge()
    {
        Merge merger = new Merge(_options);
        merger.MergeMods();
    }

    void BeginDump()
    {
        Dump dumper = new Dump(_options);
        dumper.DumpRomfs();
    }

    void BeginCache()
    {
        HashCache cache = new HashCache(_options.InputPath, eHashCacheMode.Create,_options.OutputPath);
        cache.Save();
    }
}