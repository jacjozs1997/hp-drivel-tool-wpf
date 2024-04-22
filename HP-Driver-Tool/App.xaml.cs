using HP_Driver_Tool.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
        #endregion
        protected override void OnStartup(StartupEventArgs e)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(new SelectQuery(@"Select * from Win32_ComputerSystem")))
            {
                foreach (ManagementObject process in searcher.Get())
                {
                    process.Get();
                    //m_devicePn = process["Model"] as string;
                }
            }
            SoftwareManager.GetOsInfos();
            base.OnStartup(e);
        }
    }
}
