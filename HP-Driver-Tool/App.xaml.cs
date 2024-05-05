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
            ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver");

            ManagementObjectCollection objCollection = objSearcher.Get();

            foreach (ManagementObject obj in objCollection)
            {
                string info = String.Format("Device='{0}',Manufacturer='{1}',DriverVersion='{2}' ", obj["DeviceName"], obj["Manufacturer"], obj["DriverVersion"]);
                Console.WriteLine($"Device driver: {info}");
            }

            ConsoleManager.Show();

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

            int wlanExit;
            if (!ExecuteCommand("Wlan", "wlan", out wlanExit))
            {
                string message = "";
                switch (wlanExit)
                {
                    case 1:
                        message = "\"win-8-act.xml\" fájl nem található!";
                        break;
                    case 2:
                        message = "\"hpdoa-test.xml\" fájl nem található!";
                        break;
                    default:
                        break;
                }
                if (HandyControl.Controls.MessageBox.Show(message, "Fájl hiba", MessageBoxButton.OK) == MessageBoxResult.OK)
                {
                    Current.Shutdown();
                    return;
                }
            }

            ExecuteCommand("WinUpdate", "disable-updates", out _);

            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);

            if (!IsConnectedToInternet())
            {
                WifiConnaction.Instance.Disconnect();
                WifiConnaction.Instance.Connect("Win8-activation");
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ((MainWindowViewModel)MainWindow.DataContext).GetOsInfos();
                }));
            }
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            WifiConnaction.Instance.Disconnect();
            WifiConnaction.Instance.Connect("hpdoa-test");

            ExecuteCommand("WinUpdate", "reenable-updates", out _);

            ConsoleManager.Hide();

            base.OnExit(e);
        }

        public bool ExecuteCommand(string dir, string command, out int exitCode)
        {
            exitCode = 0;
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir, $"{command}.bat");
                process.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                //* Set your output and error (asynchronous) handlers
                process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
                process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);
                //* Start process and handlers
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                exitCode = process.ExitCode;
                switch (exitCode)
                {
                    case 1:
                    case 2:
                        return false;
                }

                process.Close();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("error >> " + e.Message);
                return false;
            }
        }

        public static bool IsConnectedToInternet()
        {
            try
            {
                return (new Ping().Send("google.com", 1000, new byte[32], new PingOptions()).Status == IPStatus.Success);
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
