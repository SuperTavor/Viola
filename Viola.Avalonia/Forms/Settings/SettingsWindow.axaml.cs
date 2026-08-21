using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Viola.Avalonia.GuiUtils;
using Viola.Core.Pack.DataClasses;
using Viola.Core.Settings.Logic;

namespace Viola.Avalonia.Forms.Settings
{
    public partial class SettingsWindow : Window
    {
        private readonly CSettings _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            _settings = CSettings.Load();
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            cmbPlatform.Items.Add("Ask Every Time");
            cmbPlatform.Items.Add(Platform.NintendoSwitch.ToString());
            cmbPlatform.Items.Add(Platform.PC.ToString());

            if (_settings.DefaultPackPlatform == null)
            {
                cmbPlatform.SelectedIndex = 0;
            }
            else
            {
                cmbPlatform.SelectedItem = _settings.DefaultPackPlatform.ToString();
            }

            txtDumpInput.Text = _settings.DefaultDumpInputPath;
            txtDumpOutput.Text = _settings.DefaultDumpOutputPath;
            txtPackInput.Text = _settings.DefaultPackInputPath;
            txtPackOutput.Text = _settings.DefaultPackOutputPath;
            txtVanillaCpk.Text = _settings.DefaultVanillaCpkListPath;
            chkClearOutput.IsChecked = _settings.ClearOutputBeforePack;
            chkSmartDump.IsChecked = _settings.SmartDump;
        }

        private async void btnBrowseDumpInput_Click(object? sender, RoutedEventArgs e)
        {
            var path = await CGuiUtils.ChooseFolderAsync(this, "Select Default Dump Input Folder", txtDumpInput.Text ?? "");
            if (!string.IsNullOrEmpty(path)) txtDumpInput.Text = path;
        }

        private async void btnBrowseDumpOutput_Click(object? sender, RoutedEventArgs e)
        {
            var path = await CGuiUtils.ChooseFolderAsync(this, "Select Default Dump Output Folder", txtDumpOutput.Text ?? "");
            if (!string.IsNullOrEmpty(path)) txtDumpOutput.Text = path;
        }

        private async void btnBrowsePackInput_Click(object? sender, RoutedEventArgs e)
        {
            var path = await CGuiUtils.ChooseFolderAsync(this, "Select Default Pack Input Folder", txtPackInput.Text ?? "");
            if (!string.IsNullOrEmpty(path)) txtPackInput.Text = path;
        }

        private async void btnBrowsePackOutput_Click(object? sender, RoutedEventArgs e)
        {
            var path = await CGuiUtils.ChooseFolderAsync(this, "Select Default Pack Output Folder", txtPackOutput.Text ?? "");
            if (!string.IsNullOrEmpty(path)) txtPackOutput.Text = path;
        }

        private async void btnBrowseVanillaCpk_Click(object? sender, RoutedEventArgs e)
        {
            var path = await CGuiUtils.ChooseExistingFileAsync(this, "Select Default Vanilla CPK List", "CfgBin file|*.bin", txtVanillaCpk.Text ?? "");
            if (!string.IsNullOrEmpty(path)) txtVanillaCpk.Text = path;
        }

        private void btnSave_Click(object? sender, RoutedEventArgs e)
        {
            if (cmbPlatform.SelectedIndex == 0 || cmbPlatform.SelectedItem == null)
            {
                _settings.DefaultPackPlatform = null;
            }
            else
            {
                if (Enum.TryParse(cmbPlatform.SelectedItem.ToString(), out Platform p))
                {
                    _settings.DefaultPackPlatform = p;
                }
            }

            _settings.DefaultDumpInputPath = txtDumpInput.Text ?? "";
            _settings.DefaultDumpOutputPath = txtDumpOutput.Text ?? "";
            _settings.DefaultPackInputPath = txtPackInput.Text ?? "";
            _settings.DefaultPackOutputPath = txtPackOutput.Text ?? "";
            _settings.DefaultVanillaCpkListPath = txtVanillaCpk.Text ?? "";
            _settings.ClearOutputBeforePack = chkClearOutput.IsChecked ?? false;
            _settings.SmartDump = chkSmartDump.IsChecked ?? false;

            _settings.Save();
            Close();
        }

        private void btnCancel_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}