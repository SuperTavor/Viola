namespace Viola.WinForms.Forms
{
    partial class ChoosePackPlatformForm
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
            submitBtn = new Button();
            comboBox1 = new ComboBox();
            descLabel = new Label();
            SuspendLayout();
            // 
            // submitBtn
            // 
            submitBtn.BackColor = SystemColors.Info;
            submitBtn.Font = new Font("Yu Gothic UI Semibold", 12F, FontStyle.Bold);
            submitBtn.Location = new Point(73, 98);
            submitBtn.Name = "submitBtn";
            submitBtn.Size = new Size(154, 46);
            submitBtn.TabIndex = 1;
            submitBtn.Text = "Submit";
            submitBtn.UseVisualStyleBackColor = false;
            submitBtn.Click += submitBtn_Click;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(56, 54);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(189, 23);
            comboBox1.TabIndex = 2;
            // 
            // descLabel
            // 
            descLabel.AutoSize = true;
            descLabel.Font = new Font("Yu Gothic UI Semibold", 12F, FontStyle.Bold);
            descLabel.Location = new Point(12, 19);
            descLabel.Name = "descLabel";
            descLabel.Size = new Size(283, 21);
            descLabel.TabIndex = 3;
            descLabel.Text = "Choose target platform for your mod";
            // 
            // ChoosePackPlatformForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlDark;
            ClientSize = new Size(313, 156);
            Controls.Add(descLabel);
            Controls.Add(comboBox1);
            Controls.Add(submitBtn);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "ChoosePackPlatformForm";
            Text = "Choose pack platform";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button submitBtn;
        private ComboBox comboBox1;
        private Label descLabel;
    }
}