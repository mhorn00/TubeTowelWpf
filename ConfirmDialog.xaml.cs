using System.Windows;
using System.ComponentModel;
using System.Threading;

using System.Runtime.InteropServices;
using System;

namespace TubeTowelAppWpf {
    public partial class ConfirmDialog : Window {
        public ConfirmDialog(string msg1, string msg2, bool taskbar) {
            InitializeComponent();
            cancelBtn.Focus();
            lbl1.Content = msg1;
            lbl2.Content = msg2;
            ShowInTaskbar = taskbar;
        }

        private void yesBtn_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void ConfirmDialog_Closing(object sender, CancelEventArgs e) {
            if (!DialogResult.HasValue) {
                DialogResult = false;
            }
        }
    }
}
