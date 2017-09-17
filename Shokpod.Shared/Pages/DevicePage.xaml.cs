using MiningImpactSensor.Controls;
using Newtonsoft.Json;
using SensorTag;
using Shokpod10;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static MiningImpactSensor.SensorTag;
using static SensorTag.ObservableBluetoothLEDevice;

namespace MiningImpactSensor.Pages
{
    public sealed partial class DevicePage : Page
    {
        private static string CURRENT_IMPACT = "Current Impact";
        private static string CANT_CONNECT= "Can't Connect";

        ObservableCollection<TileModel> tiles = new ObservableCollection<TileModel>();
        LiveTileUpdater liveTileUpdater;
        DispatcherTimer loggedOnIndicatorTimer;
        private bool displayAcceleration;
        private SensorTag SelectedSensorTag;

        public DevicePage()
        {
            this.InitializeComponent();

            Clear();

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            SelectedSensorTag = e.Parameter as SensorTag;

            SensorList.ItemsSource = tiles;

            SelectedSensorTag.MovementDataChanged += OnMovementMeasurementValueChanged;

            ConnectionResult result = await SelectedSensorTag.Connect();
            if ((result.Result == null) && (result.Exception == null) && (result.Success == false))
            {
                MessageDialog dialog = new MessageDialog("No permission to access device", "Connection error");
                await dialog.ShowAsync();
                //device = null;
                return;
            }
            if (result.Result.Status == GattCommunicationStatus.Success)
            {
            }
            if (result.Result.Status == GattCommunicationStatus.ProtocolError)
            {
                Debug($"Connection protocol error: {result.Result.ProtocolError.Value.ToString()}. Connection failures");
            }
            else if (result.Result.Status == GattCommunicationStatus.Unreachable)
            {
                Debug($"Connection protocol error: Device unreachable");
            }
            else if(result.Exception != null)
            {
                string msg = String.Format("Message:\n{0}\n\nInnerException:\n{1}\n\nStack:\n{2}", result.Exception.Message, result.Exception.InnerException, result.Exception.StackTrace);
                var messageDialog = new MessageDialog(msg, "Exception");
                await messageDialog.ShowAsync();
            }

            InidicateConnectionStatus(result);

            this.displayAcceleration = ShokpodSettings.getSettings().Result.DisplayAcceleration;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SelectedSensorTag.MovementDataChanged -= OnMovementMeasurementValueChanged;
            SelectedSensorTag.Disconnect();

            base.OnNavigatedFrom(e);
        }

        void OnStatusChanged(object sender, string status)
        {
            Debug(status);
        }

        double Fahrenheit(double celcius)
        {
            return celcius * 1.8 + 32.0;
        }


        string FormatTemperature(double t)
        {
            if (!Settings.Instance.Celcius)
            {
                t = Fahrenheit(t);
            }
            return t.ToString("N2");
        }

        private void OnMovementMeasurementValueChanged(object sender, SensorTag.MovementDataChangedEventArgs movementData)
        {
            if (this.displayAcceleration)
            {
                var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                {
                    string caption = Math.Round(movementData.Total, 2) + "G. [" + Math.Round(movementData.X, 2) + "," + Math.Round(movementData.Y, 2) + "," + Math.Round(movementData.Z, 2) + "]";
                    setCurrentImpct(caption);
                }));
            }

