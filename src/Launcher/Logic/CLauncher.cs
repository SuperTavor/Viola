using Viola.Dump;
using Viola.CLI;
using Viola.Pack;
using Viola.Merge;

namespace Viola.Launcher;

class CLauncher
{
    public delegate void BeginMode();
    private CParsedArguments _options;

    private Dictionary<eModes, BeginMode> _modeFuncs;
    public CLauncher(CParsedArguments parsedArgs)
    {
        _options = parsedArgs;
    }
    public void Launch()
    {
        _modeFuncs = new()
        {
            {eModes.Pack, new BeginMode(BeginPack) },
            {eModes.Merge, new BeginMode(BeginMerge) },
            {eModes.Dump, new BeginMode(BeginDump) }
        };
        _modeFuncs[_options.Mode]();
        Console.WriteLine("Finished.");
    }
    void BeginPack()
    {
        CPack packer = new CPack(_options);
        packer.Pack();
    }
    void BeginMerge()
    {
        CMerge merger = new CMerge(_options);
        merger.Merge();
    }

    void BeginDump()
    {
        CDump dumper = new CDump(_options);
        dumper.Dump();
    }
}