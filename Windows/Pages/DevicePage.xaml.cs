using MiningImpactSensor.Controls;
using Newtonsoft.Json;
using SensorTag;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        ObservableCollection<TileModel> tiles = new ObservableCollection<TileModel>();
        LiveTileUpdater liveTileUpdater;

        public DevicePage()
        {
            this.InitializeComponent();

            Clear();

        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            ((App)Application.Current).SensorTag = e.Parameter as SensorTag;

            SensorList.ItemsSource = tiles;

            ((App)Application.Current).SensorTag.MovementDataChanged += OnMovementMeasurementValueChanged;

            Boolean success = await ((App)Application.Current).SensorTag.ConnectMotionService();

            if(success)
            {
                AddTile(new TileModel() { Caption = CURRENT_IMPACT, Icon = new BitmapImage(new Uri("ms-appx:/Assets/Accelerometer.png")) });
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        void OnStatusChanged(object sender, string status)
        {
            DisplayMessage(status);
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
            var nowait = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                string caption = Math.Round(movementData.X, 3) + "," + Math.Round(movementData.Y, 3) + "," + Math.Round(movementData.Z, 3);
                setCurrentImpct(caption);
                updateLoggedOnTime();
            }));

            PostAsJsonAsync(movementData);
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
            record.AssignedName = sensor.DeviceName;
            record.DeviceAddress = sensor.DeviceAddress;
            SingleRecord singleRecord = new SingleRecord();
            singleRecord.Time = DateTime.Now;
            singleRecord.Value = new MovementMeasurement(movementData.X, movementData.Y, movementData.Z);
            record.Recording.Add(singleRecord);

            RecordingQueue.Enqueue(record);
        }

        private TileModel GetTile(string name)
        {
            return (from t in tiles where t.Caption == name select t).FirstOrDefault();
        }

        void OnServiceError(object sender, string message)
        {
            DisplayMessage(message);
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
            this.AssignedToTextBox.Text = App.getSelectedSensorTag().AssignedToName;
            updateLoggedOnTime();
            liveTileUpdater = new LiveTileUpdater();
            liveTileUpdater.Start();
        }

        private void updateLoggedOnTime()
        {
            TimeSpan ts = App.getSelectedSensorTag().DateTimeConnected - DateTime.Now;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
                ts.Hours, ts.Minutes, ts.Seconds);

            this.LoggedOnTimeTextBox.Text = "Logged on time: " + elapsedTime;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).SensorTag.Connected = false;
            PersistedDevices.saveDevice(((App)Application.Current).SensorTag);
            ((App)Application.Current).SensorTag.Disconnect();
            setCurrentImpct("");
            Frame.GoBack();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            liveTileUpdater.Stop();
        }
    }
}
