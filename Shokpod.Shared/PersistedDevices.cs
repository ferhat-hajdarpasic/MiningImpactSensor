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
        private static string FILE_NAME = "devices.json";

        public static PersistedDevices singleInstance = new PersistedDevices();

        public List<PersistedDevice> Devices { get ; private set; }

        private PersistedDevices()
        {
        }

        public async Task<bool> populate()
        {
            Devices = new List<PersistedDevice>();
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
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
                    Devices.Add(device);
                }
            }
            catch (Exception e)
            {
                App.Debug("Error reading " + FILE_NAME + "." + e.Message);
            }
            return true;
        }

        private async void saveToFile()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile devicesFile = await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.ReplaceExisting);

                string json = JsonConvert.SerializeObject(Devices);
                await FileIO.WriteTextAsync(devicesFile, json);
            } catch (System.IO.IOException e)
            {
                MetroEventSource.ToastAsync("Cannot save configuration! " + e.Message);
            }
        }

        public string getAssignedToName(string deviceAddress)
        {
            foreach (PersistedDevice device in Devices)
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
            foreach (PersistedDevice device in Devices)
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
            foreach (PersistedDevice device in Devices)
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
                Devices.Add(targetDevice);
            }
            targetDevice.AssignedToName = sensorTag.AssignedToName;
            targetDevice.Selected = sensorTag.Connected;
            targetDevice.DeviceAddress = sensorTag.DeviceAddress;
            targetDevice.DeviceName = sensorTag.DeviceName;
            saveToFile();
        }
    }
}
