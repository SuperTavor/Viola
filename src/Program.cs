using Viola.CLI;
using Viola.Launcher;
namespace Viola;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        //register SHIFT-JIS
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        CCLI cliManager = new CCLI(args);
        var pArgs = cliManager.ParseArguments();
        CLauncher launcher = new CLauncher(pArgs);
        launcher.Launch();
    }
}