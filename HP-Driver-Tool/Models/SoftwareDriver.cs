using HandyControl.Tools.Command;
using HP_Driver_Tool.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        public SoftwareDrivers Parent { get; set; }
        public string SoftwareId { get; set; }
        public string title { get; set; }
        public string version { get; set; }
        public string versionUpdatedDateString { get; set; }
        public string fileSize { get; set; }
        public string fileUrl { get; set; }
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
        public override int GetHashCode()
        {
            int hashCode = 1557441032;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(title);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(version);
            return hashCode;
        }
    }
}
