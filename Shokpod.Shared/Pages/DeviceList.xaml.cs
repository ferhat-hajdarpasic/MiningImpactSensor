using MiningImpactSensor.Controls;
using SensorTag;
using Shokpod10;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
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
        public BluetoothLEAdvertisementWatcher BleWatcher { get; private set; }
        private GattSampleContext gattContext;

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
            gattContext = GattSampleContext.Context;
            this.Scan();
            gattContext.BluetoothLEDevices.CollectionChanged += BluetoothLEDevices_CollectionChanged;
        }

        public void Hide()
        {
            gattContext.BluetoothLEDevices.CollectionChanged -= BluetoothLEDevices_CollectionChanged;
        }
        private void Scan()
        {
            gattContext.StartEnumeration();
        }

        private object deviceListLock = new object();
        private void BluetoothLEDevices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            string msg = string.Empty;
            lock (deviceListLock)
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    foreach (ObservableBluetoothLEDevice newDev in e.NewItems)
                    {
                        SensorTag sensorTag = new SensorTag(newDev);
                        tiles.Add(new TileModel() { Caption = $"{newDev.BluetoothAddressAsString}({newDev.Name})", Icon = new BitmapImage(new Uri("ms-appx:/Assets/shokpodSensorIcon150x150.png")), UserData = sensorTag });
                    }
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    foreach (ObservableBluetoothLEDevice oldDev in e.OldItems)
                    {
                        //DeviceList.Remove(oldDev);
                    }
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                {
                    //DeviceList.Clear();
                }
            }

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
            SensorTag sensorTag = (SensorTag)tile.UserData;
            gattContext.SelectedBluetoothLEDevice = sensorTag.Device;
            Frame frame = Window.Current.Content as Frame;
            frame.Navigate(typeof(DevicePage), sensorTag);
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsEnabled = false;
            this.Scan();
            RefreshButton.IsEnabled = true;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            tiles.Clear();
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
