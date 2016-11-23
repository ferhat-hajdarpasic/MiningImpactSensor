using MiningImpactSensor;
using Newtonsoft.Json;
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
        public static List<PersistedDevice> devices;
        private static string FILE_NAME = "devices.json";

        public static async Task<List<PersistedDevice>> readFromFile()
        {
            List<PersistedDevice> result = new List<PersistedDevice>();
            try
            {
                Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile devicesFile = await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.OpenIfExists);
                String devicesJson = await FileIO.ReadTextAsync(devicesFile);

                App.Debug(devicesJson);

                JsonArray devicesArray;
                if (String.IsNullOrEmpty(devicesJson))
                {
                    devicesArray = new JsonArray();
                }
                else
                {
                    devicesArray = JsonArray.Parse(devicesJson);
                }
                for (uint i = 0; i < devicesArray.Count; i++)
                {
                    PersistedDevice device = new PersistedDevice();
                    JsonObject obj = devicesArray.GetObjectAt(i);
                    device.DeviceName = obj.GetNamedString("DeviceName");
                    device.AssignedToName = obj.GetNamedString("AssignedToName");
                    device.Selected = obj.GetNamedBoolean("Selected");
                    device.DeviceAddress = obj.GetNamedString("DeviceAddress");
                    result.Add(device);
                }
            } catch(Exception e)
            {
                App.Debug("Error reading " + FILE_NAME + "." + e.Message);
            }
            devices = result;
            return result;
        }
        public static async void saveToFile(List<PersistedDevice> _devices)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile devicesFile = await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.ReplaceExisting);

            string json = JsonConvert.SerializeObject(_devices);
            await FileIO.WriteTextAsync(devicesFile, json);
            devices = _devices;
        }

        internal static async Task<string> getAssignedToName(string deviceAddress)
        {
            if (devices == null)
            {
                devices = await readFromFile();
            }
            foreach (PersistedDevice device in devices)
            {
                if (device.DeviceAddress == deviceAddress)
                {
                    return device.AssignedToName;
                }
            }
            return deviceAddress;
        }

        internal static async Task<bool> getConnected(string deviceAddress)
        {
            if (devices == null)
            {
                devices = await readFromFile();
            }
            foreach (PersistedDevice device in devices)
            {
                if (device.DeviceAddress == deviceAddress)
                {
                    return device.Selected;
                }
            }
            return false;
        }

        public static void saveDevice(MiningImpactSensor.SensorTag sensorTag)
        {
            PersistedDevice targetDevice = null;
            foreach (PersistedDevice device in devices)
            {
                if (device.DeviceAddress == sensorTag.DeviceAddress)
                {
                    targetDevice = device;
                    break;
                }
            }
            if (targetDevice == null)
            {
                targetDevice = new PersistedDevice();
                devices.Add(targetDevice);
            }
            targetDevice.AssignedToName = sensorTag.AssignedToName;
            targetDevice.Selected = sensorTag.Connected;
            targetDevice.DeviceAddress = sensorTag.DeviceAddress;
            targetDevice.DeviceName = sensorTag.DeviceName;


            saveToFile(devices);
        }
    }
}
