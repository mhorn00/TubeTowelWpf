using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace TubeTowelAppWpf {

    /*
     * TODO: 
     * - Make About and Help pages
     * - finish Close out button function
     */
    public partial class MainWindow : Window {
        internal static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Colors.Red);
        internal static readonly SolidColorBrush StatusBrush = new SolidColorBrush(Colors.Black);
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
        private static readonly String logFile = "log" + DateTime.Now.ToString("MM-dd-yyyy") + ".txt";
        private static readonly String date = DateTime.Now.ToString("MM/dd/yyyy");

        internal static bool closedOut = false;

        public MainWindow() {
            InitializeComponent();
            log("\nTube Towel Check In/Out Tool Started", LogLevel.Info);
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
                    Dictionary<string, MemberInfo> jsonDict = null;
                    try {
                        jsonDict = js.Deserialize<Dictionary<string, MemberInfo>>(jr);
                    } catch (Exception ex) {
                        statusLbl.Foreground = ErrorBrush;
                        statusLbl.Content = "Database JSON deserialization failed!";
                        log("JSON Deserialization failed!", LogLevel.Fatal);
                        log(ex.Message, LogLevel.Fatal);
                        log(ex.StackTrace ?? "No StackTrace avalible", LogLevel.Fatal);
                        bool? result = new ConfirmDialog("Failed to load the database.", "Would you like to reset the database?", true).ShowDialog();
                        if (result == true) {
                            jsonDict = new Dictionary<string, MemberInfo>();
                            File.Create("db.json").Close();
                            statusLbl.Foreground = StatusBrush;
                            statusLbl.Content = "Database reset after deserialization error";
                        } else {
                            log("User denied database reset, fix above error or delete database file and try again.", LogLevel.Fatal);
                            Environment.Exit(-1);
                        }
                    }
                    if (jsonDict != null && jsonDict.GetType() == typeof(Dictionary<string, MemberInfo>)) {
                        checkList = jsonDict;
                        if (checkList.Count > 0) {
                            listviewID = checkList.Values.Max(m => m.ListID);
                        }
                        memberListView.ItemsSource = ListViewItems;
                    } else {
                        statusLbl.Foreground = ErrorBrush;
                        statusLbl.Content = "Database JSON deserialization failed!";
                        log("JSON Deserialization failed!", LogLevel.Fatal);
                        bool? result = new ConfirmDialog("Failed to load the database.", "Would you like to reset the database?", true).ShowDialog();
                        if (result == true) {
                            checkList = new Dictionary<string, MemberInfo>();
                            File.Create("db.json").Close();
                            statusLbl.Foreground = StatusBrush;
                            statusLbl.Content = "Database reset after deserialization error";
                        } else {
                            log("User denied database reset, delete database file and try again.", LogLevel.Fatal);
                            Environment.Exit(-1);
                        }
                    }
                }
            }
        }

        private void DbValidationError(object sender, SchemaValidationEventArgs e) {
            statusLbl.Foreground = ErrorBrush;
            statusLbl.Content = "Database validation error!";
            SystemSounds.Beep.Play();
            log("JSON DB Validation Error!", LogLevel.Fatal);
            log(e.Message, LogLevel.Fatal);
            bool? result = new ConfirmDialog("Failed to load the database.", "Would you like to reset the database?", true).ShowDialog();
            if (result == true) {
                checkList = new Dictionary<string, MemberInfo>();
                File.Create("db.json").Close();
                statusLbl.Foreground = StatusBrush;
                statusLbl.Content = "Database reset after validation error";
            } else {
                log("User denied database reset, fix above error to correct database or delete database and try again.", LogLevel.Fatal);
                Environment.Exit(-1);
            }
        }
        private void mainWindow_Closing(object sender, CancelEventArgs e) {
            bool? result = new ConfirmDialog("Are you sure you want to exit?","Today's database will be saved.", false).ShowDialog();
            if (result == true) {
                e.Cancel = false;
            } else {
                e.Cancel = true;
            }
        }
        private void mainWindow_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (!e.Handled && !memberSearchTxtBx.IsFocused) {
                memberTxtBx.Focus();
            }
        }
        private void memberTxtBx_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            if (!Regex.IsMatch(e.Text, @"^[0-9]{1,7}$")) e.Handled = true;
        }
        private void memberSeachTxtBx_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            if (!Regex.IsMatch(e.Text, @"^[0-9]{1,7}$")) e.Handled = true;
        }
        private void memberTxtBx_TextChanged(object sender, TextChangedEventArgs e) {
            memberTxtBx.Text = memberTxtBx.Text.Trim();
            if (memberTxtBx.Text.Length > 7) {
                memberTxtBx.Text = memberTxtBx.Text.Substring(0, 7);
                memberTxtBx.CaretIndex = memberTxtBx.Text.Length;
            }
        }
        private void memberSearchTxtBx_TextChanged(object sender, TextChangedEventArgs e) {
            memberSearchTxtBx.Text = memberSearchTxtBx.Text.Trim();
            if (memberSearchTxtBx.Text.Length > 7) {
                memberSearchTxtBx.Text = memberSearchTxtBx.Text.Substring(0, 7);
                memberSearchTxtBx.CaretIndex = memberSearchTxtBx.Text.Length;
            }
            if (Regex.IsMatch(memberSearchTxtBx.Text, @"^[0-9]{1,7}$")) {
                memberListView.ItemsSource = new ObservableCollection<MemberInfo>(ListViewItems.Where(m => m.MemberNum.StartsWith(memberSearchTxtBx.Text)));
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
            memberTxtBx.Focus();
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
            memberListView.ScrollIntoView(memberListView.Items[0]);
            statusLbl.Foreground = StatusBrush;
            statusLbl.Content = checkList[memNum].IsCheckedOut ? "Checked out member " + memNum : "Checked in member " + memNum;
            try {
                File.WriteAllText("db.json", JsonConvert.SerializeObject(checkList, Newtonsoft.Json.Formatting.Indented));
            } catch (Exception ex) {
                statusLbl.Foreground = ErrorBrush;
                statusLbl.Content = "Failed to write to the database!";
                SystemSounds.Beep.Play();
                log("Failed to write to DB", LogLevel.Error);
                log(ex.Message, LogLevel.Error);
                log(ex.StackTrace ?? "No StackTrace avalible", LogLevel.Error);
            }
        }
        private void closeOutBtn_Click(object sender, RoutedEventArgs e) {//aquatics@clubwestside.com
            bool? result = new ConfirmDialog("Are you sure you want to close out?", "This can't be undone.",false).ShowDialog();
            if (result == true) {
                closedOut = true;
                memberTxtBx.IsEnabled = false;
                submitBtn.IsEnabled = false;
                memberSearchTxtBx.IsEnabled = false;
                closeOutBtn.IsEnabled = false;
                closeOutMenuItem.IsEnabled = false;
                checkInAllMenuItem.IsEnabled = false;
                resetDbMenuItem.IsEnabled = false;
                log("Members who did not check in are:",LogLevel.Info);
                string emailBody = "The following members did not check in their tubes or towels today:\n";
                foreach (MemberInfo m in checkList.Values) {
                    if (m.IsCheckedOut) {
                        m.ListID = listviewID++;
                        emailBody += m.MemberNum + ", ";
                        log($"Member {m.MemberNum}: last checked out at {m.TimeModified} and checked out {m.CheckoutCount}" + (m.CheckoutCount==1?" time.":" times."),LogLevel.Info);
                    }
                }
                emailBody.Remove(emailBody.Length-1);
                log("Finished closeing out.", LogLevel.Info);
                memberListView.ItemsSource = ListViewItems;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\TubeTowelCheckInOutTool")) {
                    object k = key.GetValue("email");
                    object p = key.GetValue("sendpassword");
                    if (p != null) {
                        SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587) {
                            Credentials = new NetworkCredential("westsidetubetowel@gmail.com", p.ToString()),
                            EnableSsl = true,
                        };
                        if (k != null) {
                            MailMessage mailMessage = new MailMessage("westsidetubetowel@gmail.com", k.ToString(), "Westside Tube Towel Checkout List for " + date, emailBody);
                            try {
                                smtp.Send(mailMessage);
                                log("Close out email sent to " + k.ToString(), LogLevel.Info);
                                statusLbl.Foreground = StatusBrush;
                                statusLbl.Content = "Close out email sent! You can close the program now.";
                            } catch (Exception ex) {
                                log("Exception occured trying to send close out email!", LogLevel.Error);
                                Exception innerException = ex;
                                while (innerException != null) {
                                    log(innerException.GetType().ToString(), LogLevel.Error);
                                    log(innerException.Message, LogLevel.Error);
                                    log(innerException.StackTrace ?? "No StackTrace available", LogLevel.Error);
                                    innerException = innerException.InnerException;
                                }
                                statusLbl.Foreground = ErrorBrush;
                                statusLbl.Content = "Unable to send close out email! (Excption occured)";
                            }
                        } else {
                            log("Unable to send close out email as provided email is null!", LogLevel.Error);
                            statusLbl.Foreground = ErrorBrush;
                            statusLbl.Content = "Unable to send close out email! (No provided email)";
                        }
                    } else {
                        log("Unable to send close out email as email password is null!",LogLevel.Error);
                        statusLbl.Foreground = ErrorBrush;
                        statusLbl.Content = "Unable to send close out email! (No provided email password)";
                    }
                }
                if (!Directory.Exists("dbs")) Directory.CreateDirectory("dbs");
                if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
                File.Move("db.json", $"dbs\\db-{DateTime.Now.ToString("MM-dd-yyyy_HH.mm.ss")}.json");
                File.Move(logFile, $"logs\\log-{DateTime.Now.ToString("MM-dd-yyyy_HH.mm.ss")}.txt");
            }
        }
        private void emailMenuItem_Click(object sender, RoutedEventArgs e) {
            bool? result = new TextEntryDialog(this, "Please enter email.","This is the destinaiton to email to after closing out.", "email", true).ShowDialog();
            if (result == true) {
                statusLbl.Foreground = StatusBrush;
                statusLbl.Content = "Updated email.";
            }
        }
        private void emailPasswordMenuItem_Click(object sender, RoutedEventArgs e) {
            bool? result = new TextEntryDialog(this, "Please enter email password. Do not change unless needed.", "This is the Google App Password to the sending email.", "sendpassword", false).ShowDialog();
            if (result == true) {
                statusLbl.Foreground = StatusBrush;
                statusLbl.Content = "Updated email password.";
            }
        }
        private void closeOutMenuItem_Click(object sender, RoutedEventArgs e) {
            closeOutBtn_Click(sender, e);
        }
        private void checkInAllMenuItem_Click(object sender, RoutedEventArgs e) {
            bool? result = new ConfirmDialog("Are you sure you want to check in all memebers?", "This can't be undone.",false).ShowDialog();
            if (result == true) {
                foreach (MemberInfo m in checkList.Values) {
                    if (m.IsCheckedOut) {
                        m.IsCheckedOut = false;
                        m.LastModify = DateTime.Now;
                        m.ListID = ++listviewID;
                    }
                }
                memberListView.ItemsSource = ListViewItems;
                statusLbl.Foreground = StatusBrush;
                statusLbl.Content = "All members checked in.";
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
        }
        private void resetDbMenuItem_Click(object sender, RoutedEventArgs e) {
            bool? result = new ConfirmDialog("Are you sure you want to reset today's database?", "This will remove all checked in and out members.", false).ShowDialog();
            if (result == true) {
                checkList.Clear();
                memberListView.ItemsSource = ListViewItems;
                statusLbl.Foreground = StatusBrush;
                statusLbl.Content = "Database cleared.";
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
        }
        internal void log(string msg, LogLevel lvl) {
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
                        if (msg.StartsWith("\n")) {
                            sw.WriteLine($"\n[{DateTime.Now.ToString("HH:mm:ss")}] [{loglvl}] {msg.Remove(0,1)}");
                        } else {
                            sw.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] [{loglvl}] {msg}");
                        }
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
            public string Status { 
                get {
                    if (closedOut) return IsCheckedOut ? "Member still checked out after close." : "Checked In.";
                    return IsCheckedOut ? "Checked Out. Member has checked out " + CheckoutCount + (CheckoutCount == 1 ? " time." : " times.") : "Checked In."; 
                } 
            }

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
        public enum LogLevel {
            Info = 0,
            Warn = 1,
            Error = 2,
            Fatal = 3,
        }
    }
}
