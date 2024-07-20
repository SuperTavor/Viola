using Gtk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viola.src.GUI.Logic.Windows
{
    public class CInputDialog
    {
        public static string Show(string message)
        {
            Dialog dialog = new Dialog("Viola", null, DialogFlags.Modal);
            dialog.SetDefaultSize(300, 100);
            dialog.BorderWidth = 10;
            dialog.Resizable = false;
            dialog.SetPosition(WindowPosition.Center);
            Box contentArea = dialog.ContentArea;
            //10 so they are not extremely crammed together
            contentArea.Spacing = 10;
            Label label = new Label(message);
            contentArea.Add(label);

            Entry inputField = new Entry();
            contentArea.Add(inputField);

            dialog.AddButton("OK", ResponseType.Ok);
            dialog.AddButton("Cancel", ResponseType.Cancel);
            dialog.ShowAll();

            ResponseType response = (ResponseType)dialog.Run();

            string result = string.Empty;
            if (response == ResponseType.Ok)
            {
                result = inputField.Text;
            }

            dialog.Destroy();
            return result;

        }
    }
}
