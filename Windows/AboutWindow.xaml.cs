﻿using System.Windows;

namespace InvoicesManager.Windows
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
            => InitializeComponent();

        private void Window_Loaded(object sender, RoutedEventArgs e)
            => Msg_PVersion.Content = $"Version: {Classes.EnvironmentsVariable.PROGRAM_VERSION}";
    }
}
