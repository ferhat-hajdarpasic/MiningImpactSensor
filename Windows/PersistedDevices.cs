using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace SensorTag
{
    class PersistedDevices
    {
        public static async Task<List<PersistedDevice>> readFromFile()
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile devicesFile = await localFolder.CreateFileAsync("devices.json", CreationCollisionOption.OpenIfExists);
            String devicesJson = await FileIO.ReadTextAsync(devicesFile);
            JsonObject devicesObject;
            if (String.IsNullOrEmpty(devicesJson))
            {
                devicesObject = new JsonObject();
                devicesObject.SetNamedValue("devices", new JsonArray());
            }
            else
            {
                devicesObject = JsonObject.Parse(devicesJson);
            }
            JsonArray devicesArray = devicesObject.GetNamedArray("devices");
            List<PersistedDevice> result = new List<PersistedDevice>();
            for (uint i = 0; i < devicesArray.Count; i++)
            {
                PersistedDevice device = new PersistedDevice();
                JsonObject obj = devicesArray.GetObjectAt(i);
                device.DeviceId = obj.GetNamedString("deviceId");
                device.AssignedToName = obj.GetNamedString("assignedToName");
                device.Selected = obj.GetNamedBoolean("selected");
                result.Add(device);
            }
            return result;
        }
    }
}
