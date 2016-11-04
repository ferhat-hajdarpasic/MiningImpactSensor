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
using Windows.UI.Core;

namespace MiningImpactSensor
{
    public class SensorTag
    {
        public static Guid IRTemperatureServiceUuid = Guid.Parse("f000aa00-0451-4000-b000-000000000000");

        public SensorTag(DeviceInformation device)
        {
            string name = device.Name;
            App.Debug("Found sensor tag: [{0}]", name);
            if (name == "CC2650 SensorTag" || name == "SensorTag 2.0" || name == "SensorTag")
            {
                DeviceName = "CC2650";
            }
            else
            {
                DeviceName = "CC2541";
            }
            this.DeviceId = device.Id;
            this.DeviceAddress = SensorTagDeviceIdParser.parse(device);
        }

        public string DeviceId { get; set; }
        public string DeviceAddress {get ; set;}
        public string DeviceName { get; set; }
        public bool Connected { get; set; }
        
        public static async Task<IEnumerable<SensorTag>> FindAllMotionSensors()
        {
            List<SensorTag> result = new List<SensorTag>();
            foreach (DeviceInformation device in await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(new Guid("f000aa80-0451-4000-b000-000000000000"))))
            {
                string name = device.Name;
                if (name.Contains("SensorTag") || name.Contains("Sensor Tag"))
                {
                    result.Add(new SensorTag(device));
                }
                App.Debug("Name=" + device.Name + ", Id=" + device.Id);
            }
            return result;
        }

        public async Task<bool> ConnectMotionService()
        {
            GattDeviceService accService = await GattDeviceService.FromIdAsync(this.DeviceId);
            if (accService != null)
            {
                App.Debug("Found movement service!" + DeviceId);
                var list = accService.GetCharacteristics(new Guid("f000aa81-0451-4000-b000-000000000000"));
                var accData = list.FirstOrDefault();
                accData.ValueChanged += accData_ValueChanged;
                await accData.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                var accConfig = accService.GetCharacteristics(new Guid("f000aa82-0451-4000-b000-000000000000"))[0];
                await accConfig.WriteValueAsync((new byte[] { 0x7F, 0x03 }).AsBuffer());

                var periodConfig = accService.GetCharacteristics(new Guid("f000aa83-0451-4000-b000-000000000000"))[0];
                await periodConfig.WriteValueAsync((new byte[] { 100 }).AsBuffer());

                App.Debug("Connection all good." + DeviceId);
                return true;
            }
            else
            {
                App.Debug("Could not connect to device." + DeviceId);
                return false;
            }
        }

        private async void accData_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            double SCALE200G = (double)0.049;
            var data = (await sender.ReadValueAsync()).Value.ToArray();
            short x = (short)((data[7] << 8) | data[6]);
            short y = (short)((data[9] << 8) | data[8]);
            short z = (short)((data[11] << 8) | data[10]);

            MovementDataChangedEventArgs measurement = new MovementDataChangedEventArgs();
            measurement.X = (double)x * SCALE200G;
            measurement.Y = (double)y * SCALE200G;
            measurement.Z = (double)z * SCALE200G;
            App.Debug("X=" + x + ", Y=" + y + ", Z=" + z + ", abs = " + measurement.Total);

            MovementDataChanged(this, measurement);
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
