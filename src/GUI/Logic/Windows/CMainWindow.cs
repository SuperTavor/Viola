using System;
using Gtk;
using Viola.src.Launcher.DataClasses;
using Viola.src.Launcher.Logic;
using Viola.src.Utils.General.Logic;

namespace Viola.src.GUI.Logic.Windows
{
    public class CMainWindow : Window
    {
        private const string PAY_ATTENTION_TO_CMD_TEXT = "Operation will be started as soon as this message box is dismissed. Pay attention to the command prompt window for a log of what's happening.";
        public CMainWindow() : base($"Viola {CGeneralUtils.APP_VERSION} (GUI)")
        {
            SetDefaultSize(400, 400);
            SetPosition(WindowPosition.Center);

            DeleteEvent += delegate { Application.Quit(); };

            Grid grid = new Grid();
            grid.RowSpacing = 5;
            grid.ColumnSpacing = 5;

            Button btnPack = new Button("Pack");
            Button btnDump = new Button("Dump");
            Button btnCache = new Button("Cache");
            Button btnMerge = new Button("Merge");
            Button btnEnc = new Button("Encrypt");
            Button btnDec = new Button("Decrypt");
            grid.Attach(btnPack, left: 0, top: 0, width: 1, height: 1);
            grid.Attach(btnDump, left: 1, top: 0, width: 1, height: 1);
            grid.Attach(btnCache, left: 0, top: 1, width: 1, height: 1);
            grid.Attach(btnMerge, left: 1, top: 1, width: 1, height: 1);
            grid.Attach(btnEnc, left: 0, top: 2, width: 1, height: 1);
            grid.Attach(btnDec, left: 1, top: 2, width: 1, height: 1);
            btnPack.Hexpand = true;
            btnPack.Vexpand = true;
            btnDump.Hexpand = true;
            btnDump.Vexpand = true;
            btnCache.Hexpand = true;
            btnCache.Vexpand = true;
            btnMerge.Hexpand = true;
            btnMerge.Vexpand = true;
            btnMerge.Clicked += btnMerge_Clicked;
            btnPack.Clicked += btnPack_Clicked;
            btnDump.Clicked += btnDump_Clicked;
            btnCache.Clicked += btnCache_Clicked;
            btnEnc.Clicked += btnEnc_Clicked;
            btnDec.Clicked += btnDec_Clicked;
            Add(grid);

            ShowAll();
        }
        private void btnDec_Clicked(object obf, EventArgs args)
        {
            var options = new CLaunchOptions();
            options.Mode = Mode.Decrypt;
            var fileToDecrypt = CGuiUtils.ChooseExistingFile(this, "Choose the file you want to decrypt");
            if(fileToDecrypt == null )
            {
                CGuiUtils.ShowMsgBox("Operation cancelled");
                return;
            }
            var destiniation = CGuiUtils.ChooseNewFile(this, "Choose where you want to save the decrypted output to.");
            if( destiniation == null )
            {
                CGuiUtils.ShowMsgBox("Operation cancelled");
                return;
            }
            options.InputPath = fileToDecrypt;
            options.OutputPath = destiniation;
            var launcher = new CLauncher(options);
            CGuiUtils.ShowMsgBox(PAY_ATTENTION_TO_CMD_TEXT);
            Application.Quit();
            launcher.Launch();
        }
        private void btnEnc_Clicked(object obf, EventArgs args)
        {
            var options = new CLaunchOptions();
            options.Mode = Mode.Encrypt;
            var key = CInputDialog.Show("Please enter an encryption key.");
            try
            {
                options.Key = uint.Parse(key.Trim());
            }
            catch
            {
                CGuiUtils.ShowMsgBox("Please enter a valid uint as the key.");
                return;
            }
            var fileToEncrypt = CGuiUtils.ChooseExistingFile(this, "Please choose the file you wish to encrypt.");
            if(fileToEncrypt == null)
            {
                CGuiUtils.ShowMsgBox("Operation cancelled by user.");
                return;
            }
            var savePath = CGuiUtils.ChooseNewFile(this, "Please choose the path where you want to save the encrypted file to.");
            if (savePath == null)
            {
                CGuiUtils.ShowMsgBox("Operation cancelled by user.");
                return;
            }
            options.InputPath = fileToEncrypt;
            options.OutputPath = savePath;
            var launcher = new CLauncher(options);
            CGuiUtils.ShowMsgBox(PAY_ATTENTION_TO_CMD_TEXT);
            Application.Quit();
            launcher.Launch();
        }

