namespace Viola.WinForms.Forms.Settings
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpDefaults = new System.Windows.Forms.GroupBox();
            this.lblPlatform = new System.Windows.Forms.Label();
            this.cmbPlatform = new System.Windows.Forms.ComboBox();
            this.lblDumpInput = new System.Windows.Forms.Label();
            this.txtDumpInput = new System.Windows.Forms.TextBox();
            this.btnBrowseDumpInput = new System.Windows.Forms.Button();
            this.lblDumpOutput = new System.Windows.Forms.Label();
            this.txtDumpOutput = new System.Windows.Forms.TextBox();
            this.btnBrowseDumpOutput = new System.Windows.Forms.Button();
            this.lblPackInput = new System.Windows.Forms.Label();
            this.txtPackInput = new System.Windows.Forms.TextBox();
            this.btnBrowsePackInput = new System.Windows.Forms.Button();
            this.lblPackOutput = new System.Windows.Forms.Label();
            this.txtPackOutput = new System.Windows.Forms.TextBox();
            this.btnBrowsePackOutput = new System.Windows.Forms.Button();
            this.lblVanillaCpk = new System.Windows.Forms.Label();
            this.txtVanillaCpk = new System.Windows.Forms.TextBox();
            this.btnBrowseVanillaCpk = new System.Windows.Forms.Button();
            this.chkClearOutput = new System.Windows.Forms.CheckBox();
            this.chkSmartDump = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpDefaults.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpDefaults
            // 
            this.grpDefaults.Controls.Add(this.lblPlatform);
            this.grpDefaults.Controls.Add(this.cmbPlatform);
            this.grpDefaults.Controls.Add(this.lblDumpInput);
            this.grpDefaults.Controls.Add(this.txtDumpInput);
            this.grpDefaults.Controls.Add(this.btnBrowseDumpInput);
            this.grpDefaults.Controls.Add(this.lblDumpOutput);
            this.grpDefaults.Controls.Add(this.txtDumpOutput);
            this.grpDefaults.Controls.Add(this.btnBrowseDumpOutput);
            this.grpDefaults.Controls.Add(this.lblPackInput);
            this.grpDefaults.Controls.Add(this.txtPackInput);
            this.grpDefaults.Controls.Add(this.btnBrowsePackInput);
            this.grpDefaults.Controls.Add(this.lblPackOutput);
            this.grpDefaults.Controls.Add(this.txtPackOutput);
            this.grpDefaults.Controls.Add(this.btnBrowsePackOutput);
            this.grpDefaults.Controls.Add(this.lblVanillaCpk);
            this.grpDefaults.Controls.Add(this.txtVanillaCpk);
            this.grpDefaults.Controls.Add(this.btnBrowseVanillaCpk);
            this.grpDefaults.Controls.Add(this.chkClearOutput);
            this.grpDefaults.Controls.Add(this.chkSmartDump);
            this.grpDefaults.ForeColor = System.Drawing.Color.White;
            this.grpDefaults.Location = new System.Drawing.Point(12, 12);
            this.grpDefaults.Name = "grpDefaults";
            this.grpDefaults.Size = new System.Drawing.Size(460, 300);
            this.grpDefaults.TabIndex = 0;
            this.grpDefaults.TabStop = false;
            this.grpDefaults.Text = "Default Settings";
            // 
            // lblPlatform
            // 
            this.lblPlatform.AutoSize = true;
            this.lblPlatform.Location = new System.Drawing.Point(20, 30);
            this.lblPlatform.Name = "lblPlatform";
            this.lblPlatform.Size = new System.Drawing.Size(96, 15);
            this.lblPlatform.TabIndex = 0;
            this.lblPlatform.Text = "Default Platform:";
            // 
            // cmbPlatform
            // 
            this.cmbPlatform.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlatform.FormattingEnabled = true;
            this.cmbPlatform.Location = new System.Drawing.Point(130, 27);
            this.cmbPlatform.Name = "cmbPlatform";
            this.cmbPlatform.Size = new System.Drawing.Size(150, 23);
            this.cmbPlatform.TabIndex = 1;
            // 
            // lblDumpInput
            // 
            this.lblDumpInput.AutoSize = true;
            this.lblDumpInput.Location = new System.Drawing.Point(20, 70);
            this.lblDumpInput.Name = "lblDumpInput";
            this.lblDumpInput.Size = new System.Drawing.Size(74, 15);
            this.lblDumpInput.TabIndex = 2;
            this.lblDumpInput.Text = "Dump Input:";
            // 
            // txtDumpInput
            // 
            this.txtDumpInput.Location = new System.Drawing.Point(130, 67);
            this.txtDumpInput.Name = "txtDumpInput";
            this.txtDumpInput.Size = new System.Drawing.Size(240, 23);
            this.txtDumpInput.TabIndex = 3;
            // 
            // btnBrowseDumpInput
            // 
            this.btnBrowseDumpInput.ForeColor = System.Drawing.Color.Black;
            this.btnBrowseDumpInput.Location = new System.Drawing.Point(380, 67);
            this.btnBrowseDumpInput.Name = "btnBrowseDumpInput";
            this.btnBrowseDumpInput.Size = new System.Drawing.Size(60, 23);
            this.btnBrowseDumpInput.TabIndex = 4;
            this.btnBrowseDumpInput.Text = "Browse";
            this.btnBrowseDumpInput.UseVisualStyleBackColor = true;
            this.btnBrowseDumpInput.Click += new System.EventHandler(this.btnBrowseDumpInput_Click);
            // 
            // lblDumpOutput
            // 
            this.lblDumpOutput.AutoSize = true;
            this.lblDumpOutput.Location = new System.Drawing.Point(20, 100);
            this.lblDumpOutput.Name = "lblDumpOutput";
            this.lblDumpOutput.Size = new System.Drawing.Size(84, 15);
            this.lblDumpOutput.TabIndex = 5;
            this.lblDumpOutput.Text = "Dump Output:";
            // 
            // txtDumpOutput
            // 
            this.txtDumpOutput.Location = new System.Drawing.Point(130, 97);
            this.txtDumpOutput.Name = "txtDumpOutput";
            this.txtDumpOutput.Size = new System.Drawing.Size(240, 23);
            this.txtDumpOutput.TabIndex = 6;
            // 
            // btnBrowseDumpOutput
            // 
            this.btnBrowseDumpOutput.ForeColor = System.Drawing.Color.Black;
            this.btnBrowseDumpOutput.Location = new System.Drawing.Point(380, 97);
            this.btnBrowseDumpOutput.Name = "btnBrowseDumpOutput";
            this.btnBrowseDumpOutput.Size = new System.Drawing.Size(60, 23);
            this.btnBrowseDumpOutput.TabIndex = 7;
            this.btnBrowseDumpOutput.Text = "Browse";
            this.btnBrowseDumpOutput.UseVisualStyleBackColor = true;
            this.btnBrowseDumpOutput.Click += new System.EventHandler(this.btnBrowseDumpOutput_Click);
            // 
            // lblPackInput
            // 
            this.lblPackInput.AutoSize = true;
            this.lblPackInput.Location = new System.Drawing.Point(20, 140);
            this.lblPackInput.Name = "lblPackInput";
            this.lblPackInput.Size = new System.Drawing.Size(66, 15);
            this.lblPackInput.TabIndex = 8;
            this.lblPackInput.Text = "Pack Input:";
            // 
            // txtPackInput
            // 
            this.txtPackInput.Location = new System.Drawing.Point(130, 137);
            this.txtPackInput.Name = "txtPackInput";
            this.txtPackInput.Size = new System.Drawing.Size(240, 23);
            this.txtPackInput.TabIndex = 9;
            // 
            // btnBrowsePackInput
            // 
            this.btnBrowsePackInput.ForeColor = System.Drawing.Color.Black;
            this.btnBrowsePackInput.Location = new System.Drawing.Point(380, 137);
            this.btnBrowsePackInput.Name = "btnBrowsePackInput";
            this.btnBrowsePackInput.Size = new System.Drawing.Size(60, 23);
            this.btnBrowsePackInput.TabIndex = 10;
            this.btnBrowsePackInput.Text = "Browse";
            this.btnBrowsePackInput.UseVisualStyleBackColor = true;
            this.btnBrowsePackInput.Click += new System.EventHandler(this.btnBrowsePackInput_Click);
            // 
            // lblPackOutput
            // 
            this.lblPackOutput.AutoSize = true;
            this.lblPackOutput.Location = new System.Drawing.Point(20, 170);
            this.lblPackOutput.Name = "lblPackOutput";
            this.lblPackOutput.Size = new System.Drawing.Size(76, 15);
            this.lblPackOutput.TabIndex = 11;
            this.lblPackOutput.Text = "Pack Output:";
            // 
            // txtPackOutput
            // 
            this.txtPackOutput.Location = new System.Drawing.Point(130, 167);
            this.txtPackOutput.Name = "txtPackOutput";
            this.txtPackOutput.Size = new System.Drawing.Size(240, 23);
            this.txtPackOutput.TabIndex = 12;
            // 
            // btnBrowsePackOutput
            // 
            this.btnBrowsePackOutput.ForeColor = System.Drawing.Color.Black;
            this.btnBrowsePackOutput.Location = new System.Drawing.Point(380, 167);
            this.btnBrowsePackOutput.Name = "btnBrowsePackOutput";
            this.btnBrowsePackOutput.Size = new System.Drawing.Size(60, 23);
            this.btnBrowsePackOutput.TabIndex = 13;
            this.btnBrowsePackOutput.Text = "Browse";
            this.btnBrowsePackOutput.UseVisualStyleBackColor = true;
            this.btnBrowsePackOutput.Click += new System.EventHandler(this.btnBrowsePackOutput_Click);
            // 
            // lblVanillaCpk
            // 
            this.lblVanillaCpk.AutoSize = true;
            this.lblVanillaCpk.Location = new System.Drawing.Point(20, 200);
            this.lblVanillaCpk.Name = "lblVanillaCpk";
            this.lblVanillaCpk.Size = new System.Drawing.Size(91, 15);
            this.lblVanillaCpk.TabIndex = 14;
            this.lblVanillaCpk.Text = "Vanilla CPK List:";
            // 
            // txtVanillaCpk
            // 
            this.txtVanillaCpk.Location = new System.Drawing.Point(130, 197);
            this.txtVanillaCpk.Name = "txtVanillaCpk";
            this.txtVanillaCpk.Size = new System.Drawing.Size(240, 23);
            this.txtVanillaCpk.TabIndex = 15;
            // 
            // btnBrowseVanillaCpk
            // 
            this.btnBrowseVanillaCpk.ForeColor = System.Drawing.Color.Black;
            this.btnBrowseVanillaCpk.Location = new System.Drawing.Point(380, 197);
            this.btnBrowseVanillaCpk.Name = "btnBrowseVanillaCpk";
            this.btnBrowseVanillaCpk.Size = new System.Drawing.Size(60, 23);
            this.btnBrowseVanillaCpk.TabIndex = 16;
            this.btnBrowseVanillaCpk.Text = "Browse";
            this.btnBrowseVanillaCpk.UseVisualStyleBackColor = true;
            this.btnBrowseVanillaCpk.Click += new System.EventHandler(this.btnBrowseVanillaCpk_Click);
            // 
            // chkClearOutput
            // 
            this.chkClearOutput.AutoSize = true;
            this.chkClearOutput.Location = new System.Drawing.Point(20, 230);
            this.chkClearOutput.Name = "chkClearOutput";
            this.chkClearOutput.Size = new System.Drawing.Size(200, 19);
            this.chkClearOutput.TabIndex = 17;
            this.chkClearOutput.Text = "Clear Output Folder Before Packing";
            this.chkClearOutput.UseVisualStyleBackColor = true;
            // 
            // chkSmartDump
            // 
            this.chkSmartDump.AutoSize = true;
            this.chkSmartDump.Location = new System.Drawing.Point(20, 260);
            this.chkSmartDump.Name = "chkSmartDump";
            this.chkSmartDump.Size = new System.Drawing.Size(200, 19);
            this.chkSmartDump.TabIndex = 18;
            this.chkSmartDump.Text = "Smart Dump (Skip existing files)";
            this.chkSmartDump.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.SystemColors.Info;
            this.btnSave.Font = new System.Drawing.Font("Yu Gothic UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnSave.Location = new System.Drawing.Point(316, 330);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 30);
            this.btnSave.TabIndex = 18;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.SystemColors.Info;
            this.btnCancel.Font = new System.Drawing.Font("Yu Gothic UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnCancel.Location = new System.Drawing.Point(397, 330);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.GrayText;
            this.ClientSize = new System.Drawing.Size(484, 380);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.grpDefaults);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.grpDefaults.ResumeLayout(false);
            this.grpDefaults.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox grpDefaults;
        private System.Windows.Forms.Label lblPlatform;
        private System.Windows.Forms.ComboBox cmbPlatform;
        private System.Windows.Forms.Label lblDumpInput;
        private System.Windows.Forms.TextBox txtDumpInput;
        private System.Windows.Forms.Button btnBrowseDumpInput;
        private System.Windows.Forms.Label lblDumpOutput;
        private System.Windows.Forms.TextBox txtDumpOutput;
        private System.Windows.Forms.Button btnBrowseDumpOutput;
        private System.Windows.Forms.Label lblPackInput;
        private System.Windows.Forms.TextBox txtPackInput;
        private System.Windows.Forms.Button btnBrowsePackInput;
        private System.Windows.Forms.Label lblPackOutput;
        private System.Windows.Forms.TextBox txtPackOutput;
        private System.Windows.Forms.Button btnBrowsePackOutput;
        private System.Windows.Forms.Label lblVanillaCpk;
        private System.Windows.Forms.TextBox txtVanillaCpk;
        private System.Windows.Forms.Button btnBrowseVanillaCpk;
        private System.Windows.Forms.CheckBox chkClearOutput;
        private System.Windows.Forms.CheckBox chkSmartDump;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
