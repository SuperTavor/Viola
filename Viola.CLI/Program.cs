using System.Text;
using Viola.CLI.CLI.Logic;
using Viola.Core.Launcher.Logic;
class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        CCLI cliManager = new CCLI(args);
        var parsedArguments = cliManager.ParseArguments();
        CLauncher launcher = new CLauncher(parsedArguments);
        launcher.Launch();
    }
}
