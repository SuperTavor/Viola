using Viola.src.Dump.Logic;
using Viola.src.Launcher.DataClasses;
using Viola.src.Merge.Logic;
using Viola.src.HashCache.DataClasses;
using Viola.src.HashCache.Logic;
using Viola.src.Pack.Logic;
using Viola.src.EncryptDecrypt.Logic;
namespace Viola.src.Launcher.Logic;

class CLauncher
{
    public delegate void BeginMode();
    private CLaunchOptions _options;

    private Dictionary<Mode, BeginMode> _modeFuncs;
    public CLauncher(CLaunchOptions parsedArgs)
    {
        _options = parsedArgs;
        _modeFuncs = new()
        {
            {Mode.Pack, new BeginMode(BeginPack) },
            {Mode.Merge, new BeginMode(BeginMerge) },
            {Mode.Dump, new BeginMode(BeginDump) },
            {Mode.Cache,new BeginMode(BeginCache) },
            {Mode.Decrypt,new BeginMode(BeginDecrypt) },
            {Mode.Encrypt,new BeginMode(BeginEncrypt) },
        };
    }
    public void Launch()
    {
        _modeFuncs[_options.Mode]();
        Console.WriteLine("Finished.");
    }

    public async Task LaunchAsync()
    {
        await Task.Run(() => _modeFuncs[_options.Mode]());
        Console.WriteLine("Finished.");
    }

    void BeginPack()
    {
        CPack packer = new CPack(_options);
        packer.PackMod();
    }
    void BeginMerge()
    {
        CMerge merger = new CMerge(_options);
        merger.MergeMods();
    }

    void BeginDump()
    {
        CDump dumper = new CDump(_options);
        dumper.DumpRomfs();
    }

    void BeginCache()
    {
        CHashCache cache = new CHashCache(_options.InputPath, HashCacheMode.Create, _options.OutputPath);
        cache.Save();
    }

    void BeginEncrypt()
    {
        CEnc encryptor = new CEnc(_options);
        encryptor.Encrypt();
    }

    void BeginDecrypt()
    {
        CDec decryptor = new CDec(_options);
        decryptor.Decrypt();
    }
}