using HandyControl.Tools.Command;
using HP_Driver_Tool.Models;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace HP_Driver_Tool.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private static ObservableCollection<SoftwareDriver> m_selected = new ObservableCollection<SoftwareDriver>();
        public static ObservableCollection<SoftwareDriver> SelectedSoftwares => m_selected;
        public static ObservableConcurrentCollection<SoftwareType> Softwares => SoftwareManager.Softwares;
        public static ObservableConcurrentCollection<string> OsPlatforms => SoftwareManager.OsPlatforms;
        public static ObservableConcurrentCollection<string> OsVersions => SoftwareManager.OsVersions;

        private RelayCommand<SoftwareDriver> m_removeCmd;
        private RelayCommand<string> m_downloadAllCmd;
        private RelayCommand<string> m_installAllCmd;
        public RelayCommand<SoftwareDriver> RemoveCmd => m_removeCmd;
        public RelayCommand<string> DownloadAllCmd => m_downloadAllCmd;
        public RelayCommand<string> InstallAllCmd => m_installAllCmd;

        private bool m_loading = false;
        private bool m_isValid = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public delegate void InvalidSearch(string message);
        public delegate void ValidSearch();
        public delegate void SelectPlatform();

        public InvalidSearch InvalidSearchHandler;
        public ValidSearch ValidSearchHandler;
        public SelectPlatform SelectPlatformHandler;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public bool Loading
        {
            get { return m_loading; }
            set
            {
                m_loading = value;
                OnPropertyChanged();//For .Net<4.5, use OnPropertyChanged("IsSelected")
            }
        }        
        public bool IsValid
        {
            get { return m_isValid; }
            set
            {
                m_isValid = value;
                OnPropertyChanged();//For .Net<4.5, use OnPropertyChanged("IsSelected")
            }
        }

        public MainWindowViewModel()
        {
            m_removeCmd = new RelayCommand<SoftwareDriver>(Remove, x => true);
            m_downloadAllCmd = new RelayCommand<string>(Download, x => true);
            m_installAllCmd = new RelayCommand<string>(Install, x => true);
        }
        private void Remove(SoftwareDriver drive)
        {
            drive.Parent.IsSelected = false;
            if (drive.DownloadTask.Status == System.Threading.Tasks.TaskStatus.Running)
            {
                //drive.DownloadTask?.Dispose(); TODO Cancelling CancellationToken
                //TODO Delete downloaded part file
            }
        }
        private void Download(string args)
        {
            string directory = GetDownloadFolderPath();
            if (App.DeviceProductNumber != null)
            {
                directory = Path.Combine(directory, App.DeviceProductNumber);
            }
            if (App.DeviceProductNumber != null && !Directory.Exists(Path.Combine(directory, App.DeviceProductNumber)))
            {
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
                    drive.filePath = Path.Combine(directory, $"{ToUrlSlug(drive.title)}--{drive.version.Replace('.', '-')}.exe");
                    client.DownloadFileAsync(new Uri(drive.fileUrl), drive.filePath);
                }
            }
        }
        private void Install(string args)
        {
            string directory = GetDownloadFolderPath();
            if (App.DeviceProductNumber != null)
            {
                directory = Path.Combine(directory, App.DeviceProductNumber);
            }
            if (App.DeviceProductNumber != null && !Directory.Exists(Path.Combine(directory, App.DeviceProductNumber)))
            {
                Directory.CreateDirectory(directory);
            }
            foreach (var drive in m_selected)
            {
                drive.Percent = 0;
                var path = Path.Combine(directory, $"{ToUrlSlug(drive.title)}--{drive.version.Replace('.', '-')}");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                ExtractFile(drive.filePath, path);

                path = Path.Combine(path, "install.cmd");
                if (File.Exists(path))
                {
                    Process.Start(path);
                } 
                else
                {
                    //TODO
                }
            }
        }
        public void ExtractFile(string sourceArchive, string destination)
        {
            string zPath = @".\7zip\7za.exe"; //add to proj and set CopyToOuputDir
            try
            {
                ProcessStartInfo pro = new ProcessStartInfo();
                //pro.WindowStyle = ProcessWindowStyle.Hidden;
                pro.FileName = zPath;
                pro.Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", sourceArchive, destination);
                Process x = Process.Start(pro);
                x.WaitForExit();
            }
            catch (System.Exception Ex)
            {
                //handle error
            }
        }
        public void GetOsInfos(string productNumber = null)
        {
            Loading = true;
            SoftwareManager.GetOsInfos(productNumber, () =>
            {
                Loading = false;
                IsValid = true;
                ValidSearchHandler?.Invoke();
            }, () =>
            {
                Loading = false;
                IsValid = false;
                InvalidSearchHandler?.Invoke(null);
            });
        }
        public void UpdateOsVersion(string platform)
        {
            Loading = true;
            SoftwareManager.UpdateOsVersion(platform);
            Loading = false;
            SelectPlatformHandler?.Invoke();
        }
        public void GetDrivers(string version)
        {
            Loading = true;
            SoftwareManager.GetDrivers(version, () => Loading = false);
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
