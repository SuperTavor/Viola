using Viola.Core.Dump.Logic;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Merge.Logic;
using Viola.Core.Pack.Logic;
using Viola.Core.EncryptDecrypt.Logic;
using Viola.Core.ViolaLogger.Logic;
namespace Viola.Core.Launcher.Logic;

public class CLauncher
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
            {Mode.Decrypt,new BeginMode(BeginDecrypt) },
            {Mode.Encrypt,new BeginMode(BeginEncrypt) },
        };
    }
    public void Launch()
    {
        _modeFuncs[_options.Mode]();
        CLogger.InvokeImportantInfos();
        CLogger.LogInfo("Finished");
    }

    public async Task LaunchAsync()
    {
        await Task.Run(() => _modeFuncs[_options.Mode]());
        CLogger.InvokeImportantInfos();
        CLogger.LogInfo("Finished");
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