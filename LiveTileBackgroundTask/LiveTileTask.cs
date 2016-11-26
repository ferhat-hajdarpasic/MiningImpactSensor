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
            deferral.Complete();
        }

        private static void UpdateTile(double maximumImpact)
        {
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear();
            XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150SmallImageAndText03);

            string titleText = "" + maximumImpact + "G";
            tileXml.GetElementsByTagName(textElementName)[0].InnerText = titleText;

            updater.Update(new TileNotification(tileXml));
            Debug.WriteLine("Background task 'LiveTileTask' feed update: " + titleText + ".");
        }

        static string textElementName = "text";
    }
}
