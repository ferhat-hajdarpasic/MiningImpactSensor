using MiningImpactSensor.Controls;
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

        void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.F5)
            {
                this.Scan();
            }
        }

        public void Show()
        {
            this.Scan();
        }

        public void Hide()
        {

        }

        private async void Scan()
        {
            try
            {
                HideHelp();

                tiles.Clear();

                App.Debug("Looking for sensors");
                SensorTag sensorToConnectTo = null;
                List<SensorTag> pairedSensors = await SensorTag.FindAllMotionSensors();
                foreach (SensorTag sensor in pairedSensors)
                {
                    App.Debug("Name=" + sensor.DeviceName + ", Id=" + sensor.DeviceId);

                    tiles.Add(new TileModel() { Caption = sensor.AssignedToName, Icon = new BitmapImage(new Uri("ms-appx:/Assets/shokpodSensorIcon150x150.png")), UserData = sensor });
                    sensor.MovementDataChanged += OnMovementMeasurementValueChanged;

                    if ((sensorToConnectTo == null) && sensor.Connected)
                    {
                        sensorToConnectTo = sensor;
                    } else
                    {
                        sensor.Connected = false;
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

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsEnabled = false;
            this.Scan();
            RefreshButton.IsEnabled = true;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Scan();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ShokpodSettings settings = await ShokpodSettings.getSettings();
            this.ApiLocation.Text = settings.ShokpodApiLocation;
            this.ThresholdG.Text = "" + settings.ServerImpactThreshhold;
            this.DisplayG.IsChecked = settings.DisplayAcceleration;
        }

        private async void Flyout_Closed(object sender, object e)
        {
            ShokpodSettings settings = await ShokpodSettings.getSettings();
            settings.ShokpodApiLocation = this.ApiLocation.Text;
            settings.ServerImpactThreshhold = Convert.ToDouble(this.ThresholdG.Text);
            settings.DisplayAcceleration = Convert.ToBoolean(this.DisplayG.IsChecked);
            ShokpodSettings.saveToFile(settings);
        }
    }
}
