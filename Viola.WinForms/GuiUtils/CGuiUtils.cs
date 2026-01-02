using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viola.WinForms.GuiUtils
{
    public class CGuiUtils
    {

        public static string ChooseFolder(string desc, string initialDirectory = "")
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = desc;
                if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                {
                    folderDialog.InitialDirectory = initialDirectory;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;

                    return selectedPath;
                }
                else return string.Empty;
            }
        }

        public static string SaveFile(string windowTitle, string filter, string initialDirectory = "")
        {
            return ChooseFile(windowTitle, true, filter, initialDirectory);
        }

        public static string ChooseExistingFile(string windowTitle, string fileExtension, string initialDirectory = "")
        {
            return ChooseFile(windowTitle, false, fileExtension, initialDirectory);
        }
        public static string ChooseFile(string windowTitle, bool isSaveDialog, string filter, string initialDirectory = "")
        {
            if (isSaveDialog)
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Title = windowTitle;
                    //Saving teh hashcache is the only time we actually save a file so it's OK to put this filter every time we save
                    saveFileDialog.Filter = filter;
                    if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                    {
                        saveFileDialog.InitialDirectory = initialDirectory;
                    }

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        return saveFileDialog.FileName;
                    }
                }
            }
            else
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = windowTitle;
                    openFileDialog.Filter = filter;
                    if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                    {
                        openFileDialog.InitialDirectory = initialDirectory;
                    }

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        return openFileDialog.FileName;
                    }
                }
            }

            return string.Empty;
        }
    }
}
