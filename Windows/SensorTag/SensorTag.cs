using MiningImpactSensor.Pages;
using SensorTag;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using Windows.UI.Core;

namespace MiningImpactSensor
{
    public class SensorTag
    {
        public static Guid IRTemperatureServiceUuid = Guid.Parse("f000aa00-0451-4000-b000-000000000000");
        private PnpObjectWatcher watcher;
        private GattCharacteristic characteristic;
        GattDeviceService accService;
        private const GattClientCharacteristicConfigurationDescriptorValue CHARACTERISTIC_NOTIFICATION_TYPE =
            GattClientCharacteristicConfigurationDescriptorValue.Notify;

        public SensorTag(DeviceInformation device)
        {
            string name = device.Name;
            App.Debug("Found sensor tag: [{0}]", name);

            DeviceName = device.Name;
            //if (name == "CC2650 SensorTag" || name == "SensorTag 2.0" || name == "SensorTag")
            //{
            //    DeviceName = "CC2650";
            //}
            //else
            //{
            //    DeviceName = "CC2541";
            //}
            this.DeviceId = device.Id;
            this.DeviceAddress = SensorTagDeviceIdParser.parse(device);
            this.AssignedToName = DeviceAddress;
        }

        public string DeviceId { get; set; }
        public string DeviceAddress {get ; set;}
        public string DeviceName { get; set; }
        public string AssignedToName { get; set; }
        public bool Connected { get; set; }
        public DateTime DateTimeConnected  { get; set; }
    //public String deviceContainerId { get; set; }

    public static async Task<List<SensorTag>> FindAllMotionSensors()
        {
            List<PersistedDevice> persistedDevices = await PersistedDevices.readFromFile();

            List<SensorTag> result = new List<SensorTag>();
            foreach (DeviceInformation device in await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(new Guid("f000aa80-0451-4000-b000-000000000000"))))
            {
                string name = device.Name;
                if (name.Contains("SensorTag") || name.Contains("Sensor Tag") || name.Contains("ShokPod"))
                {
                    SensorTag sensor = new SensorTag(device);
                    sensor.AssignedToName = await PersistedDevices.getAssignedToName(sensor.DeviceAddress);
                    sensor.Connected = await PersistedDevices.getConnected(sensor.DeviceAddress);
                    result.Add(sensor);
                }
                App.Debug("Name=" + device.Name + ", Id=" + device.Id);
            }

            return result;
        }

        private void StartDeviceConnectionWatcher()
        {
            watcher = PnpObject.CreateWatcher(PnpObjectType.DeviceContainer,
                new string[] { "System.Devices.Connected" }, String.Empty);

            watcher.Updated += DeviceConnection_Updated;
            watcher.Start();
        }

        private async void DeviceConnection_Updated(PnpObjectWatcher watcher, PnpObjectUpdate args)
        {
            bool isConnected = (bool)args.Properties["System.Devices.Connected"];

            DateTimeConnected = DateTime.Now;
            this.Connected = true;

            if (isConnected)
            {
                watcher.Stop();
                watcher = null;
            }
        }

        public async Task<bool> ConnectMotionService()
        {
            bool result = false;
            accService = await GattDeviceService.FromIdAsync(this.DeviceId);
            if (accService != null)
            {
                App.Debug("Found movement service!" + DeviceId);
                var list = accService.GetCharacteristics(new Guid("f000aa81-0451-4000-b000-000000000000"));
                characteristic = list.FirstOrDefault();
                characteristic.ValueChanged += accData_ValueChanged;
                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                var accConfig = accService.GetCharacteristics(new Guid("f000aa82-0451-4000-b000-000000000000"))[0];
                await accConfig.WriteValueAsync((new byte[] { 0x7F, 0x03 }).AsBuffer());

                //var periodConfig = accService.GetCharacteristics(new Guid("f000aa83-0451-4000-b000-000000000000"))[0];
                //await periodConfig.WriteValueAsync((new byte[] { 100 }).AsBuffer());

                App.Debug("Connection all good." + DeviceId);
                StartDeviceConnectionWatcher();
                result = true;
            }
            else
            {
                App.Debug("Could not connect to device." + DeviceId);
                result = false;
            }
            this.Connected = result;
            this.DateTimeConnected = DateTime.Now;
            PersistedDevices.saveDevice(this);
            return result;
        }

        private async void accData_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            double SCALE200G = (double)0.049;
            var data = (await sender.ReadValueAsync()).Value.ToArray();
            short x = (short)((data[7] << 8) | data[6]);
            short y = (short)((data[9] << 8) | data[8]);
            short z = (short)((data[11] << 8) | data[10]);

            MovementDataChangedEventArgs measurement = new MovementDataChangedEventArgs();
            measurement.X = (double)(x * SCALE200G)/8;
            measurement.Y = (double)(y * SCALE200G)/8;
            measurement.Z = (double)(z * SCALE200G)/8;
            //String logMsg = "X=" + x + ", Y=" + y + ", Z=" + z + ", abs = " + measurement.Total;
            //App.Debug("x="+ Convert.ToString(data[7], 2).PadLeft(8,'0') + Convert.ToString(data[6], 2).PadLeft(8, '0') +
            //", y=" + Convert.ToString(data[9], 2).PadLeft(8, '0') + Convert.ToString(data[8], 2).PadLeft(8, '0') +
            //", z=" + Convert.ToString(data[11], 2).PadLeft(8, '0') + Convert.ToString(data[10], 2).PadLeft(8, '0'));

            MovementDataChanged(this, measurement);
        }

        internal async void Disconnect()
        {
            if (characteristic != null)
            {
                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            }
            accService.Dispose();
            accService = null;
            characteristic = null;
            GC.Collect();
        }

        public class MovementDataChangedEventArgs : EventArgs
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double Total { get { return Math.Sqrt(X*X + Y*Y + Z*Z);} }
        }

        public event EventHandler<MovementDataChangedEventArgs> MovementDataChanged;
    }
}
