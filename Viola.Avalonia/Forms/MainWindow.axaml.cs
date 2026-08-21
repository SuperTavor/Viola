using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Media;
using Avalonia.Threading;
using Viola.Avalonia.Forms.ChoosePlatform;
using Viola.Avalonia.Forms.Settings;
using Viola.Avalonia.GuiUtils;
using Viola.Core.Launcher.DataClasses;
using Viola.Core.Launcher.Logic;
using Viola.Core.Pack.DataClasses;
using Viola.Core.Settings.Logic;
using Viola.Core.Utils.General.Logic;
using Viola.Core.ViolaLogger.Logic;

namespace Viola.Avalonia.Forms
{
    public partial class MainWindow : Window
    {
        private readonly List<Button> _btns = new();
        private bool _buttonsDisabled;

        public MainWindow()
        {
            InitializeComponent();
            CGeneralUtils.isConsole = false;
            _btns.Add(packBtn);
            _btns.Add(dumpBtn);
            _btns.Add(mergeBtn);
            _btns.Add(decryptBtn);
            _btns.Add(encryptBtn);
            _btns.Add(settingsBtn);
            Title = $"Viola {CGeneralUtils.APP_VERSION} (Avalonia)";
            CLogger.GuiLogInfoEvent += GuiLog;
            CLogger.GuiMsgBoxEvent += GuiMsgBox;
            CGeneralUtils.OnProgress += UpdateProgress;
        }

