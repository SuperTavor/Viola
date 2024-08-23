using Viola.WinForms.GuiUtils;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Launcher.Logic;
using Viola.Core.Utils.General.Logic;
using Viola.Core.ViolaLogger.Logic;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Viola.Core.Pack.DataClasses;

namespace Viola.WinForms.Forms.MainForm
{
    public partial class MainForm : Form
    {
        //Buttons to be disabled during operations
        private List<Button> _btns;
        private bool _buttonsDisabled;
        public MainForm()
        {
            InitializeComponent();
            CGeneralUtils.isConsole = false;
            _btns = [packBtn, dumpBtn, mergeBtn, decryptBtn, encryptBtn];
            this.Text = $"Viola {CGeneralUtils.APP_VERSION} (GUI)";
            //to display all logs to the gui console
            CLogger.GuiLogInfoEvent += GuiLog;
            
        }
        private List<EventHandler> _oldButtonClicks = [];
        //Simulates the button being disables because the default disabled look is ugly as shit
        private void SensitiveAllBtns(bool disable)
        {
            foreach (var btn in _btns)
            {
                if (disable)
                {
                    btn.ForeColor = Color.DarkGray;
                    _buttonsDisabled = true;
                }
                else
                {
                    btn.ForeColor = Color.Black;
                    _buttonsDisabled = false;
                }
            }
        }

        private async void GuiLog(string msg)
        {
            if (consoleRichTextBox.InvokeRequired)
            {
                consoleRichTextBox.Invoke(new Action(() => GuiLog(msg)));
            }
            else
            {
                consoleRichTextBox.AppendText(msg);
                consoleRichTextBox.ScrollToCaret();
            }
        }

        private async void packBtn_Click(object sender, EventArgs e)
        {
            if (_buttonsDisabled)
            {
                return;
            }
            //Ask if the user wants to use an external cpklist
            DialogResult result = MessageBox.Show("Do you want to use an external, vanilla cpk_list? (Recommended)", "Confirmation", MessageBoxButtons.YesNo);
            string file = string.Empty;
            if (result == DialogResult.Yes)
            {
                file = CGuiUtils.ChooseExistingFile("Choose an external cpk_list file", "CfgBin file|*.bin");
                if (file == null)
                {
                    CLogger.AddImportantInfo("Operation cancelled by user.");
                    CLogger.InvokeImportantInfos();
                    SensitiveAllBtns(false);
                    return;
                }
            }
            var platformDialog = new ChoosePackPlatformForm();
            platformDialog.ShowDialog();
            Platform plat = 0;
            if(platformDialog.DialogResult == DialogResult.OK)
            {
                plat = platformDialog.Output;
            }
            else
            {
                MessageBox.Show("Operation cancelled by user.");
                SensitiveAllBtns(false);
                return;
            }
            await CommonMode(Mode.Pack, false, "Select folder to pack", "Select folder to put the packed mod in", file,"",plat,true);
        }

        private async Task CommonMode(Mode mode, bool saveFile, string inputFileMessage, string outputFileMessage, string specificCpkListPath = "",string saveFileFilter="",Platform targetPlat = 0,bool isUseTargetPlat = false)
        {
            if (_buttonsDisabled)
            {
                return;
            }
            SensitiveAllBtns(true);
            var options = new CLaunchOptions();
            if(isUseTargetPlat) options.PackPlatform = targetPlat;
            options.Mode = mode;
            var inputDir = CGuiUtils.ChooseFolder(inputFileMessage);
            if (inputDir == string.Empty)
            {
                MessageBox.Show("Operation cancelled by user.");
                SensitiveAllBtns(false);
                return;
            }
            string outputDir = "";
            if (saveFile)
            {
                outputDir = CGuiUtils.SaveFile(outputFileMessage,saveFileFilter);
            }
            else outputDir = CGuiUtils.ChooseFolder(outputFileMessage);

            if (outputDir == string.Empty)
            {
                MessageBox.Show("Operation cancelled by user.");
                SensitiveAllBtns(false);
                return;
            }
            //put the paths into the options
            options.InputPath = inputDir;
            options.OutputPath = outputDir;
            options.CpkListPath = specificCpkListPath;
            var launcher = new CLauncher(options);
            await launcher.LaunchAsync();
            SensitiveAllBtns(false);
            CLogger.InvokeImportantInfos();
        }

        private void quickStartLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/SuperTavor/Viola?tab=readme-ov-file#quickstart") { UseShellExecute = true });
        }

        private async void dumpBtn_Click(object sender, EventArgs e)
        {
            await CommonMode(Mode.Dump, false, "Select the directory to dump", "Select the directory to put your dump in");
        }

