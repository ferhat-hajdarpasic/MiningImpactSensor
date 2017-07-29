using MiningImpactSensor;
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
            sendTileTextNotification();
        }

        void sendTileTextNotification()
        {
            string g = RecordingQueue.MaximumImpact + "G";
            UpdateTile(g);
        }

        private static void UpdateTile(string titleText)
        {
            Debug.WriteLine("Background task 'LiveTileTask' feed update: " + titleText + ".");
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear();
            XmlDocument tileXml = createXmlDocument1(titleText);
            updater.Update(new TileNotification(tileXml));
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
