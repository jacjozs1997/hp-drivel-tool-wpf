using HandyControl.Tools.Command;
using HP_Driver_Tool.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HP_Driver_Tool.Models
{
    public class SoftwaresResponse
    {
        public int statusCode { get; set; }
        public string statusMessage { get; set; }
        public SoftwareTypeArray data { get; set; }
    }
    public class SoftwareTypeArray
    {
        public List<SoftwareType> softwareTypes { get; set; }
    }
    public class SoftwareType : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #region Properties
        private bool m_isSelectedAll { get; set; } = true;
        public string id { get; set; }
        public string accordionName { get; set; }
        public string accordionNameEn { get; set; }
        public int count { get; set; }
        public List<SoftwareDrivers> softwareDriversList { get; set; }
        #endregion       
        private RelayCommand<ToggleButton> m_selectAllCmd;
        public RelayCommand<ToggleButton> SelectAllCmd => m_selectAllCmd;
        public bool IsSelectedAll
        {
            get { return m_isSelectedAll; }
            set
            {
                m_isSelectedAll = value;
                OnPropertyChanged();
            }
        }

        public SoftwareType()
        {
            m_selectAllCmd = new RelayCommand<ToggleButton>(SelectAll, x => true);
        }
        private void SelectAll(ToggleButton button)
        {
            bool isChecked = !button.IsChecked ?? true;
            foreach (var item in softwareDriversList)
            {
                item.IsSelected = isChecked;
            }
        }
        public void ChangeSelectAll()
        {
            IsSelectedAll = !softwareDriversList.All(i => i.IsSelected);
        }
    }
}
