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
        private static ConcurrentQueue<MovementRecord> cq = new ConcurrentQueue<MovementRecord>();
        private static HttpClient httpClient = new HttpClient();

        private static ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer(async (t) =>
        {
            List<MovementRecord> recordsToSend = getRecordsToSend();
            App.Debug("Found " + recordsToSend.Count + " records to send.");
            await sendRecords(recordsToSend);
        }, TimeSpan.FromSeconds(30));

        public static void Enqueue(MovementRecord record)
        {
            cq.Enqueue(record);
        }

        private static List<MovementRecord> getRecordsToSend()
        {
            MovementRecord record = new MovementRecord();
            List<MovementRecord> recordsToSend = new List<MovementRecord>();
            while (cq.TryDequeue(out record))
            {
                recordsToSend.Add(record);
            }
            return recordsToSend;
        }

        private static async Task sendRecords(List<MovementRecord> recordsToSend)
        {
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
                MovementRecord record = maximumImpact(recordItem);
                var itemAsJson = JsonConvert.SerializeObject(record);
                var content = new StringContent(itemAsJson);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                try
                {
                    HttpResponseMessage response = await httpClient.PostAsync("http://localhost:8080/records", content);
                    if (response.IsSuccessStatusCode)
                    {
                        String json = await response.Content.ReadAsStringAsync();
                        var pushServerResponse = JsonConvert.DeserializeAnonymousType(json, new
                        {
                            type = true,
                            data = ""
                        });
                        App.Debug(pushServerResponse.data);
                    }
                    else
                    {
                        App.Debug("HTTP call to local server failed." + response.ReasonPhrase);
                    }
                }
                catch (Exception e)
                {
                    App.Debug("Exception while saving data to the local server." + e.Message);
                }
            }
        }

        private static MovementRecord maximumImpact(MovementRecord record)
        {
            SingleRecord maximumImpact = new SingleRecord();
            foreach (SingleRecord singleRecord in record.Recording)
            {
                double _acceleration = singleRecord.Value.Acceleration;
                if (singleRecord.Value.Acceleration > maximumImpact.Value.Acceleration)
                {
                    maximumImpact = singleRecord;
                }
            }
            MovementRecord result = new MovementRecord();
            result.AssignedName = record.AssignedName;
            result.DeviceAddress = record.DeviceAddress;
            result.Recording.Add(maximumImpact);

            return result;
        }
    }
}
