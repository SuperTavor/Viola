using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viola.src.GUI.Logic
{
    public class CGuiUtils
    {
        public static void ShowMsgBox(string message)
        {
            MessageDialog md = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok, message);
            md.SetPosition(WindowPosition.Center);
            md.Run();
            md.Destroy();
        }

        public static string ChooseFolder(Window window,string windowTitle)
        {
            FileChooserDialog fc = new(
                       windowTitle,
                       window,
                       FileChooserAction.SelectFolder,
                       "Cancel", ResponseType.Cancel,
                       "Open", ResponseType.Accept
            );
            string fname = "";

            if (fc.Run() == (int)ResponseType.Accept)
            {
                fname = fc.Filename;
            }
            else
            {
                fname = "";
            }
            fc.Destroy();
            return fname;
        }

        public static string ChooseNewFile(Window window, string windowTitle)
        {
            return ChooseFile(window, windowTitle, FileChooserAction.Save);
        }

        public static string ChooseExistingFile(Window window, string windowTitle)
        {
            return ChooseFile(window,windowTitle, FileChooserAction.Open);
        }
        private static string ChooseFile(Window window, string windowTitle,FileChooserAction action)
        {
            FileChooserDialog fc = new(
                      windowTitle,
                      window,
                        action,
                      "Cancel", ResponseType.Cancel,
                      "Save", ResponseType.Accept
           );
            string fname = "";

            if (fc.Run() == (int)ResponseType.Accept)
            {
                fname = fc.Filename;
            }
            else
            {
                fname = "";
            }
            fc.Destroy();
            return fname;
        }
    }
}
