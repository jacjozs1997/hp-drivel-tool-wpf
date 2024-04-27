using HandyControl.Tools.Command;
using HP_Driver_Tool.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace HP_Driver_Tool.Models
{
    public class SoftwareDrivers : INotifyPropertyChanged
    {
        private bool isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged();//For .Net<4.5, use OnPropertyChanged("IsSelected")
            }
        }
        
        public SoftwareType Parent { get; set; }
        public SoftwareDriver latestVersionDriver { get; set; }
        public List<SoftwareDriver> previousVersionOfDriversList { get; set; }
    }
    public class SoftwareDriver : INotifyPropertyChanged
    {
        private int percent;
        private Style m_progressBarStyle = Application.Current.TryFindResource(typeof(ProgressBar)) as Style;
        private bool m_progressBarShow = false;

        public SoftwareDrivers Parent { get; set; }
        public WebClient DownloadClient { get; set; }
        public string SoftwareId { get; set; }
        public string title { get; set; }
        public string version { get; set; }
        private DateTime m_versionUpdatedDate { get; set; }
        public DateTime VersionUpdatedDate => m_versionUpdatedDate;
        public string versionUpdatedDateString
        {
            get { return this.m_versionUpdatedDate.ToString("yyyy-MM-dd"); }
            set { this.m_versionUpdatedDate = DateTime.Parse(value); }
        }
        public string fileSize { get; set; }
        public string fileUrl { get; set; }
        public string filePath { get; set; }
        private RelayCommand<SoftwareDriver> m_removeCmd;
        private RelayCommand<SoftwareDriver> m_openFolderCmd;
        public RelayCommand<SoftwareDriver> RemoveCmd => m_removeCmd;
        public RelayCommand<SoftwareDriver> OpenFolderCmd => m_openFolderCmd;
        public override bool Equals(object obj)
        {
            return obj is SoftwareDriver driver &&
                   title == driver.title &&
                   version == driver.version;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public int Percent
        {
            get { return percent; }
            set
            {
                percent = value;
                OnPropertyChanged();//For .Net<4.5, use OnPropertyChanged("IsSelected")
            }
        }
        public Style ProgressBarStyle
        {
            get { return m_progressBarStyle; }
            set
            {
                m_progressBarStyle = value;
                OnPropertyChanged();//For .Net<4.5, use OnPropertyChanged("IsSelected")
            }
        }        
        public bool ProgressBarShow
        {
            get { return m_progressBarShow; }
            set
            {
                m_progressBarShow = value;
                OnPropertyChanged();//For .Net<4.5, use OnPropertyChanged("IsSelected")
            }
        }
        public SoftwareDriver()
        {
            m_removeCmd = new RelayCommand<SoftwareDriver>(Remove, x => true);
            m_openFolderCmd = new RelayCommand<SoftwareDriver>(OpenFolder, x => true);
        }
        private void Remove(SoftwareDriver drive)
        {
            drive.Parent.IsSelected = false;
            drive.DownloadClient?.CancelAsync();
            drive.ProgressBarStyle = Application.Current.TryFindResource(typeof(ProgressBar)) as Style;
            drive.ProgressBarShow = false;
            drive.Percent = 0;
            if (drive.filePath != null)
            {
                File.Delete(drive.filePath);
                Directory.Delete(Path.GetDirectoryName(drive.filePath));
            }
        }
        private void OpenFolder(SoftwareDriver drive)
        {
            if (drive.filePath != null)
                Process.Start("explorer.exe", Path.GetDirectoryName(drive.filePath));
        }
        public override int GetHashCode()
        {
            int hashCode = 1557441032;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(title);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(version);
            return hashCode;
        }
    }
}
