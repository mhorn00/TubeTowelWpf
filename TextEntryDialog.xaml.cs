using System;
using System.ComponentModel;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;

namespace TubeTowelAppWpf {
    public partial class TextEntryDialog : Window {
        private const string emailRegex = @"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])";
        private readonly MainWindow mwInst;
        private readonly string regkey;
        private readonly bool fill;

        public TextEntryDialog(MainWindow mw, string msg1, string msg2, string rkey, bool fillPrev) {
            InitializeComponent();
            lbl1.Content = msg1;
            lbl2.Content = msg2;
            mwInst = mw;
            regkey = rkey;
            fill = fillPrev;
            if (fillPrev) {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\TubeTowelCheckInOutTool")) {
                    object k = key.GetValue(regkey);
                    if (k != null) {
                        txtBx.Text = k.ToString();
                    } else {
                        txtBx.Text = "";
                    }
                }
            }
        }

        private void textEntryDialog_Closing(object sender, CancelEventArgs e) {
            if (!DialogResult.HasValue) {
                DialogResult = false;
            }
        }

        private void okBtn_Click(object sender, RoutedEventArgs e) {
            if (Regex.IsMatch(txtBx.Text.Trim(), emailRegex) || !fill) {
                DialogResult = true;
                try {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\TubeTowelCheckInOutTool")) {
                        if (txtBx.Text.Trim().Length > 0) {
                            key.SetValue(regkey, txtBx.Text.Trim());
                        }
                    }
                } catch (Exception ex) {
                    mwInst.log("Excpetion in writing email to registry!",MainWindow.LogLevel.Error);
                    mwInst.log(ex.Message, MainWindow.LogLevel.Error);
                    mwInst.log(ex.StackTrace ?? "No StackTrace avalible", MainWindow.LogLevel.Error);
                    SystemSounds.Beep.Play();
                    mwInst.statusLbl.Foreground = MainWindow.ErrorBrush;
                    mwInst.statusLbl.Content = "Unable to save email to registry! Check logs.";
                }
            } else {
                lbl2.Foreground = MainWindow.ErrorBrush;
                lbl2.Content = "Invalid email format";
            }
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs eventArgs) {
            DialogResult = false;
        }
    }
}
