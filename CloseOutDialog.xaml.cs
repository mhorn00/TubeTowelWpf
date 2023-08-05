using System.Windows;
using System.ComponentModel;

namespace TubeTowelAppWpf {
    public partial class CloseOutDialog : Window {
        public CloseOutDialog() {
            InitializeComponent();
            cancelBtn.Focus();
        }

        private void yesBtn_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void CloseOutDialog_Closing(object sender, CancelEventArgs e) {
            if (!DialogResult.HasValue) {
                DialogResult = false;
            }
        }
    }
}
