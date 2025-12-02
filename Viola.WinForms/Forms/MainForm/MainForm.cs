using Viola.WinForms.GuiUtils;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Launcher.Logic;
using Viola.Core.Utils.General.Logic;
using Viola.Core.ViolaLogger.Logic;
using System.Diagnostics;
using Microsoft.VisualBasic;
using Viola.Core.Pack.DataClasses;
using Viola.Core.Settings.Logic;
using Viola.WinForms.Forms.Settings;

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
            _btns = [packBtn, dumpBtn, mergeBtn, decryptBtn, encryptBtn, settingsBtn];
            this.Text = $"Viola {CGeneralUtils.APP_VERSION} (GUI)";
            //to display all logs to the gui console
            CLogger.GuiLogInfoEvent += GuiLog;
            CGeneralUtils.OnProgress += UpdateProgress;
        }

        private void UpdateProgress(long current, long total, string prefix)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action(() => UpdateProgress(current, total, prefix)));
            }
            else
            {
                if (total > 0)
                {
                    int percentage = (int)((double)current / total * 100);
                    progressBar.Value = Math.Min(100, Math.Max(0, percentage));
                    
                    // Update status label text
                    statusLabel.Text = $"{prefix}: {percentage}% ({current}/{total})";
                    statusLabel.Visible = true;
                    statusLabel.BringToFront();
                    
                    // Also update form title for visibility in taskbar
                    this.Text = $"Viola {CGeneralUtils.APP_VERSION} (GUI) - {prefix} {percentage}%";
                }
                else
                {
                    progressBar.Value = 0;
                    statusLabel.Text = "";
                    this.Text = $"Viola {CGeneralUtils.APP_VERSION} (GUI)";
                }
            }
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
                
                // Reset progress bar when a new operation starts or finishes (heuristic)
                if (msg.Contains("Done.") || msg.Contains("Operation cancelled"))
                {
                    progressBar.Value = 0;
                    statusLabel.Text = "";
                    Text = $"Viola {CGeneralUtils.APP_VERSION} (GUI)";
                }
            }
        }

        private async void packBtn_Click(object sender, EventArgs e)
        {
            if (_buttonsDisabled)
            {
                return;
            }

            var settings = CSettings.Load();
            string file = string.Empty;

            if (!string.IsNullOrEmpty(settings.DefaultVanillaCpkListPath) && File.Exists(settings.DefaultVanillaCpkListPath))
            {
                file = settings.DefaultVanillaCpkListPath;
            }
            else
            {
                //Ask if the user wants to use an external cpklist
                DialogResult result = MessageBox.Show("Do you want to use an external, vanilla cpk_list? (Recommended)", "Confirmation", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    file = CGuiUtils.ChooseExistingFile("Choose an external cpk_list file", "CfgBin file|*.bin");
                    if (file == null || file == "")
                    {
                        CLogger.AddImportantInfo("Operation cancelled by user.");
                        CLogger.InvokeImportantInfos();
                        SensitiveAllBtns(false);
                        return;
                    }
                }
            }

            Platform plat = 0;

            if (settings.DefaultPackPlatform != null)
            {
                plat = settings.DefaultPackPlatform.Value;
            }
            else
            {
                var platformDialog = new ChoosePackPlatformForm();
                platformDialog.ShowDialog();
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
            }
            
            await CommonMode(Mode.Pack, false, "Select folder to pack", "Select folder to put the packed mod in", file, "", plat, true, settings.DefaultPackInputPath, settings.DefaultPackOutputPath);
        }

        private async Task CommonMode(Mode mode, bool saveFile, string inputFileMessage, string outputFileMessage, string specificCpkListPath = "", string saveFileFilter = "", Platform targetPlat = 0, bool isUseTargetPlat = false, string defaultInputPath = "", string defaultOutputPath = "")
        {
            if (_buttonsDisabled)
            {
                return;
            }
            SensitiveAllBtns(true);
            var options = new CLaunchOptions();
            if (isUseTargetPlat) options.PackPlatform = targetPlat;
            options.Mode = mode;

            // Load settings to apply global options
            var settings = CSettings.Load();
            options.ClearOutputBeforePack = settings.ClearOutputBeforePack;

            string inputDir = "";
            if (!string.IsNullOrEmpty(defaultInputPath) && Directory.Exists(defaultInputPath))
            {
                inputDir = defaultInputPath;
            }
            else
            {
                inputDir = CGuiUtils.ChooseFolder(inputFileMessage, defaultInputPath);
            }

            if (inputDir == string.Empty)
            {
                MessageBox.Show("Operation cancelled by user.");
                SensitiveAllBtns(false);
                return;
            }
            string outputDir = "";
            if (saveFile)
            {
                outputDir = CGuiUtils.SaveFile(outputFileMessage, saveFileFilter, defaultOutputPath);
            }
            else
            {
                if (!string.IsNullOrEmpty(defaultOutputPath) && Directory.Exists(defaultOutputPath))
                {
                    outputDir = defaultOutputPath;
                }
                else
                {
                    outputDir = CGuiUtils.ChooseFolder(outputFileMessage, defaultOutputPath);
                }
            }

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
            var settings = CSettings.Load();
            string cpkListPath = "";

            if (settings.SmartDump)
            {
                if (!string.IsNullOrEmpty(settings.DefaultVanillaCpkListPath) && File.Exists(settings.DefaultVanillaCpkListPath))
                {
                    cpkListPath = settings.DefaultVanillaCpkListPath;
                }
                else
                {
                    MessageBox.Show("Smart Dump is enabled, but no Vanilla CPK List is configured.\nPlease select the original cpk_list.cfg.bin file.", "Smart Dump Requirement", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    cpkListPath = CGuiUtils.ChooseExistingFile("Select Vanilla cpk_list.cfg.bin", "CfgBin file|*.bin");
                    
                    if (string.IsNullOrEmpty(cpkListPath))
                    {
                        CLogger.AddImportantInfo("Smart Dump requires a CPK List. Operation cancelled.");
                        CLogger.InvokeImportantInfos();
                        return;
                    }
                }
            }

            await CommonMode(Mode.Dump, false, "Select the directory to dump", "Select the directory to put your dump in", cpkListPath, "", 0, false, settings.DefaultDumpInputPath, settings.DefaultDumpOutputPath);
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
            MessageBox.Show("Note: The encryption key is calculated based on the filename of the input file. Make sure the filename is correct before encrypting.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var fileToEncrypt = CGuiUtils.ChooseExistingFile("Please choose the file you wish to encrypt.", "All files|*.*");
            if (fileToEncrypt == null || fileToEncrypt == "")
            {
                MessageBox.Show("Operation cancelled by user.");
                SensitiveAllBtns(false);
                return;
            }

            var savePath = CGuiUtils.SaveFile("Please choose the path where you want to save the encrypted file to.", "All files|*.*");
            if (savePath == null || savePath == "")
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
            var fileToDecrypt = CGuiUtils.ChooseExistingFile("Choose the file you want to decrypt", "All files|*.*");
            if (fileToDecrypt == "")
            {
                MessageBox.Show("Operation cancelled");
                SensitiveAllBtns(false);
                return;
            }
            var destiniation = CGuiUtils.SaveFile("Choose where you want to save the decrypted output to.", "All files|*.*");
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

        private void settingsBtn_Click(object sender, EventArgs e)
        {
            if (_buttonsDisabled) return;
            var settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
        }
    }
}