        private void UpdateProgress(long current, long total, string prefix)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                UpdateProgressCore(current, total, prefix);
            }
            else
            {
                Dispatcher.UIThread.Post(() => UpdateProgressCore(current, total, prefix));
            }
        }

        private void UpdateProgressCore(long current, long total, string prefix)
        {
            if (total > 0)
            {
                int percentage = (int)((double)current / total * 100);
                progressBar.Value = Math.Min(100, Math.Max(0, percentage));

                statusLabel.Text = $"{prefix}: {percentage}% ({current}/{total})";
                statusLabel.IsVisible = true;

                Title = $"Viola {CGeneralUtils.APP_VERSION} (Avalonia) - {prefix} {percentage}%";
            }
            else
            {
                progressBar.Value = 0;
                statusLabel.Text = "";
                Title = $"Viola {CGeneralUtils.APP_VERSION} (Avalonia)";
            }
        }

        //Simulates the button being disabled because the default disabled look is ugly as shit
        internal void SensitiveAllBtns(bool disable)
        {
            foreach (var btn in _btns)
            {
                if (disable)
                {
                    btn.Foreground = new SolidColorBrush(Colors.DarkGray);
                }
                else
                {
                    btn.Foreground = new SolidColorBrush(Colors.White);
                }
                btn.IsEnabled = !disable;
            }
            _buttonsDisabled = disable;
        }

        internal bool ButtonsDisabled => _buttonsDisabled;

        internal void UnhookEvents()
        {
            CLogger.GuiLogInfoEvent -= GuiLog;
            CGeneralUtils.OnProgress -= UpdateProgress;
        }

        internal static Func<Window, Task<(bool Confirmed, Platform Output)>>? PlatformPickOverride;

        internal static Func<CLaunchOptions, Task>? LaunchOverride;

        private async Task LaunchOptionsAsync(CLaunchOptions options)
        {
            if (LaunchOverride != null)
            {
                await LaunchOverride(options);
                return;
            }
            try
            {
                var launcher = new CLauncher(options);
                await launcher.LaunchAsync();
            }
            catch (Exception ex)
            {
                CLogger.AddImportantInfo($"An error occurred during the operation: {ex.Message}");
                CLogger.LogInfo(ex.ToString());
            }
        }

        private async Task<(bool Confirmed, Platform Output)> PickPlatformAsync()
        {
            if (PlatformPickOverride != null)
            {
                return await PlatformPickOverride(this);
            }
            var platformDialog = new ChoosePackPlatformWindow();
            await platformDialog.ShowDialog(this);
            return (platformDialog.Confirmed, platformDialog.Output);
        }

        private void GuiLog(string msg)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                GuiLogCore(msg);
            }
            else
            {
                Dispatcher.UIThread.Post(() => GuiLogCore(msg));
            }
        }

        private void GuiMsgBox(string msg)
        {
            _ = CAlert.ShowInfoAsync(this, msg, "Viola");
        }
        private void GuiLogCore(string msg)
        {
            consoleTextBox.Text += msg;
            consoleTextBox.CaretIndex = consoleTextBox.Text.Length;
            consoleTextBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault()?.ScrollToEnd();

            if (msg.Contains("Done.") || msg.Contains("Operation cancelled"))
            {
                progressBar.Value = 0;
                statusLabel.Text = "";
                Title = $"Viola {CGeneralUtils.APP_VERSION} (GUI)";
            }
        }

        internal async void packBtn_Click(object? sender, RoutedEventArgs e)
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
                bool yes = await CAlert.ShowConfirmAsync(this, "Do you want to use an external, vanilla cpk_list? (Recommended)", "Confirmation");
                if (yes)
                {
                    file = await CGuiUtils.ChooseExistingFileAsync(this, "Choose an external cpk_list file", "CfgBin file|*.bin");
                    if (string.IsNullOrEmpty(file))
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
                var (confirmed, output) = await PickPlatformAsync();
                if (!confirmed)
                {
                    await CAlert.ShowInfoAsync(this, "Operation cancelled by user.", "Viola");
                    SensitiveAllBtns(false);
                    return;
                }
                plat = output;
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

            var settings = CSettings.Load();
            options.ClearOutputBeforePack = settings.ClearOutputBeforePack;

            string inputDir = "";
            if (!string.IsNullOrEmpty(defaultInputPath) && Directory.Exists(defaultInputPath))
            {
                inputDir = defaultInputPath;
            }
            else
            {
                inputDir = await CGuiUtils.ChooseFolderAsync(this, inputFileMessage, defaultInputPath);
            }

            if (inputDir == string.Empty)
            {
                await CAlert.ShowInfoAsync(this, "Operation cancelled by user.", "Viola");
                SensitiveAllBtns(false);
                return;
            }
            string outputDir = "";
            if (saveFile)
            {
                outputDir = await CGuiUtils.SaveFileAsync(this, outputFileMessage, saveFileFilter, defaultOutputPath);
            }
            else
            {
                if (!string.IsNullOrEmpty(defaultOutputPath) && Directory.Exists(defaultOutputPath))
                {
                    outputDir = defaultOutputPath;
                }
                else
                {
                    outputDir = await CGuiUtils.ChooseFolderAsync(this, outputFileMessage, defaultOutputPath);
                }
            }

            if (outputDir == string.Empty)
            {
                await CAlert.ShowInfoAsync(this, "Operation cancelled by user.", "Viola");
                SensitiveAllBtns(false);
                return;
            }
            options.InputPath = inputDir;
            options.OutputPath = outputDir;
            options.CpkListPath = specificCpkListPath;
            await LaunchOptionsAsync(options);
            SensitiveAllBtns(false);
            CLogger.InvokeImportantInfos();
        }

        internal void QuickStartLink_OnClick(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/SuperTavor/Viola?tab=readme-ov-file#quickstart") { UseShellExecute = true });
        }

        internal async void dumpBtn_Click(object? sender, RoutedEventArgs e)
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
                    await CAlert.ShowInfoAsync(this, "Smart Dump is enabled, but no Vanilla CPK List is configured.\nPlease select the original cpk_list.cfg.bin file.", "Smart Dump Requirement");
                    cpkListPath = await CGuiUtils.ChooseExistingFileAsync(this, "Select Vanilla cpk_list.cfg.bin", "CfgBin file|*.bin");

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

        internal async void mergeBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (_buttonsDisabled)
            {
                return;
            }
            SensitiveAllBtns(true);
            string modCountStr = await CAlert.ShowInputAsync(this, "How many mods would you like to merge?", "Viola");
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

                var pathsToMerge = new List<string>();
                bool isSuccess = true;
                for (int i = 0; i < modCount; i++)
                {
                    var path = await CGuiUtils.ChooseFolderAsync(this, $"Choose mod to add to merge pool at priority num. {i + 1}");
                    if (path == string.Empty)
                    {
                        isSuccess = false;
                        break;
                    }
                    else
                    {
                        pathsToMerge.Add(path);
                    }
                }

                if (!isSuccess)
                {
                    CLogger.AddImportantInfo("Something went wrong when choosing files. Cancelling merge operation");
                    CLogger.InvokeImportantInfos();
                    SensitiveAllBtns(false);
                    return;
                }

                var options = new CLaunchOptions();
                options.Mode = Mode.Merge;
                options.StuffToMerge = pathsToMerge;
                var packOutputPath = await CGuiUtils.ChooseFolderAsync(this, "Choose output path");
                if (packOutputPath == string.Empty)
                {
                    await CAlert.ShowInfoAsync(this, "Something went wrong when choosing files. Cancelling merge operation", "Viola");
                    SensitiveAllBtns(false);
                    return;
                }
                options.OutputPath = packOutputPath;

                string cpkListPath = await CGuiUtils.ChooseExistingFileAsync(this, "Choose an external cpk_list file", "cfgbin file|*.bin");
                if (string.IsNullOrEmpty(cpkListPath))
                {
                    await CAlert.ShowInfoAsync(this, "Operation cancelled by user.", "Viola");
                    SensitiveAllBtns(false);
                    return;
                }
                options.CpkListPath = cpkListPath;

                var (confirmed, plat) = await PickPlatformAsync();
                if (!confirmed)
                {
                    await CAlert.ShowInfoAsync(this, "Operation cancelled by user.", "Viola");
                    SensitiveAllBtns(false);
                    return;
                }
                options.PackPlatform = plat;

                await LaunchOptionsAsync(options);
                CLogger.InvokeImportantInfos();
                SensitiveAllBtns(false);
            }
        }

        internal async void encryptBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (_buttonsDisabled)
            {
                return;
            }
            SensitiveAllBtns(true);

            var options = new CLaunchOptions();
            options.Mode = Mode.Encrypt;
            await CAlert.ShowInfoAsync(this, "Note: The encryption key is calculated based on the filename of the input file. Make sure the filename is correct before encrypting.", "Information");

            var fileToEncrypt = await CGuiUtils.ChooseExistingFileAsync(this, "Please choose the file you wish to encrypt.", "All files|*.*");
            if (string.IsNullOrEmpty(fileToEncrypt))
            {
                await CAlert.ShowInfoAsync(this, "Operation cancelled by user.", "Viola");
                SensitiveAllBtns(false);
                return;
            }

            var savePath = await CGuiUtils.SaveFileAsync(this, "Please choose the path where you want to save the encrypted file to.", "All files|*.*");
            if (string.IsNullOrEmpty(savePath))
            {
                await CAlert.ShowInfoAsync(this, "Operation cancelled by user.", "Viola");
                SensitiveAllBtns(false);
                return;
            }

            options.InputPath = fileToEncrypt;
            options.OutputPath = savePath;

            await LaunchOptionsAsync(options);

            CLogger.InvokeImportantInfos();
            SensitiveAllBtns(false);
        }

        internal async void decryptBtn_Click(object? sender, RoutedEventArgs e)
        {
            SensitiveAllBtns(true);
            var options = new CLaunchOptions();
            options.Mode = Mode.Decrypt;
            var fileToDecrypt = await CGuiUtils.ChooseExistingFileAsync(this, "Choose the file you want to decrypt", "All files|*.*");
            if (string.IsNullOrEmpty(fileToDecrypt))
            {
                await CAlert.ShowInfoAsync(this, "Operation cancelled", "Viola");
                SensitiveAllBtns(false);
                return;
            }
            var destination = await CGuiUtils.SaveFileAsync(this, "Choose where you want to save the decrypted output to.", "All files|*.*");
            if (string.IsNullOrEmpty(destination))
            {
                await CAlert.ShowInfoAsync(this, "Operation cancelled", "Viola");
                SensitiveAllBtns(false);
                return;
            }
            options.InputPath = fileToDecrypt;
            options.OutputPath = destination;
            try
            {
                var launcher = new CLauncher(options);
                await launcher.LaunchAsync();
            }
            catch (Exception ex)
            {
                CLogger.AddImportantInfo($"An error occurred during the operation: {ex.Message}");
                CLogger.LogInfo(ex.ToString());
            }
            CLogger.InvokeImportantInfos();
            SensitiveAllBtns(false);
        }

        internal async void settingsBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (_buttonsDisabled) return;
            var settingsWindow = new SettingsWindow();
            await settingsWindow.ShowDialog(this);
        }
    }
}