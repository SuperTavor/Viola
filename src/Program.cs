using Viola.CLINS;
using Viola.NSLauncher;
namespace Viola;
using System.Text;
class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        CLI cliManager = new CLI(args);
        var parsedArguments = cliManager.ParseArguments();
        Launcher launcher = new Launcher(parsedArguments);
        launcher.Launch();
    }
}