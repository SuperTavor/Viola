using Viola.Core.Pack.DataClasses;

namespace Viola.WinForms.Forms
{
    public partial class ChoosePackPlatformForm : Form
    {
        public Platform Output;
        public ChoosePackPlatformForm()
        {
            InitializeComponent();
            comboBox1.DataSource = Enum.GetValues(typeof(Platform));
        }

        private void submitBtn_Click(object sender, EventArgs e)
        {
            Output = Enum.Parse<Platform>(comboBox1.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
