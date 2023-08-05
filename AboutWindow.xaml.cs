using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TubeTowelAppWpf {
    public partial class AboutWindow : Window {
        public AboutWindow() {
            InitializeComponent();
        }

        private void aboutWindow_Closed(object sender, EventArgs e) {
            MainWindow.aboutWindow = null;
        }
    }
}
