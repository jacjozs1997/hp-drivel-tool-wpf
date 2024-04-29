using HP_Driver_Tool.Models;
using HP_Driver_Tool.ViewModels;
using OSVersionExtension;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace HP_Driver_Tool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("wininet.dll")]
        public extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        #region Vaiables
        private static string m_devicePn = "3D6S5EA";
        private static string m_deviceOs = null;
        private static string m_deviceOsVersion = null;
        #endregion
        #region Properties
        public static string DeviceProductNumber => m_devicePn;
        public static string DeviceOS => m_deviceOs;
        public static string DeviceOsVersion => m_deviceOsVersion;
        #endregion
        protected override void OnStartup(StartupEventArgs e)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(new SelectQuery(@"Select * from Win32_ComputerSystem")))
            {
                foreach (ManagementObject process in searcher.Get())
                {
                    process.Get();
                    m_devicePn = process["SystemSKUNumber"] as string;
                    if (m_devicePn == null)
                    {
                       m_devicePn = process["SystemSKU"] as string;
                    }
                }
            }

            m_deviceOs = OSVersion.GetOperatingSystem().GetName();
            m_deviceOsVersion = OSVersion.MajorVersion10Properties().DisplayVersion;

            ExecuteCommand("disable-updates");

            if (!IsConnectedToInternet())
            {
                NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);
                WifiConnaction.Instance.Connect("Win8-activation");
            }
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            WifiConnaction.Instance.Disconnect();
            WifiConnaction.Instance.Connect("hpdoa-test");

            ExecuteCommand("reenable-updates");

            base.OnExit(e);
        }

        public void ExecuteCommand(string command)
        {
            try
            {
                int ExitCode;
                ProcessStartInfo ProcessInfo;
                Process process;

                ProcessInfo = new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinUpdate", $"{command}.bat"));
                ProcessInfo.CreateNoWindow = false;
                ProcessInfo.UseShellExecute = false;
                ProcessInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinUpdate");
                // *** Redirect the output ***
                ProcessInfo.RedirectStandardError = true;
                ProcessInfo.RedirectStandardOutput = true;

                process = Process.Start(ProcessInfo);
                process.WaitForExit();

                // *** Read the streams ***
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                ExitCode = process.ExitCode;
                if (!String.IsNullOrEmpty(error))
                {
                    MessageBox.Show("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
                }
                process.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("error>>" + e.Message);
            }
        }

        public static bool IsConnectedToInternet()
        {
            try
            {
                return InternetGetConnectedState(out _, 0);
            }
            catch
            {
                return false;
            }
        }

        void AddressChangedCallback(object sender, EventArgs e)
        {
            if (IsConnectedToInternet())
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ((MainWindowViewModel)MainWindow.DataContext).GetOsInfos();
                }));
            }
        }
    }
}
