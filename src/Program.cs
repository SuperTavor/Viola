
namespace Viola;
using System.Text;
using Viola.src.GUI.Logic.Windows;
using Viola.src.CLI.Logic;
using Viola.src.Launcher.Logic;
using Gtk;
class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        if (args.Length > 0 && args[0]=="gui")
        {
            Application.Init();
            //create instance of the main window so Application.Run() can just load it.
            new CMainWindow();
            //add a css provider from the built in resources.
            var cssProvider = new CssProvider();
            cssProvider.LoadFromData(
              @"
            @import url('resource:///org/gtk/libgtk/theme/Adwaita/gtk-contained-dark.css');
          "
            );
            StyleContext.AddProviderForScreen(Gdk.Screen.Default, cssProvider, 800);
            Application.Run();
        }
        else
        {
            CCLI cliManager = new CCLI(args);
            var parsedArguments = cliManager.ParseArguments();
            CLauncher launcher = new CLauncher(parsedArguments);
            launcher.Launch();
        }
    }
}