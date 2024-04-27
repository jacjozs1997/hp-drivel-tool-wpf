using HP_Driver_Tool.Models;
using OSVersionExtension;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Management;
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

            Process.Start("netsh", "wlan add profile filename=win-8-act.xml").WaitForExit();
            Process.Start("netsh", "wlan add profile filename=hpdoa-test.xml").WaitForExit();

            Process.Start("netsh", "wlan connect ssid=Win8-activation name=Win8-activation").WaitForExit();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Process.Start("netsh", "wlan disconnect").WaitForExit();
            Process.Start("netsh", "wlan connect ssid=hpdoa-test name=hpdoa-test").WaitForExit();
            base.OnExit(e);
        }
    }
}
