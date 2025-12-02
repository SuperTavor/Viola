namespace Viola.WinForms.Forms.MainForm
{
    partial class MainForm
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
            packBtn = new Button();
            decryptBtn = new Button();
            mergeBtn = new Button();
            dumpBtn = new Button();
            consoleRichTextBox = new RichTextBox();
            encryptBtn = new Button();
            settingsBtn = new Button();
            quickStartLinkLabel = new LinkLabel();
            progressBar = new ProgressBar();
            statusLabel = new Label();
            SuspendLayout();
            // 
            // packBtn
            // 
            packBtn.BackColor = SystemColors.Info;
            packBtn.Font = new Font("Yu Gothic UI Semibold", 12F, FontStyle.Bold);
            packBtn.Location = new Point(138, 225);
            packBtn.Name = "packBtn";
            packBtn.Size = new Size(154, 86);
            packBtn.TabIndex = 0;
            packBtn.Text = "Pack";
            packBtn.UseVisualStyleBackColor = false;
            packBtn.Click += packBtn_Click;
            // 
            // decryptBtn
            // 
            decryptBtn.BackColor = SystemColors.Info;
            decryptBtn.Font = new Font("Yu Gothic UI Semibold", 12F, FontStyle.Bold);
            decryptBtn.Location = new Point(298, 313);
            decryptBtn.Name = "decryptBtn";
            decryptBtn.Size = new Size(154, 86);
            decryptBtn.TabIndex = 1;
            decryptBtn.Text = "Decrypt Criware";
            decryptBtn.UseVisualStyleBackColor = false;
            decryptBtn.Click += decryptBtn_Click;
            // 
            // mergeBtn
            // 
            mergeBtn.BackColor = SystemColors.Info;
            mergeBtn.Font = new Font("Yu Gothic UI Semibold", 12F, FontStyle.Bold);
            mergeBtn.Location = new Point(458, 225);
            mergeBtn.Name = "mergeBtn";
            mergeBtn.Size = new Size(154, 86);
            mergeBtn.TabIndex = 2;
            mergeBtn.Text = "Merge";
            mergeBtn.UseVisualStyleBackColor = false;
            mergeBtn.Click += mergeBtn_Click;
            // 
            // dumpBtn
            // 
            dumpBtn.BackColor = SystemColors.Info;
            dumpBtn.Font = new Font("Yu Gothic UI Semibold", 12F, FontStyle.Bold);
            dumpBtn.Location = new Point(298, 225);
            dumpBtn.Name = "dumpBtn";
            dumpBtn.Size = new Size(154, 86);
            dumpBtn.TabIndex = 3;
            dumpBtn.Text = "Dump";
            dumpBtn.UseVisualStyleBackColor = false;
            dumpBtn.Click += dumpBtn_Click;
            // 
            // consoleRichTextBox
            // 
            consoleRichTextBox.BackColor = SystemColors.Info;
            consoleRichTextBox.Font = new Font("Yu Gothic UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            consoleRichTextBox.Location = new Point(12, 12);
            consoleRichTextBox.Name = "consoleRichTextBox";
            consoleRichTextBox.ReadOnly = true;
            consoleRichTextBox.Size = new Size(742, 175);
            consoleRichTextBox.TabIndex = 4;
            consoleRichTextBox.Text = "Viola Console\n";
            // 
            // progressBar
            // 
            progressBar.Location = new Point(12, 205);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(742, 15);
            progressBar.TabIndex = 9;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.BackColor = Color.Transparent;
            statusLabel.Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold);
            statusLabel.ForeColor = Color.White;
            statusLabel.Location = new Point(12, 188);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(0, 15);
            statusLabel.TabIndex = 10;
            // 
            // encryptBtn
            // 
            encryptBtn.BackColor = SystemColors.Info;
            encryptBtn.Font = new Font("Yu Gothic UI Semibold", 12F, FontStyle.Bold);
            encryptBtn.Location = new Point(138, 313);
            encryptBtn.Name = "encryptBtn";
            encryptBtn.Size = new Size(154, 86);
            encryptBtn.TabIndex = 6;
            encryptBtn.Text = "Encrypt Criware";
            encryptBtn.UseVisualStyleBackColor = false;
            encryptBtn.Click += encryptBtn_Click;
            // 
            // settingsBtn
            // 
            settingsBtn.BackColor = SystemColors.Info;
            settingsBtn.Font = new Font("Yu Gothic UI Semibold", 12F, FontStyle.Bold);
            settingsBtn.Location = new Point(458, 313);
            settingsBtn.Name = "settingsBtn";
            settingsBtn.Size = new Size(154, 86);
            settingsBtn.TabIndex = 8;
            settingsBtn.Text = "Settings";
            settingsBtn.UseVisualStyleBackColor = false;
            settingsBtn.Click += settingsBtn_Click;
            // 
            // quickStartLinkLabel
            // 
            quickStartLinkLabel.ActiveLinkColor = Color.LightSkyBlue;
            quickStartLinkLabel.AutoSize = true;
            quickStartLinkLabel.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            quickStartLinkLabel.LinkColor = Color.FromArgb(192, 255, 192);
            quickStartLinkLabel.Location = new Point(319, 404);
            quickStartLinkLabel.Name = "quickStartLinkLabel";
            quickStartLinkLabel.Size = new Size(117, 20);
            quickStartLinkLabel.TabIndex = 7;
            quickStartLinkLabel.TabStop = true;
            quickStartLinkLabel.Text = "Quickstart guide";
            quickStartLinkLabel.LinkClicked += quickStartLinkLabel_LinkClicked;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GrayText;
            ClientSize = new Size(766, 433);
            Controls.Add(statusLabel);
            Controls.Add(progressBar);
            Controls.Add(quickStartLinkLabel);
            Controls.Add(settingsBtn);
            Controls.Add(encryptBtn);
            Controls.Add(consoleRichTextBox);
            Controls.Add(dumpBtn);
            Controls.Add(mergeBtn);
            Controls.Add(decryptBtn);
            Controls.Add(packBtn);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button packBtn;
        private Button decryptBtn;
        private Button mergeBtn;
        private Button dumpBtn;
        private RichTextBox consoleRichTextBox;
        private Button encryptBtn;
        private Button settingsBtn;
        private LinkLabel quickStartLinkLabel;
        private ProgressBar progressBar;
        private Label statusLabel;
    }
}