using HandyControl.Tools.Command;
using HP_Driver_Tool.Models;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace HP_Driver_Tool.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private static ObservableCollection<SoftwareDriver> m_selected = new ObservableCollection<SoftwareDriver>();
        public static ObservableCollection<SoftwareDriver> SelectedSoftwares => m_selected;
        public static ObservableConcurrentCollection<SoftwareType> Softwares => SoftwareManager.Softwares;
        public static ObservableConcurrentCollection<string> OsPlatforms => SoftwareManager.OsPlatforms;
        public static ObservableConcurrentCollection<string> OsVersions => SoftwareManager.OsVersions;

        private RelayCommand<string> m_downloadAllCmd;
        private RelayCommand<string> m_installAllCmd;
        public RelayCommand<string> DownloadAllCmd => m_downloadAllCmd;
        public RelayCommand<string> InstallAllCmd => m_installAllCmd;

        private bool m_loading = false;
        private bool m_isValid = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public delegate void InvalidSearch(string message);
        public delegate void ValidSearch();
        public delegate void SelectPlatform();
        public delegate void SelectVersion(string version);

        public InvalidSearch InvalidSearchHandler;
        public ValidSearch ValidSearchHandler;
        public SelectPlatform SelectPlatformHandler;
        public SelectVersion SelectVersionHandler;

        public string Token { get; set; } = nameof(MainWindowViewModel);

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

        public string Title 
        { 
            get
            {
                string bios = "";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(new SelectQuery(@"SELECT * FROM Win32_BIOS")))
                {
                    foreach (ManagementObject process in searcher.Get())
                    {
                        bios = ((string[])process["BIOSVersion"]).Length > 1 ? ((string[])process["BIOSVersion"])[0] + " - " + ((string[])process["BIOSVersion"])[1] : ((string[])process["BIOSVersion"])[0];
                    }
                }
                return $"HP Driver Tool | PN: {App.DeviceProductNumber} | OS: {App.DeviceOS} | Version: {App.DeviceOsVersion} | Bios Ver: {bios}";
            } 
        }

        public MainWindowViewModel()
        {
            m_downloadAllCmd = new RelayCommand<string>(Download, x => true);
            m_installAllCmd = new RelayCommand<string>(Install, x => true);
        }
        private void Download(string args)
        {
            try
            {
                string directory = GetDownloadFolderPath();
                if (SoftwareManager.ProductNumber != null)
                {
                    directory = Path.Combine(directory, SoftwareManager.ProductNumber);
                }
                if (SoftwareManager.ProductNumber != null && !Directory.Exists(Path.Combine(directory, SoftwareManager.ProductNumber)))
                {
                    Directory.CreateDirectory(directory);
                }
                foreach (var drive in m_selected)
                {
                    drive.Percent = 0;
                    drive.ProgressBarStyle = Application.Current.TryFindResource(typeof(ProgressBar)) as Style;
                    drive.ProgressBarShow = true;
                    drive.filePath = Path.Combine(directory, $"{ToUrlSlug(drive.title)}--{drive.version.Replace('.', '-')}.exe");
                    Console.WriteLine($"Download: {drive.title} - {drive.version}");
                    var client = new WebClient();
                    client.DownloadProgressChanged += (s, e) =>
                    {
                        drive.Percent = e.ProgressPercentage;
                    };
                    client.DownloadFileCompleted += (s, e) =>
                    {
                        if (e.Cancelled)
                        {
                            client.Dispose();
                            drive.DownloadClient = null;
                            File.Delete(drive.filePath);
                            drive.ProgressBarStyle = Application.Current.Resources["ProgressBarDanger"] as Style;
                            Console.WriteLine($"Download fail: {drive.title} - {drive.version}");
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"Downloaded: {drive.title} - {drive.version}");
                            drive.ProgressBarStyle = Application.Current.Resources["ProgressBarSuccess"] as Style;
                        }
                    };
                    drive.DownloadClient = client;
                    drive.DownloadClient.DownloadFileAsync(new Uri(drive.fileUrl), drive.filePath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private void Install(string args)
        {
            try
            {
                string directory = GetDownloadFolderPath();
                if (SoftwareManager.ProductNumber != null)
                {
                    directory = Path.Combine(directory, SoftwareManager.ProductNumber);
                }
                if (SoftwareManager.ProductNumber != null && !Directory.Exists(Path.Combine(directory, SoftwareManager.ProductNumber)))
                {
                    Directory.CreateDirectory(directory);
                }
                foreach (var drive in m_selected)
                {
                    if (drive.DownloadClient?.IsBusy ?? false) continue;
                    var path = Path.Combine(directory, $"{ToUrlSlug(drive.title)}--{drive.version.Replace('.', '-')}");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    ExtractFile(drive.filePath, path);

                    string installPath = Path.Combine(path, "install.cmd");
                    if (File.Exists(installPath))
                    {
                        Console.WriteLine($"Start: {installPath}");
                        Process process = new Process();
                        process.StartInfo.FileName = installPath;
                        process.StartInfo.WorkingDirectory = path;
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

                        process.Close();
                    }
                    else
                    {
                        var files = Directory.GetFiles(path);
                        if (files.Length == 1)
                        {
                            Console.WriteLine($"Start: {files[0]}");
                            Process process = new Process();
                            process.StartInfo.FileName = files[0];
                            process.StartInfo.WorkingDirectory = path;
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

                            process.Close();
                        }
                        //TODO
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void ExtractFile(string sourceArchive, string destination)
        {
            string zPath = @".\7zip\7za.exe"; //add to proj and set CopyToOuputDir
            try
            {
                Console.WriteLine($"Extract: {sourceArchive}");

                Process process = new Process();
                process.StartInfo.FileName = zPath;
                process.StartInfo.Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", sourceArchive, destination);
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

                process.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void GetOsInfos(string productNumber = null)
        {
            try
            {
                Loading = true;
                SoftwareManager.GetOsInfos(productNumber, () =>
                {
                    Loading = false;
                    IsValid = true;
                    ValidSearchHandler?.Invoke();
                    if (productNumber == null)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            UpdateOsVersion();
                        });
                    }
                }, (msg) =>
                {
                    Loading = false;
                    IsValid = false;
                    InvalidSearchHandler?.Invoke(msg);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void UpdateOsVersion(string platform = null)
        {
            try
            {
                Loading = true;
                SoftwareManager.UpdateOsVersion(platform);
                Loading = false;
                SelectPlatformHandler?.Invoke();
                if (platform == null)
                {
                    GetDrivers();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void GetDrivers(string version = null)
        {
            try
            {
                Loading = true;
                string finalVersion;
                SoftwareManager.GetDrivers(version, out finalVersion, () => Loading = false);
                if (finalVersion != version && finalVersion != null && version == null)
                {
                    SelectVersionHandler?.Invoke(finalVersion);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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
