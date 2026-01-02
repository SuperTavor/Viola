using System;
using System.Windows.Forms;
using Viola.Core.Pack.DataClasses;
using Viola.Core.Settings.Logic;
using Viola.WinForms.GuiUtils;

namespace Viola.WinForms.Forms.Settings
{
    public partial class SettingsForm : Form
    {
        private CSettings _settings;

        public SettingsForm()
        {
            InitializeComponent();
            _settings = CSettings.Load();
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            // Platform
            cmbPlatform.Items.Add("Ask Every Time");
            cmbPlatform.Items.Add(Platform.SWITCH.ToString());
            cmbPlatform.Items.Add(Platform.PC.ToString());

            if (_settings.DefaultPackPlatform == null)
            {
                cmbPlatform.SelectedIndex = 0;
            }
            else
            {
                cmbPlatform.SelectedItem = _settings.DefaultPackPlatform.ToString();
            }

            // Paths
            txtDumpInput.Text = _settings.DefaultDumpInputPath;
            txtDumpOutput.Text = _settings.DefaultDumpOutputPath;
            txtPackInput.Text = _settings.DefaultPackInputPath;
            txtPackOutput.Text = _settings.DefaultPackOutputPath;
            txtVanillaCpk.Text = _settings.DefaultVanillaCpkListPath;
            chkClearOutput.Checked = _settings.ClearOutputBeforePack;
            chkSmartDump.Checked = _settings.SmartDump;
        }

        private void btnBrowseDumpInput_Click(object sender, EventArgs e)
        {
            var path = CGuiUtils.ChooseFolder("Select Default Dump Input Folder", txtDumpInput.Text);
            if (!string.IsNullOrEmpty(path)) txtDumpInput.Text = path;
        }

        private void btnBrowseDumpOutput_Click(object sender, EventArgs e)
        {
            var path = CGuiUtils.ChooseFolder("Select Default Dump Output Folder", txtDumpOutput.Text);
            if (!string.IsNullOrEmpty(path)) txtDumpOutput.Text = path;
        }

        private void btnBrowsePackInput_Click(object sender, EventArgs e)
        {
            var path = CGuiUtils.ChooseFolder("Select Default Pack Input Folder", txtPackInput.Text);
            if (!string.IsNullOrEmpty(path)) txtPackInput.Text = path;
        }

        private void btnBrowsePackOutput_Click(object sender, EventArgs e)
        {
            var path = CGuiUtils.ChooseFolder("Select Default Pack Output Folder", txtPackOutput.Text);
            if (!string.IsNullOrEmpty(path)) txtPackOutput.Text = path;
        }

        private void btnBrowseVanillaCpk_Click(object sender, EventArgs e)
        {
            var path = CGuiUtils.ChooseExistingFile("Select Default Vanilla CPK List", "CfgBin file|*.bin", txtVanillaCpk.Text);
            if (!string.IsNullOrEmpty(path)) txtVanillaCpk.Text = path;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Platform
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

            // Paths
            _settings.DefaultDumpInputPath = txtDumpInput.Text;
            _settings.DefaultDumpOutputPath = txtDumpOutput.Text;
            _settings.DefaultPackInputPath = txtPackInput.Text;
            _settings.DefaultPackOutputPath = txtPackOutput.Text;
            _settings.DefaultVanillaCpkListPath = txtVanillaCpk.Text;
            _settings.ClearOutputBeforePack = chkClearOutput.Checked;
            _settings.SmartDump = chkSmartDump.Checked;

            _settings.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
