using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace TubeTowelAppWpf {

    /*
     * TODO: 
     * - Make About and Help pages
     * - Add any options that are needed to menu
     * - Close out button function
     * - Handle Json Errors
     * 
     */
    public partial class MainWindow : Window {

        private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Colors.Red);
        private static readonly SolidColorBrush StatusBrush = new SolidColorBrush(Colors.Black);
        private const string dbSchema = @"{
              'type': 'object',
              'additionalProperties': {
                'type': 'object',
                'properties': {
                  'MemberNum': { 'type': 'string' },
                  'CheckoutCount': { 'type': 'integer' },
                  'ListID': { 'type': 'integer' },
                  'IsCheckedOut': { 'type': 'boolean' },
                  'LastModify': { 'type': 'string', 'format': 'date-time' }
                },
                'required': ['MemberNum', 'CheckoutCount', 'ListID', 'IsCheckedOut', 'LastModify']
              }
            }";

        internal Dictionary<string, MemberInfo> checkList;
        internal ObservableCollection<MemberInfo> ListViewItems {
            get {
                return new ObservableCollection<MemberInfo>(checkList.Values.OrderByDescending(m => m.ListID));
            }
        }
        private int listviewID = 0;
        private readonly String logFile = "log" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt";
        internal static AboutWindow aboutWindow { get; set; }

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
            if (!File.Exists(logFile) || (File.Exists("db.json") && new FileInfo("db.json").Length == 0)) {
                File.Create(logFile).Close();
                log("Log File Created", LogLevel.Info);
            }
            if (!File.Exists("db.json") || (File.Exists("db.json") && new FileInfo("db.json").Length == 0)) {
                File.Create("db.json").Close();
                checkList = new Dictionary<string, MemberInfo>();
                log("Created new DB", LogLevel.Info);
            } else {
                string dbJson = @File.ReadAllText("db.json");
                using (JSchemaValidatingReader jr = new JSchemaValidatingReader(new JsonTextReader(new StringReader(dbJson)))) {
                    jr.Schema = JSchema.Parse(dbSchema);
                    jr.ValidationEventHandler += DbValidationError;
                    JsonSerializer js = new JsonSerializer();
                    Dictionary<string, MemberInfo> jsonDict = js.Deserialize<Dictionary<string, MemberInfo>>(jr);
                    if (jsonDict != null && jsonDict.GetType() == typeof(Dictionary<string, MemberInfo>)) {
                        checkList = jsonDict;
                        listviewID = checkList.Values.Max(m => m.ListID);
                        memberListView.ItemsSource = ListViewItems;
                    } else {
                        checkList = new Dictionary<string, MemberInfo>();
                        log("JSON Deserialization failed!", LogLevel.Error);
                        //TODO: Handle json deserilize of dict failing
                    }
                }
            }
        }

        private void DbValidationError(object sender, SchemaValidationEventArgs e) {
            statusLbl.Foreground = ErrorBrush;
            statusLbl.Content = "DB validation error!";
            SystemSounds.Beep.Play();
            log("JSON DB Validation Error!", LogLevel.Error);
            log(e.Message, LogLevel.Error);
        }

        private void memberTxtBx_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            if (!Regex.IsMatch(e.Text, @"^[0-9]{1,7}$")) e.Handled = true;
        }
        private void memberSeachTxtBx_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            if (!Regex.IsMatch(e.Text, @"^[0-9]{1,7}$")) e.Handled = true;
        }
        private void memberTxtBx_TextChanged(object sender, TextChangedEventArgs e) {
            if (sender == null || sender.GetType() != typeof(TextBox)) return;
            TextBox txtBx = (TextBox)sender;
            txtBx.Text = txtBx.Text.Trim();
            if (txtBx.Text.Length > 7) {
                txtBx.Text = txtBx.Text.Substring(0, 7);
                txtBx.CaretIndex = txtBx.Text.Length;
            }
        }
        private void memberSearchTxtBx_TextChanged(object sender, TextChangedEventArgs e) {
            if (sender == null || sender.GetType() != typeof(TextBox)) return;
            TextBox txtBx = (TextBox)sender;
            txtBx.Text = txtBx.Text.Trim();
            if (txtBx.Text.Length > 7) {
                txtBx.Text = txtBx.Text.Substring(0, 7);
                txtBx.CaretIndex = txtBx.Text.Length;
            }
            if (Regex.IsMatch(txtBx.Text, @"^[0-9]{1,7}$")) {
                memberListView.ItemsSource = new ObservableCollection<MemberInfo>(ListViewItems.Where(m => m.MemberNum.StartsWith(txtBx.Text)));
            } else {
                memberListView.ItemsSource = ListViewItems;
            }
        }
        private void memberTextBx_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter || e.Key == Key.Return) {
                updateCheckList();
            }
        }
        private void memberTxtBx_GotFocus(object sender, RoutedEventArgs e) {
            memberSearchTxtBx.Text = "";
        }
        private void submitBtn_Click(object sender, RoutedEventArgs e) {
            updateCheckList();
        }
        private void memberListViewBtn_Click(object sender, RoutedEventArgs e) {
            if (sender == null || sender.GetType() != typeof(Button)) return;
            Button btn = (Button)sender;
            if (btn.Tag == null || btn.Tag.GetType() != typeof(string)) return;
            string memNum = (string)btn.Tag;
            if (checkList.ContainsKey(memNum)) {//if member is in list
                if (checkList[memNum].IsCheckedOut) {//and is currently checked out
                    checkList[memNum].IsCheckedOut = false; //check in
                    checkList[memNum].ListID = ++listviewID;
                    checkList[memNum].LastModify = DateTime.Now;
                    log("Checked in member " + memNum, LogLevel.Info);
                } else {
                    checkList[memNum].IsCheckedOut = true; //or check out and inc checkout count
                    checkList[memNum].ListID = ++listviewID;
                    checkList[memNum].CheckoutCount++;
                    checkList[memNum].LastModify = DateTime.Now;
                    log("Checked out member " + memNum + "; " + checkList[memNum].CheckoutCount + " times", LogLevel.Info);
                }
            } else {
                //This shouldnt happen here....
                MemberInfo mem = new MemberInfo(memNum, 1, ++listviewID, true, DateTime.Now);
                checkList.Add(memNum, mem);// else add member to list
                log("First time checked in member " + memNum, LogLevel.Info);
            }
            memberListView.ItemsSource = ListViewItems;
        }
        private void updateCheckList() {
            memberTxtBx.Text = memberTxtBx.Text.Trim();
            string memNum = memberTxtBx.Text;
            if (!Regex.IsMatch(memNum, @"^[0-9]{1,7}$")) {
                SystemSounds.Beep.Play();
                statusLbl.Foreground = ErrorBrush;
                statusLbl.Content = "Invalid member ID. Check format.";
                memberTxtBx.Focus();
                memberTxtBx.SelectAll();
                return;
            }
            if (checkList.ContainsKey(memNum)) {//if member is in list
                if (checkList[memNum].IsCheckedOut) {//and is currently checked out
                    checkList[memNum].IsCheckedOut = false; //check in
                    checkList[memNum].ListID = ++listviewID;
                    checkList[memNum].LastModify = DateTime.Now;
                    log("Checked in member " + memNum, LogLevel.Info);
                } else {
                    checkList[memNum].IsCheckedOut = true; //or check out and inc checkout count
                    checkList[memNum].ListID = ++listviewID;
                    checkList[memNum].CheckoutCount++;
                    checkList[memNum].LastModify = DateTime.Now;
                    log("Checked out member " + memNum + "; " + checkList[memNum].CheckoutCount + " times", LogLevel.Info);
                }
            } else {
                MemberInfo mem = new MemberInfo(memNum, 1, ++listviewID, true, DateTime.Now);
                checkList.Add(memNum, mem);// else add member to list
                log("First time checked in member " + memNum, LogLevel.Info);
            }
            memberTxtBx.Text = "";
            memberTxtBx.Focus();
            memberListView.ItemsSource = ListViewItems;
            statusLbl.Foreground = StatusBrush;
            statusLbl.Content = checkList[memNum].IsCheckedOut ? "Checked out member " + memNum : "Checked in member " + memNum;
            try {
                File.WriteAllText("db.json", JsonConvert.SerializeObject(checkList, Newtonsoft.Json.Formatting.Indented));
            } catch (Exception ex) {
                statusLbl.Foreground = ErrorBrush;
                statusLbl.Content = "Failed to write to DB!";
                SystemSounds.Beep.Play();
                log("Failed to write to DB", LogLevel.Error);
                log(ex.Message, LogLevel.Error);
                log(ex.StackTrace ?? "No StackTrace avalible", LogLevel.Error);
            }
        }
        private void closeOutBtn_Click(object sender, RoutedEventArgs e) {
            bool? result = new CloseOutDialog().ShowDialog();
            if (result == true) {
                memberTxtBx.IsEnabled = false;
                submitBtn.IsEnabled = false;
                memberSearchTxtBx.IsEnabled = false;
                closeOutBtn.IsEnabled = false;
            }
        }
        private void aboutMenuItem_Click(object sender, RoutedEventArgs e) {
            if (aboutWindow == null) {
                aboutWindow = new AboutWindow();
                aboutWindow.Show();
                aboutWindow.Focus();
            }
        }
        private void log(string msg, LogLevel lvl) {
            try {
                using (FileStream fs = new FileStream(logFile, FileMode.Append, FileAccess.Write)) {
                    using (StreamWriter sw = new StreamWriter(fs)) {
                        string loglvl = "";
                        switch (lvl) {
                            case LogLevel.Warn: loglvl = "WARN"; break;
                            case LogLevel.Error: loglvl = "ERROR"; break;
                            case LogLevel.Fatal: loglvl = "FATAL"; break;
                            default: loglvl = "INFO"; break;

                        }
                        sw.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] [{loglvl}] {msg}");
                    }
                }
            } catch (IOException e) {
                Console.WriteLine("Unable to write to log file.");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                statusLbl.Foreground = ErrorBrush;
                statusLbl.Content = "Unable to write to log file!";
                SystemSounds.Beep.Play();
            }
        }
        internal class MemberInfo : INotifyPropertyChanged {
            [JsonIgnore]
            private string memberNum;
            public string MemberNum {
                get { return memberNum; }
                set {
                    if (memberNum != value) {
                        memberNum = value;
                        OnPropertyChanged(nameof(MemberNum));
                    }
                }
            }
            [JsonIgnore]
            private int checkoutCount;
            public int CheckoutCount {
                get { return checkoutCount; }
                set {
                    if (checkoutCount != value) {
                        checkoutCount = value;
                        OnPropertyChanged(nameof(CheckoutCount));
                    }
                }
            }
            [JsonIgnore]
            private int listID;
            public int ListID {
                get { return listID; }
                set {
                    if (listID != value) {
                        listID = value;
                        OnPropertyChanged(nameof(ListID));
                    }
                }
            }
            [JsonIgnore]
            private bool isCheckedOut;
            public bool IsCheckedOut {
                get { return isCheckedOut; }
                set {
                    if (isCheckedOut != value) {
                        isCheckedOut = value;
                        OnPropertyChanged(nameof(IsCheckedOut));
                    }
                }
            }
            [JsonIgnore]
            private DateTime lastModify;
            public DateTime LastModify {
                get { return lastModify; }
                set {
                    if (lastModify != value) {
                        lastModify = value;
                        OnPropertyChanged(nameof(LastModify));
                    }
                }
            }
            [JsonIgnore]
            public string TimeModified { get { return LastModify.ToString("hh:mm:ss tt"); } }
            [JsonIgnore]
            public string Status { get { return IsCheckedOut ? "Checked Out. Member has checked out " + CheckoutCount + (CheckoutCount == 1 ? " time." : " times.") : "Checked In."; } }

            public event PropertyChangedEventHandler PropertyChanged;

            [JsonConstructor]
            public MemberInfo(string num, int count, int id, bool isCheckedOut, DateTime time) {
                this.memberNum = num;
                this.checkoutCount = count;
                this.listID = id;
                this.isCheckedOut = isCheckedOut;
                this.lastModify = time;
            }

            protected void OnPropertyChanged(string propertyName) {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        internal enum LogLevel {
            Info = 0,
            Warn = 1,
            Error = 2,
            Fatal = 3,
        }

    }
}
