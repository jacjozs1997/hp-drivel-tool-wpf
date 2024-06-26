﻿using HandyControl.Controls;
using HandyControl.Data;
using HP_Driver_Tool.Models;
using HP_Driver_Tool.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Windows.Controls;
using System.Windows.Media;

namespace HP_Driver_Tool.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel Context => (MainWindowViewModel)DataContext;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            sb_pnNumber.Text = App.DeviceProductNumber;

            Context.InvalidSearchHandler += InvalidSearch;
            Context.ValidSearchHandler += ValidSearch;
            Context.SelectPlatformHandler += SelectPlatform;
            Context.SelectVersionHandler += SelectVersion;
        }
        public void InvalidSearch(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var danger = FindResource("DangerBrush") as LinearGradientBrush;
                sb_pnNumber.BorderBrush = danger;

                Growl.Error(new GrowlInfo
                {
                    Message = message,
                    ShowDateTime = false,
                    Token = Context.Token
                });
            }));
        }
        public void ValidSearch()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var primary = FindResource("PrimaryBrush") as LinearGradientBrush;
                sb_pnNumber.BorderBrush = primary;
                cb_osPlatforms.Focus();
                cb_osPlatforms.SelectedValue = App.DeviceOS;
            }));
        }        
        public void SelectPlatform()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                cb_osVersions.Focus();
            }));
        }
        public void SelectVersion(string version)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                cb_osVersions.SelectedValue = version; 
            }));
        }
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SoftwareType swType = null;
            foreach (SoftwareDrivers item in e.RemovedItems)
            {
                swType = item.Parent;
                MainWindowViewModel.SelectedSoftwares.Remove(item.latestVersionDriver);
            }
            foreach (SoftwareDrivers item in e.AddedItems)
            {
                swType = item.Parent;
                MainWindowViewModel.SelectedSoftwares.Add(item.latestVersionDriver);
            }
            swType.ChangeSelectAll();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ComboBox comboBox = sender as System.Windows.Controls.ComboBox;
            Context.UpdateOsVersion(comboBox.SelectedValue as string);
        }

        private void SearchBar_SearchStarted(object sender, FunctionEventArgs<string> e)
        {
            Context.GetOsInfos(e.Info);
        }
        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ComboBox comboBox = sender as System.Windows.Controls.ComboBox;
            Context.GetDrivers(comboBox.SelectedValue as string);
        }
    }
}
