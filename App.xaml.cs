using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TubeTowelAppWpf {
    public partial class App : Application {
        private const string MutexName = "TubeTowelWpfApp";
        private static Mutex mutex;

        protected override void OnStartup(StartupEventArgs e) {
            mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew) {
                IntPtr handle = NativeMethods.FindWindow(null, "Tube n' Towel Check In/Out Tool");
                if (handle != IntPtr.Zero) {
                    NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
                    NativeMethods.SetForegroundWindow(handle);
                }
                Environment.Exit(0);
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e) {
            mutex?.ReleaseMutex();
            mutex?.Close();

            base.OnExit(e);
        }
    }
    internal static class NativeMethods {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        internal const int SW_RESTORE = 9;
    }
}