        private void btnCache_Clicked(object obj, EventArgs args)
        {
            CommonMode(Mode.Cache, true,"Select the folder to cache (Note: selected folder has to contain the data folder directly.)","Select the folder to put the cached output in.");
        }
        private void btnDump_Clicked(object obj, EventArgs args)
        {
            CommonMode(Mode.Dump,false,"Select the directory to dump","Select the directory to put your dump in");
        }
        private void btnPack_Clicked(object obj, EventArgs args)
        {
            CommonMode(Mode.Pack,false,"Select folder to pack","Select folder to put the packed mod in");
        }

        //Used for dump,pack and cache
        private void CommonMode(Mode mode, bool saveFile,string inputFileMessage, string outputFileMessage)
        {
            var options = new CLaunchOptions();
            options.Mode = mode;
            var inputDir = CGuiUtils.ChooseFolder(this, inputFileMessage);
            if (inputDir == string.Empty)
            {
                CGuiUtils.ShowMsgBox("Operation cancelled by user.");
                return;
            }
            string outputDir = "";
            if(saveFile)
            {
                outputDir = CGuiUtils.ChooseNewFile(this, outputFileMessage);
            }
            else outputDir = CGuiUtils.ChooseFolder(this, outputFileMessage);

            if (outputDir == string.Empty)
            {
                CGuiUtils.ShowMsgBox("Operation cancelled by user.");
                return;
            }
            //put the paths into the options
            options.InputPath = inputDir;
            options.OutputPath = outputDir;

            var launcher = new CLauncher(options);
            CGuiUtils.ShowMsgBox(PAY_ATTENTION_TO_CMD_TEXT);
            Application.Quit();
            launcher.Launch();
        }
        private void btnMerge_Clicked(object obj, EventArgs args)
        {
            string modCountStr = CInputDialog.Show("How many mods would you like to merge?").Trim();
            if (string.IsNullOrEmpty(modCountStr))
            {
                CGuiUtils.ShowMsgBox("Field was empty/operation cancelled by user.");
            }
            else
            {
                uint modCount;
                try
                {
                    modCount = uint.Parse(modCountStr);
                }
                catch
                {
                    CGuiUtils.ShowMsgBox("Please enter a valid uint32.");
                    return;
                }

                List<string> pathsToMerge = new List<string>();
                bool isSuccuss = true;
                //Populate the list by opening folder select dialogs.
                for (int i = 0; i < modCount; i++)
                {
                    var path = CGuiUtils.ChooseFolder(this,$"Choose mod to add to merge pool at priority num. {i + 1}");
                    if(path == string.Empty)
                    {
                        isSuccuss = false;
                        break;
                    }
                    else
                    {
                        pathsToMerge.Add(path);
                    }
                }

                if (!isSuccuss)
                {
                    CGuiUtils.ShowMsgBox("Something went wrong when choosing files. Cancelling merge operation");
                    return;
                }
                //construct LaunchOptions.
                var options = new CLaunchOptions();
                options.Mode = Mode.Merge;
                options.StuffToMerge = pathsToMerge;
                var outputPath = CGuiUtils.ChooseFolder(this,"Choose the output path");
                if(outputPath == string.Empty)
                {
                    CGuiUtils.ShowMsgBox("Something went wrong when choosing files. Cancelling merge operation");
                    return;
                }
                else
                {
                    options.OutputPath = outputPath;
                }
                var launcher = new CLauncher(options);
                CGuiUtils.ShowMsgBox(PAY_ATTENTION_TO_CMD_TEXT);
                Application.Quit();
                launcher.Launch();
            }

        }


    }
}
