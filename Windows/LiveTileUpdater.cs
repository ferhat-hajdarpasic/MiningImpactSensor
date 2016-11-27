using MiningImpactSensor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml;

namespace SensorTag
{
    class LiveTileUpdater
    {
        DispatcherTimer dispatcherTimer;
        private const string shokpod_icon = "ms-appx:///Assets/Shockpod icon 27112016 001.png";
        private static HttpClient httpClient = new HttpClient();

        void dispatcherTimer_Tick(object sender, object e)
        {
            App.Debug("Live tile timer tick.");
            sendTileTextNotification();
            App.Debug("Live tile timer tick finished.");
        }

        async void sendTileTextNotification()
        {
            string g = await computeG();
            UpdateTile(g);
        }

        private static async Task<string> computeG()
        {
            try
            {
                ShokpodSettings settings = await ShokpodSettings.getSettings();
                int secondsPeriod = settings.LiveTileUpdatePeriod;

                HttpResponseMessage response = await httpClient.GetAsync(settings.ShokpodApiLocation + "/records/" + secondsPeriod + "/seconds");
                int y = 99;
                if (response.IsSuccessStatusCode)
                {
                    String json = await response.Content.ReadAsStringAsync();
                    List<Dictionary<string, object>> list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                    double maximumImpact = 0;
                    foreach (Dictionary<string, object> o in list)
                    {
                        String deviceAddress = (string)o["DeviceAddress"];
                        JArray records = (JArray)o["h"];
                        foreach (JObject jObject in records)
                        {
                            JObject value = (JObject)jObject["Value"];
                            float X = (float)value["X"];
                            float Y = (float)value["Y"];
                            float Z = (float)value["Z"];

                            double amplitude = Math.Round(Math.Sqrt(X * X + Y * Y + Z * Z),2);
                            if (amplitude > maximumImpact)
                            {
                                maximumImpact = amplitude;
                            }
                        }
                    }
                    return "" + maximumImpact + "G";
                }
                else
                {
                    Debug.WriteLine("HTTP call to local server failed." + response.ReasonPhrase);
                    return response.ReasonPhrase;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return e.Message;
            }
        }

        private static void UpdateTile(string titleText)
        {
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear();
            XmlDocument tileXml = createXmlDocument1(titleText);
            updater.Update(new TileNotification(tileXml));
            Debug.WriteLine("Background task 'LiveTileTask' feed update: " + titleText + ".");
        }

        private static XmlDocument createXmlDocument1(string titleText)
        {
            XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150SmallImageAndText03);

            tileXml.GetElementsByTagName("text")[0].InnerText = titleText;
            ((XmlElement)tileXml.GetElementsByTagName("image")[0]).SetAttribute("src", shokpod_icon);

            // Create a new tile notification.
            return tileXml;
        }

        internal async void Start()
        {
            ShokpodSettings settings = await ShokpodSettings.getSettings();
            int secondsPeriod = settings.LiveTileUpdatePeriod;
            dispatcherTimer = new Windows.UI.Xaml.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, secondsPeriod);
            dispatcherTimer.Start();
        }

        internal void Stop()
        {
            dispatcherTimer.Stop();
            dispatcherTimer = null;
        }
    }
}
