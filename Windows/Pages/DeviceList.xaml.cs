﻿using MiningImpactSensor.Controls;
using SensorTag;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MiningImpactSensor.Pages
{
    public sealed partial class DeviceList : UserControl
    {
        ObservableCollection<TileModel> tiles = new ObservableCollection<TileModel>();

        public DeviceList()
        {
            this.InitializeComponent();

            SensorList.ItemsSource = tiles;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        async void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.F5)
            {
                await this.Scan();
            }
        }

        public async void Show()
        {
            await this.Scan();
        }

        public void Hide()
        {

        }

        bool finding;
        private async Task Scan()
        {
            try
            {
                if (finding)
                {
                    return;
                }
                finding = true;

                HideHelp();

                tiles.Clear();

                App.Debug("Looking for sensors");
                List<SensorTag> pairedSensors = await SensorTag.FindAllMotionSensors();
                foreach (SensorTag sensor in pairedSensors)
                {
                    App.Debug("Name=" + sensor.DeviceName + ", Id=" + sensor.DeviceId);
                    
                    tiles.Add(new TileModel() { Caption = sensor.AssignedToName, Icon = new BitmapImage(new Uri("ms-appx:/Assets/Accelerometer.png")), UserData = sensor });
                    sensor.MovementDataChanged += OnMovementMeasurementValueChanged;

                    if (sensor.Connected)
                    {
                        App.SetSelectedSensorTag(sensor);
                        Boolean success = await sensor.ConnectMotionService();
                    }
                }

                if (tiles.Count == 0)
                {
                    ShowHelp();
                }

            }
            catch (Exception ex)
            {
                DisplayMessage("Finding devices failed, please make sure your Bluetooth radio is on.  Details: " + ex.Message);
                ShowHelp();
            }

            finding = false;
        }

        private void OnMovementMeasurementValueChanged(object sender, SensorTag.MovementDataChangedEventArgs movementData)
        {
            SensorTag sensor = (SensorTag)sender;
            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                string caption = Math.Round(movementData.X, 3) + "," + Math.Round(movementData.Y, 3) + "," + Math.Round(movementData.Z, 3);
                var a = GetTile(sensor.AssignedToName);
                if (a != null)
                {
                    a.SensorValue = caption;
                }
            }));

            DevicePage.PostAsJsonAsync(movementData);
        }

        private TileModel GetTile(string name)
        {
            return (from t in tiles where t.Caption == name select t).FirstOrDefault();
        }

        private void ShowHelp()
        {
            Help.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void HideHelp()
        {
            Help.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void DisplayMessage(string message)
        {
            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                ErrorMessage.Text = message;
            }));
        }

        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            TileModel tile = e.ClickedItem as TileModel;
            SensorTag sensor = (SensorTag)tile.UserData;
            Frame frame = Window.Current.Content as Frame;
            frame.Navigate(typeof(DevicePage), sensor);
        }

        private async void OnRefresh(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsEnabled = false;
            await this.Scan();
            RefreshButton.IsEnabled = true;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Scan();
        }
    }
}
