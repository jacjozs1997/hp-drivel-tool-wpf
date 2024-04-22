using HandyControl.Tools.Command;
using HP_Driver_Tool.Models;
using HP_Driver_Tool.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HP_Driver_Tool.ViewModels
{
    public class MainWindowViewModel
    {
        private static ObservableCollection<SoftwareDriver> m_selected = new ObservableCollection<SoftwareDriver>();
        public static ObservableCollection<SoftwareDriver> SelectedSoftwares => m_selected;
        public static ObservableCollection<SoftwareType> Softwares => SoftwareManager.Softwares;
        public static ObservableCollection<string> OsPlatforms => SoftwareManager.OsPlatforms;
        public static ObservableCollection<string> OsVersions => SoftwareManager.OsVersions;

        private RelayCommand<SoftwareDriver> m_removeCmd;
        private RelayCommand<string> m_downloadAllCmd;
        private RelayCommand<string> m_installAllCmd;
        public RelayCommand<SoftwareDriver> RemoveCmd => m_removeCmd;
        public RelayCommand<string> DownloadAllCmd => m_downloadAllCmd;
        public RelayCommand<string> InstallAllCmd => m_installAllCmd;

        public MainWindowViewModel()
        {
            m_removeCmd = new RelayCommand<SoftwareDriver>(Remove, x => true);
            m_downloadAllCmd = new RelayCommand<string>(Download, x => true);
            m_installAllCmd = new RelayCommand<string>(Install, x => true);
        }
        private void Remove(SoftwareDriver drive)
        {
            drive.Parent.IsSelected = false;
        }
        private void Download(string args)
        {
            //List<Task> TaskList = new List<Task>();
            string directory = GetDownloadFolderPath();
            if (App.DeviceProductNumber != null && !Directory.Exists(Path.Combine(directory, App.DeviceProductNumber)))
            {
                directory = Path.Combine(directory, App.DeviceProductNumber);
                Directory.CreateDirectory(directory);
            }

            foreach (var drive in m_selected)
            {
                drive.Percent = 0;
                using (var client = new WebClient())
                {
                    client.DownloadProgressChanged += (s, e) =>
                    {
                        drive.Percent = e.ProgressPercentage;
                    };
                    client.DownloadFileAsync(new Uri(drive.fileUrl), Path.Combine(directory, $"{ToUrlSlug(drive.title)}--{drive.version.Replace('.', '-')}.exe"));
                }
            }

            //Task.WaitAll(TaskList.ToArray());
        }
        private void Install(string args)
        {
         
        }
        string GetDownloadFolderPath()
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
        }
        public static string ToUrlSlug(string value)
        {

            //First to lower case
            value = value.ToLowerInvariant();

            //Remove all accents
            var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(value);
            value = Encoding.ASCII.GetString(bytes);

            //Replace spaces
            value = Regex.Replace(value, @"\s", "-", RegexOptions.Compiled);

            //Remove invalid chars
            value = Regex.Replace(value, @"[^a-z0-9\s-_]", "", RegexOptions.Compiled);

            //Trim dashes from end
            value = value.Trim('-', '_');

            //Replace double occurences of - or _
            value = Regex.Replace(value, @"([-_]){2,}", "$1", RegexOptions.Compiled);

            return value;
        }
    }
}