            PostAsJsonAsync(movementData);
        }

        private void InidicateConnectionStatus(ConnectionResult result)
        {
            this.ProgressRing.IsActive = false;
            if (result.Result != null)
            {
                if (result.Result.Status == GattCommunicationStatus.Success)
                {
                    AddTile(new TileModel() { Caption = CURRENT_IMPACT, Icon = new BitmapImage(new Uri("ms-appx:/Assets/shokpodSensorIcon150x150.png")) });
                } else
                {
                    AddTile(new TileModel() { Caption = CANT_CONNECT, Icon = new BitmapImage(new Uri("ms-appx:/Assets/shokpodSensorBrokenIcon150x150.png")) });
                }
            }
            else
            {
                AddTile(new TileModel() { Caption = CANT_CONNECT, Icon = new BitmapImage(new Uri("ms-appx:/Assets/shokpodSensorBrokenIcon150x150.png")) });
            }
        }

        private void setCurrentImpct(string caption)
        {
            var a = GetTile(CURRENT_IMPACT);
            if (a != null)
            {
                a.SensorValue = caption;
            }
        }

        public void PostAsJsonAsync(SensorTag.MovementDataChangedEventArgs movementData)
        {
            MovementRecord record = new MovementRecord();
            record.AssignedName = SelectedSensorTag.AssignedToName;
            record.DeviceAddress = SelectedSensorTag.DeviceAddress;
            SingleRecord singleRecord = new SingleRecord();
            singleRecord.Time = DateTime.Now;
            singleRecord.Value = new MovementMeasurement(movementData.X, movementData.Y, movementData.Z);
            record.Recording.Add(singleRecord);

            RecordingQueue.SingleRecordingQueue.AddToDeviceQueue(record);
        }

        internal async void Debug(string message)
        {
            App.Debug(message);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                this.DebugMessage.Text = message;
            });
        }

        private TileModel GetTile(string name)
        {
            return (from t in tiles where t.Caption == name select t).FirstOrDefault();
        }

        void OnServiceError(object sender, string message)
        {
            Debug(message);
        }

        private void Clear()
        {
            foreach (var tile in tiles)
            {
                tile.SensorValue = "";
            }
        }

        public void Show()
        {
        }

        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            TileModel tile = e.ClickedItem as TileModel;
            if (tile != null)
            {
                SelectTile(tile);
            }
        }

        private void SelectTile(TileModel model)
        {
            Frame frame = Window.Current.Content as Frame;

            switch (model.Caption)
            {
                case "Accelerometer":
                    break;
            }
        }


        private void AddTile(TileModel model)
        {
            if (!(from t in tiles where t.Caption == model.Caption select t).Any())
            {
                tiles.Add(model);
            }
        }

        private void RemoveTiles(IEnumerable<TileModel> enumerable)
        {
            foreach (TileModel tile in enumerable.ToArray())
            {
                tiles.Remove(tile);
            }
        }

        private void OnGoBack(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private void AssignedToTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SelectedSensorTag.AssignedToName != this.AssignedToTextBox.Text)
            {
                SelectedSensorTag.AssignedToName = this.AssignedToTextBox.Text;
                PersistedDevices.singleInstance.saveDevice(SelectedSensorTag);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.AssignedToTextBox.Text = SelectedSensorTag.AssignedToName;
            liveTileUpdater = new LiveTileUpdater();
            liveTileUpdater.Start();
            startLoggedOnTimer();

            RecordingQueue.SingleRecordingQueue.OnRecordingEvent += OnRecordingQueueEvent;
        }

        private void OnRecordingQueueEvent(object sender, RecordingQueue.RecordingQueueEventArgs e)
        {
            if (e.Record != null)
            {
                Debug("Persisted acceleration of " + e.Record.Value.Acceleration + "G.");
                MetroEventSource.ToastAsync("Persisted acceleration of " + e.Record.Value.Acceleration + "G.");
            }
            else if(e.Response != null)
            {
                Debug("HTTP call to the ShokPod remote server failed with error: '" + e.Response + ".'");
            } else if(e.Exception != null)
            {
                Debug("Exception while saving data to the remote server." + e.Exception.Message);
            } else if(e.MaximumImpact != null)
            {
                Debug("Recording acceleration of " + e.MaximumImpact.Value.Acceleration + "G.");
            }
        }

        private void LogoutButtonClick(object sender, RoutedEventArgs e)
        {
            PersistedDevices.singleInstance.saveDevice(SelectedSensorTag);
            setCurrentImpct("");
            Frame.GoBack();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            liveTileUpdater.Stop();
            loggedOnIndicatorTimer.Stop();
            loggedOnIndicatorTimer = null;
            RecordingQueue.SingleRecordingQueue.OnRecordingEvent -= OnRecordingQueueEvent;
        }

        void startLoggedOnTimer()
        {
            loggedOnIndicatorTimer = new Windows.UI.Xaml.DispatcherTimer();
            loggedOnIndicatorTimer.Tick += (object sender, object e) => {
                if ((SelectedSensorTag != null) && (SelectedSensorTag.Connected) && (SelectedSensorTag.DateTimeConnected.Year != 1))
                {
                    TimeSpan ts = SelectedSensorTag.DateTimeConnected - DateTime.Now;
                    this.LoggedOnTimeTextBox.Text = "Logged on time: " + String.Format("{0:dd\\.hh\\:mm\\:ss}", ts);
                }
                else
                {
                    this.LoggedOnTimeTextBox.Text = "Logged on time: " + "[not connected]";
                }
            };
            loggedOnIndicatorTimer.Interval = new TimeSpan(0, 0, 1);
            loggedOnIndicatorTimer.Start();
        }
    }
}
