using MiningImpactSensor;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace SensorTag
{
    public class RecordingQueue
    {
        private static ConcurrentQueue<MovementRecord> deviceQueue = new ConcurrentQueue<MovementRecord>();
        private static ConcurrentQueue<MovementRecord> remoteServerQueue = new ConcurrentQueue<MovementRecord>();

        private static ThreadPoolTimer deviceTimer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
        {
            List<MovementRecord> recordsToSend = getDeviceRecords();
            sendDeviceRecords(recordsToSend);
        }, TimeSpan.FromSeconds(5));


        private static Boolean timerActive = false;
        private static ThreadPoolTimer bufferedRecordsTimer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
        {
            if (!timerActive)
            {
                try
                {
                    timerActive = true;
                    sendBufferedServerRecords();
                }
                finally
                {
                    timerActive = false;
                }
            }
        }, TimeSpan.FromSeconds(5));

        public static double MaximumImpact { get; internal set; }

        public static void AddToDeviceQueue(MovementRecord record)
        {
            deviceQueue.Enqueue(record);
        }

        public static void AddToRemoteServerQueue(MovementRecord record)
        {
            if (deviceQueue.Count < 60 * 60 * 10) //10 hours
            {
                remoteServerQueue.Enqueue(record);
            }
            else
            {
                App.Debug("Found too many records waiting to go to server: " + remoteServerQueue.Count + ". ");
                MovementRecord recordToDrop;
                if (remoteServerQueue.TryDequeue(out recordToDrop))
                {
                    App.Debug("Dropped the earliest record to make room for the latest.");
                    remoteServerQueue.Enqueue(record);
                }
            }
        }

        private static List<MovementRecord> getDeviceRecords()
        {
            MovementRecord record = new MovementRecord();
            List<MovementRecord> recordsToSend = new List<MovementRecord>();
            while (deviceQueue.TryDequeue(out record))
            {
                recordsToSend.Add(record);
            }
            App.Debug("Found " + recordsToSend.Count + " records queued from device.");
            return recordsToSend;
        }

        private static void sendDeviceRecords(List<MovementRecord> recordsToSend)
        {
            ShokpodSettings settings = ShokpodSettings.getSettings().Result;
            Dictionary<string, MovementRecord> groupedByDeviceAddress = new Dictionary<string, MovementRecord>();
            foreach (MovementRecord record in recordsToSend)
            {
                MovementRecord currentCollatingRecord;
                if (!groupedByDeviceAddress.TryGetValue(record.DeviceAddress, out currentCollatingRecord))
                {
                    currentCollatingRecord = new MovementRecord();
                    currentCollatingRecord.AssignedName = record.AssignedName;
                    currentCollatingRecord.DeviceAddress = record.DeviceAddress;
                    groupedByDeviceAddress[record.DeviceAddress] = currentCollatingRecord;
                }
                currentCollatingRecord.Recording.AddRange(record.Recording);
            }

            foreach (MovementRecord recordItem in groupedByDeviceAddress.Values)
            {
                MovementRecord record = maximumImpact(recordItem).Result;
                if (record != null)
                {
                    var itemAsJson = JsonConvert.SerializeObject(record);
                    var content = new StringContent(itemAsJson);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            httpClient.Timeout.Add(TimeSpan.FromSeconds(5));
                            HttpResponseMessage response = httpClient.PostAsync(settings.ShokpodApiLocation + "/records", content).Result;
                            if (response.IsSuccessStatusCode)
                            {
                                String json = response.Content.ReadAsStringAsync().Result;
                                var pushServerResponse = JsonConvert.DeserializeAnonymousType(json, new
                                {
                                    type = true,
                                    data = ""
                                });
                                App.Debug(pushServerResponse.data);
                                MiningImpactSensor.Pages.DevicePage.Debug("Persisted acceleration of " + record.Recording[0].Value.Acceleration + "G.");
                            }
                            else
                            {
                                App.Debug("HTTP call to remote server failed with error: '" + response.ReasonPhrase + ".'");
                                RecordingQueue.AddToRemoteServerQueue(record);
                                App.Debug("Added failed record to the remote server queue: '" + record.Recording[0].Value.Acceleration + "G.'");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MiningImpactSensor.Pages.DevicePage.Debug("Exception while saving data to the remote server." + e.Message);
                    }
                }
            }
        }

        private static void sendBufferedServerRecords()
        {
            ShokpodSettings settings = ShokpodSettings.getSettings().Result;
            MovementRecord recordItem;
            App.Debug("Found " + remoteServerQueue.Count + " records queued for remote server.");
            while (!remoteServerQueue.IsEmpty && remoteServerQueue.TryPeek(out recordItem))
            {
                var itemAsJson = JsonConvert.SerializeObject(recordItem);
                var content = new StringContent(itemAsJson);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout.Add(TimeSpan.FromSeconds(5));
                    HttpResponseMessage response = httpClient.PostAsync(settings.ShokpodApiLocation + "/records", content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        String json = response.Content.ReadAsStringAsync().Result;
                        var pushServerResponse = JsonConvert.DeserializeAnonymousType(json, new
                        {
                            type = true,
                            data = ""
                        });
                        App.Debug(pushServerResponse.data);
                        MiningImpactSensor.Pages.DevicePage.Debug("Sent to server acceleration of " + recordItem.Recording[0].Value.Acceleration + "G.");
                        remoteServerQueue.TryDequeue(out recordItem);
                    }
                    else
                    {
                        App.Debug("While sending buffered records, HTTP call to remote server failed with error: '" + response.ReasonPhrase + "'. Breaking out.");
                        break;
                    }
                }
            }
        }

        private static async Task<MovementRecord> maximumImpact(MovementRecord record)
        {
            MovementRecord result = null;
            SingleRecord maximumImpact = new SingleRecord();
            foreach (SingleRecord singleRecord in record.Recording)
            {
                double _acceleration = singleRecord.Value.Acceleration;
                if (singleRecord.Value.Acceleration > maximumImpact.Value.Acceleration)
                {
                    maximumImpact = singleRecord;
                }
            }
            ShokpodSettings settings = await ShokpodSettings.getSettings();
            double serverReportingThreshold = settings.ServerImpactThreshhold;

            if (maximumImpact.Value.Acceleration > serverReportingThreshold)
            {
                result = new MovementRecord();
                result.AssignedName = record.AssignedName;
                result.DeviceAddress = record.DeviceAddress;
                result.Recording.Add(maximumImpact);
                MiningImpactSensor.Pages.DevicePage.Debug("Recording acceleration of " + maximumImpact.Value.Acceleration + "G.");
            } else
            {
                /*MiningImpactSensor.Pages.DevicePage.Debug(maximumImpact.Value.Acceleration + "G is below threshold of " + serverReportingThreshold + "G, not recording it.");*/
            }
            if(result != null)
            {
                RecordingQueue.MaximumImpact = maximumImpact.Value.Acceleration;
            } else
            {
                RecordingQueue.MaximumImpact = 0;
            }
            return result;
        }
    }
}
