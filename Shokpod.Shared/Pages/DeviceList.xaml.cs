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

        private void Scan1()
        {
            try
            {
                tiles.Clear();

                BleWatcher = new BluetoothLEAdvertisementWatcher
                {
                    ScanningMode = BluetoothLEScanningMode.Active
                };
                BleWatcher.Received += async (w, btAdv) => {
                    try
                    {
                        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);
                        if ((device != null) && device.Name.Contains("ShokPod"))
                        {
                            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                            {
                                var existing = tiles.Any((item) =>
                                {
                                    SensorTag temp = (SensorTag)item.UserData;
                                    return temp.DeviceId == device.DeviceId;
                                });
                                if (!existing)
                                {
                                    Debug.WriteLine($"Found ShokPod: {device.Name} - {device.BluetoothAddress} - {device.DeviceId} - {device.ConnectionStatus}");
                                    SensorTag sensor = new SensorTag(device.DeviceInformation);
                                    App.Debug("Name=" + sensor.DeviceName + ", Id=" + sensor.DeviceId);

                                    tiles.Add(new TileModel() { Caption = sensor.AssignedToName, Icon = new BitmapImage(new Uri("ms-appx:/Assets/shokpodSensorIcon150x150.png")), UserData = sensor });
                                    sensor.MovementDataChanged += OnMovementMeasurementValueChanged;
                                    HideHelp();
                                }
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception for device: {btAdv.BluetoothAddress} - {btAdv.AdvertisementType}- {ex.Message}");
                    }
                };
                BleWatcher.Start();

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
                        //DeviceList.Add(newDev);
                        tiles.Add(new TileModel() { Caption = $"{newDev.BluetoothAddressAsString}({newDev.Name})", Icon = new BitmapImage(new Uri("ms-appx:/Assets/shokpodSensorIcon150x150.png")), UserData = newDev });
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
            ObservableBluetoothLEDevice selectedDevice = (ObservableBluetoothLEDevice)tile.UserData;
            gattContext.SelectedBluetoothLEDevice = selectedDevice;
            Frame frame = Window.Current.Content as Frame;
            frame.Navigate(typeof(DevicePage), selectedDevice);
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
