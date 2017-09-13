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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;


namespace MiningImpactSensor.Pages
{
    public sealed partial class DevicePage : Page
    {
        private static string CURRENT_IMPACT = "Current Impact";
        private static string CANT_CONNECT= "Can't Connect";

        ObservableCollection<TileModel> tiles = new ObservableCollection<TileModel>();
        LiveTileUpdater liveTileUpdater;
        DispatcherTimer loggedOnIndicatorTimer;
        private static DevicePage SelectedDevicePage;
        private bool displayAcceleration;

        public DevicePage()
        {
            this.InitializeComponent();

            Clear();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ConnectToSelectedDevice(e);
            //ConnectAsSensorTag(e);
            base.OnNavigatedTo(e);
        }

        public async void ConnectToSelectedDevice(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ConnectToSelectedDevice: Entering");
            GattSampleContext.Context.StopEnumeration();


            ObservableBluetoothLEDevice SelectedDevice = e.Parameter as ObservableBluetoothLEDevice;
            System.Diagnostics.Debug.WriteLine("ConnectToSelectedDevice: Trying to connect to " + SelectedDevice.Name);

            if (await SelectedDevice.Connect() == false)
            {
                System.Diagnostics.Debug.WriteLine("ConnectToSelectedDevice: Something went wrong getting the BluetoothLEDevice");
                SelectedDevice = null;
                //NavigationService.Navigate(typeof(Views.Discover));
                return;
            }

            System.Diagnostics.Debug.WriteLine("ConnectToSelectedDevice: Going to Device Service Page");
            System.Diagnostics.Debug.WriteLine("ConnectToSelectedDevice: Exiting");
        }

        private void ConnectAsSensorTag(NavigationEventArgs e)
        {
            ((App)Application.Current).SensorTag = e.Parameter as SensorTag;

            SensorList.ItemsSource = tiles;

            ((App)Application.Current).SensorTag.MovementDataChanged += OnMovementMeasurementValueChanged;
            ((App)Application.Current).SensorTag.OnConnected += OnConnected;

            ((App)Application.Current).SensorTag.ConnectMotionService();

            this.displayAcceleration = ShokpodSettings.getSettings().Result.DisplayAcceleration;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //TODO: ((App)Application.Current).SensorTag.MovementDataChanged -= OnMovementMeasurementValueChanged;
            //TODO: ((App)Application.Current).SensorTag.OnConnected -= OnConnected;
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

        private void OnConnected(object sender, SensorTag.ConnectedEventArgs args)
        {
            this.ProgressRing.IsActive = false;
            if (args.success)
            {
                AddTile(new TileModel() { Caption = CURRENT_IMPACT, Icon = new BitmapImage(new Uri("ms-appx:/Assets/shokpodSensorIcon150x150.png")) });
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

        public static void PostAsJsonAsync(SensorTag.MovementDataChangedEventArgs movementData)
        {
            SensorTag sensor = ((App)Application.Current).SensorTag;
            MovementRecord record = new MovementRecord();
            record.AssignedName = sensor.AssignedToName;
            record.DeviceAddress = sensor.DeviceAddress;
            SingleRecord singleRecord = new SingleRecord();
            singleRecord.Time = DateTime.Now;
            singleRecord.Value = new MovementMeasurement(movementData.X, movementData.Y, movementData.Z);
            record.Recording.Add(singleRecord);

            RecordingQueue.AddToDeviceQueue(record);
        }

        internal static async void Debug(string message)
        {
            App.Debug(message);
            if (SelectedDevicePage != null)
            {
                await SelectedDevicePage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    if (SelectedDevicePage != null)
                    {
                        SelectedDevicePage.DebugMessage.Text = message;
                    }
                });
            }
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

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
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
            App.SetSensorTagName(this.AssignedToTextBox.Text);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.AssignedToTextBox.Text = "HARDCODED"; //App.getSelectedSensorTag().AssignedToName;
            liveTileUpdater = new LiveTileUpdater();
            liveTileUpdater.Start();
            startLoggedOnTimer();
            SelectedDevicePage = this;
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).SensorTag.Connected = false;
            PersistedDevices persistedDevices = await PersistedDevices.getPersistedDevices();
            persistedDevices.saveDevice(((App)Application.Current).SensorTag);
            ((App)Application.Current).SensorTag.Disconnect();
            setCurrentImpct("");
            Frame.GoBack();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            liveTileUpdater.Stop();
            loggedOnIndicatorTimer.Stop();
            loggedOnIndicatorTimer = null;
            SelectedDevicePage = null;
        }

        void startLoggedOnTimer()
        {
            loggedOnIndicatorTimer = new Windows.UI.Xaml.DispatcherTimer();
            loggedOnIndicatorTimer.Tick += (object sender, object e) => {
                SensorTag sensor = App.getSelectedSensorTag();
                if ((sensor != null) && (sensor.Connected) && (sensor.DateTimeConnected.Year != 1))
                {
                    TimeSpan ts = App.getSelectedSensorTag().DateTimeConnected - DateTime.Now;
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
