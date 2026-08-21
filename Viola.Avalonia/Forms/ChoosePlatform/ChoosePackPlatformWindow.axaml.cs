using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Viola.Core.Pack.DataClasses;

namespace Viola.Avalonia.Forms.ChoosePlatform
{
    public partial class ChoosePackPlatformWindow : Window
    {
        public Platform Output { get; private set; }
        public bool Confirmed { get; private set; }

        public ChoosePackPlatformWindow()
        {
            InitializeComponent();
            foreach (var platform in Enum.GetValues<Platform>())
            {
                cmbPlatform.Items.Add(platform.ToString());
            }
            if (cmbPlatform.Items.Count > 0)
            {
                cmbPlatform.SelectedIndex = 0;
            }
        }

        private void submitBtn_Click(object? sender, RoutedEventArgs e)
        {
            Output = Enum.Parse<Platform>(cmbPlatform.SelectedItem?.ToString() ?? "");
            Confirmed = true;
            Close();
        }
    }
}