        private async void mergeBtn_Click(object sender, EventArgs e)
        {
            if (_buttonsDisabled)
            {
                return;
            }
            SensitiveAllBtns(true);
            string modCountStr = Interaction.InputBox("How many mods would you like to merge?", "Viola").Trim();
            if (string.IsNullOrEmpty(modCountStr))
            {
                CLogger.AddImportantInfo("Field was empty/operation cancelled by user.");
                CLogger.InvokeImportantInfos();
                SensitiveAllBtns(false);
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
                    CLogger.AddImportantInfo("Please enter a valid uint32.");
                    CLogger.InvokeImportantInfos();
                    SensitiveAllBtns(false);
                    return;
                }

                List<string> pathsToMerge = new List<string>();
                bool isSuccuss = true;
                //Populate the list by opening folder select dialogs.
                for (int i = 0; i < modCount; i++)
                {
                    var path = CGuiUtils.ChooseFolder($"Choose mod to add to merge pool at priority num. {i + 1}");
                    if (path == string.Empty)
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
                    CLogger.AddImportantInfo("Something went wrong when choosing files. Cancelling merge operation");
                    CLogger.InvokeImportantInfos();
                    SensitiveAllBtns(false);
                    return;
                }
                //construct LaunchOptions.
                var options = new CLaunchOptions();
                options.Mode = Mode.Merge;
                options.StuffToMerge = pathsToMerge;
                var packOutputPath = CGuiUtils.ChooseFolder("Choose output path");
                if (packOutputPath == string.Empty)
                {
                    MessageBox.Show("Something went wrong when choosing files. Cancelling merge operation");
                    SensitiveAllBtns(false);
                    return;
                }
                options.OutputPath = packOutputPath;
                
                string cpkLIstPath = CGuiUtils.ChooseExistingFile("Choose an external cpk_list file", "cfgbin file|*.bin");
                if (cpkLIstPath == null)
                {
                    MessageBox.Show("Operation cancelled by user.");
                    SensitiveAllBtns(false);
                    return;
                }
                options.CpkListPath = cpkLIstPath;
                var platformDialog = new ChoosePackPlatformForm();
                platformDialog.ShowDialog();
                Platform plat = 0;
                if (platformDialog.DialogResult == DialogResult.OK)
                {
                    plat = platformDialog.Output;
                }
                else
                {
                    MessageBox.Show("Operation cancelled by user.");
                    SensitiveAllBtns(false);
                    return;
                }
                options.PackPlatform = plat;

                var launcher = new CLauncher(options);
                await launcher.LaunchAsync();
                CLogger.InvokeImportantInfos();
                SensitiveAllBtns(false);
            }
        }

        private async void encryptBtn_Click(object sender, EventArgs e)
        {
            if (_buttonsDisabled)
            {
                return;
            }
            SensitiveAllBtns(true);

            var options = new CLaunchOptions();
            options.Mode = Mode.Encrypt;
            var key = Interaction.InputBox("Please enter an encryption key.", "Viola");
            try
            {
                options.Key = uint.Parse(key.Trim());
            }
            catch
            {
                MessageBox.Show("Please enter a valid uint as the key.");
                SensitiveAllBtns(false);
                return;
            }
            var fileToEncrypt = CGuiUtils.ChooseExistingFile("Please choose the file you wish to encrypt.", "All files|*.*");
            if (fileToEncrypt == null)
            {
                MessageBox.Show("Operation cancelled by user.");
                SensitiveAllBtns(false);
                return;
            }
            var savePath = CGuiUtils.SaveFile("Please choose the path where you want to save the encrypted file to.", "All files|*.*");
            if (savePath == null)
            {
                MessageBox.Show("Operation cancelled by user.");
                SensitiveAllBtns(false);
                return;
            }
            options.InputPath = fileToEncrypt;
            options.OutputPath = savePath;
            var launcher = new CLauncher(options);
            await launcher.LaunchAsync();
            CLogger.InvokeImportantInfos();
            SensitiveAllBtns(false);
        }

        private async void decryptBtn_Click(object sender, EventArgs e)
        {
            SensitiveAllBtns(true);
            var options = new CLaunchOptions();
            options.Mode = Mode.Decrypt;
            var fileToDecrypt = CGuiUtils.ChooseExistingFile("Choose the file you want to decrypt","All files|*.*");
            if (fileToDecrypt == "")
            {
                MessageBox.Show("Operation cancelled");
                SensitiveAllBtns(false);
                return;
            }
            var destiniation = CGuiUtils.SaveFile("Choose where you want to save the decrypted output to.","All files|*.*");
            if (destiniation == "")
            {
                MessageBox.Show("Operation cancelled");
                SensitiveAllBtns(false);
                return;
            }
            options.InputPath = fileToDecrypt;
            options.OutputPath = destiniation;
            var launcher = new CLauncher(options);
            await launcher.LaunchAsync();
            CLogger.InvokeImportantInfos();
            SensitiveAllBtns(false);
        }
    }
}
