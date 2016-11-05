﻿using MiningImpactSensor.Controls;
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
        DispatcherTimer _timer;
        SensorTag sensor;
        bool registeredConnectionEvents;
        ObservableCollection<TileModel> tiles = new ObservableCollection<TileModel>();

        public DevicePage()
        {
            this.InitializeComponent();

            Clear();

        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            sensor = e.Parameter as SensorTag;

            SensorList.ItemsSource = tiles;

            sensor.MovementDataChanged += OnMovementMeasurementValueChanged;

            Boolean success = await sensor.ConnectMotionService();

            if(success)
            {
                AddTile(new TileModel() { Caption = "Accelerometer", Icon = new BitmapImage(new Uri("ms-appx:/Assets/Accelerometer.png")) });
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
                var a = GetTile("Accelerometer");
                if (a != null)
                {
                    a.SensorValue = caption;
                }
            }));

            Task.Run(() =>
            {
                MovementRecord record = new MovementRecord();
                record.AssignedName = sensor.DeviceName;
                record.DeviceAddress = sensor.DeviceAddress;
                record.Time = DateTime.Now;
                record.Value = new MovementMeasurement(movementData.X, movementData.Y, movementData.Z);

                PostAsJsonAsync(sensor.DeviceName, sensor.DeviceAddress, movementData);
            });
        }

        public static async void PostAsJsonAsync(String deviceName, String deviceAddress, SensorTag.MovementDataChangedEventArgs movementData)
        {
            await Task.Run(async () =>
            {
                MovementRecord record = new MovementRecord();
                record.AssignedName = deviceName;
                record.DeviceAddress = deviceAddress;
                record.Time = DateTime.Now;
                record.Value = new MovementMeasurement(movementData.X, movementData.Y, movementData.Z);
                var itemAsJson = JsonConvert.SerializeObject(record);
                var content = new StringContent(itemAsJson);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpClient _client = new HttpClient();
                HttpResponseMessage response = await _client.PostAsync("http://localhost:3000/records", content);
                if(response.IsSuccessStatusCode)
                {
                    String json = await response.Content.ReadAsStringAsync();
                    var customer1 = JsonConvert.DeserializeAnonymousType(json, new
                    {
                        type = true,
                        data = new
                        {
                            __v = 0,
                            DeviceAddress = "",
                            AssignedName = "",
                            Time = new DateTime(),
                            _id = "",
                            Value = new
                            {
                                X = 1.0,
                                Y = 1.0,
                                Z = -1.0
                            }
                        }
                    });
                    String id = customer1.data._id;
                    App.Debug("id=" + id);
                } else
                {

                }
             });

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
        public async void Show()
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
                    frame.Navigate(typeof(AccelerometerPage));
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
    }
}
