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

        async void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.F5)
            {
                await this.FindSensors();
            }
        }

        public async void Show()
        {
            await this.FindSensors();
        }

        public void Hide()
        {

        }

        bool finding;
        /*
        async void accData_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var values = (await sender.ReadValueAsync()).Value.ToArray();
            var x = values[0];
            var y = values[1];
            var z = values[2];
        }
        */
        private async Task FindSensors()
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

                List<PersistedDevice> persitedDevices = await PersistedDevices.readFromFile();
                DialogDebug("Looking for sensors");
                foreach (SensorTag sensor in await SensorTag.FindAllMotionSensors())
                {
                    DialogDebug("Name=" + sensor.DeviceName + ", Id=" + sensor.DeviceId);
                    string name = Settings.Instance.FindName(sensor.DeviceAddress);
                    if (string.IsNullOrEmpty(name))
                    {
                        name = sensor.DeviceAddress;
                    }

                    tiles.Add(new TileModel() { Caption = name, Icon = new BitmapImage(new Uri("ms-appx:/Assets/Accelerometer.png")), UserData = sensor });
                    sensor.MovementDataChanged += OnMovementMeasurementValueChanged;

                    Boolean success = await sensor.ConnectMotionService();
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

        private void OnMovementMeasurementValueChanged(object sender, SensorTag.MovementDataChangedEventArgs e)
        {
            SensorTag sensor = (SensorTag)sender;
            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                string caption = Math.Round(e.X, 3) + "," + Math.Round(e.Y, 3) + "," + Math.Round(e.Z, 3);
                var a = GetTile(sensor.DeviceAddress);
                if (a != null)
                {
                    a.SensorValue = caption;
                }
            }));
        }

        private TileModel GetTile(string name)
        {
            return (from t in tiles where t.Caption == name select t).FirstOrDefault();
        }

        public static async void DialogDebug(string v)
        {
            Debug.WriteLine(v);
            //CoreDispatcher coreDispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            //await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            //{
            //    var messageDialog = new MessageDialog(v);
            //    await messageDialog.ShowAsync();
            //});
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
            await this.FindSensors();
            RefreshButton.IsEnabled = true;
        }
    }
}
