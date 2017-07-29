using MiningImpactSensor;
using Newtonsoft.Json;
using Shokpod10;
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
        private List<PersistedDevice> devices = null;
        private static string FILE_NAME = "devices.json";

        private static PersistedDevices singleInstance = null;

        public static async Task<PersistedDevices> getPersistedDevices()
        {
            if (singleInstance == null)
            {
                singleInstance = await create();
            }
            return singleInstance;
        }

        private PersistedDevices(List<PersistedDevice> result)
        {
            this.devices = result;
        }

        private static async Task<PersistedDevices> create()
        {
            List<PersistedDevice> devices = new List<PersistedDevice>();
            try
            {
                Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile devicesFile = await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.OpenIfExists);
                string devicesJson = await FileIO.ReadTextAsync(devicesFile);

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
                    devices.Add(device);
                }
            }
            catch (Exception e)
            {
                App.Debug("Error reading " + FILE_NAME + "." + e.Message);
            }
            return new PersistedDevices(devices);
        }

        private async void saveToFile(List<PersistedDevice> _devices)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile devicesFile = await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.ReplaceExisting);

            string json = JsonConvert.SerializeObject(_devices);
            await FileIO.WriteTextAsync(devicesFile, json);
            devices = _devices;
        }

        public string getAssignedToName(string deviceAddress)
        {
            foreach (PersistedDevice device in devices)
            {
                if (device.DeviceAddress == deviceAddress)
                {
                    return device.AssignedToName;
                }
            }
            return deviceAddress;
        }

        public bool getConnected(string deviceAddress)
        {
            foreach (PersistedDevice device in devices)
            {
                if (device.DeviceAddress == deviceAddress)
                {
                    return device.Selected;
                }
            }
            return false;
        }

        public void saveDevice(MiningImpactSensor.SensorTag sensorTag)
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
