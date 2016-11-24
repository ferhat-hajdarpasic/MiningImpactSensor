using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.Web.Syndication;

namespace LiveTileBackgroundTask
{
    public sealed class LiveTileTask : IBackgroundTask
    {
        private static HttpClient httpClient = new HttpClient();
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background task 'LiveTileTask' invoked.");
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync("http://localhost:8080/records/20/seconds");
                int y = 99;
                if (response.IsSuccessStatusCode)
                {
                    String json = await response.Content.ReadAsStringAsync();
                    List<Dictionary<string, object>> list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                    double maximumImpact = 0;
                    foreach(Dictionary<string, object> o in list)
                    {
                        String deviceAddress = (string)o["DeviceAddress"];
                        JArray records = (JArray)o["h"];
                        foreach(JObject jObject in records)
                        {
                            JObject value = (JObject)jObject["Value"];
                            float X = (float)value["X"];
                            float Y = (float)value["Y"];
                            float Z = (float)value["Z"];

                            double amplitude = Math.Sqrt(X * X + Y * Y + Z * Z);
                            if(amplitude > maximumImpact)
                            {
                                maximumImpact = amplitude;
                            }
                        }
                    }
                    UpdateTile(maximumImpact);
                }
                else
                {
                    Debug.WriteLine("HTTP call to local server failed." + response.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            // Inform the system that the task is finished.
            deferral.Complete();
        }

        private static async void UpdateTile(double maximumImpact)
        {
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear();
            XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150SmallImageAndText03);

            string titleText = "" + maximumImpact + "G";
            tileXml.GetElementsByTagName(textElementName)[0].InnerText = titleText;
           // ((XmlElement)tileXml.GetElementsByTagName("image")[0]).SetAttribute("src", "ms-appx://Assets/shokpod-icon.png");

            // Create a new tile notification.
            updater.Update(new TileNotification(tileXml));
            Debug.WriteLine("Background task 'LiveTileTask' feed update: " + titleText + ".");
        }

        // Although most HTTP servers do not require User-Agent header, others will reject the request or return
        // a different response if this header is missing. Use SetRequestHeader() to add custom headers.
        static string customHeaderName = "User-Agent";
        static string customHeaderValue = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";

        static string textElementName = "text";
    }
}
