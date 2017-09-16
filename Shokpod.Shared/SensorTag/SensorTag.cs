using MiningImpactSensor.Pages;
using SensorTag;
using Shokpod10;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using Windows.UI.Core;
using Windows.UI.Popups;
using static SensorTag.ObservableBluetoothLEDevice;

namespace MiningImpactSensor
{
    public class SensorTag
    {
        public static Guid IRTemperatureServiceUuid = Guid.Parse("f000aa00-0451-4000-b000-000000000000");
        private ObservableBluetoothLEDevice device;
        private ObservableGattCharacteristics MotionCharacteristic;

        public SensorTag(ObservableBluetoothLEDevice device)
        {
            this.Device = device;
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
            this.DeviceAddress = device.BluetoothAddressAsString;
            this.AssignedToName = DeviceAddress;
        }

        public string DeviceAddress {get ; set;}
        public string DeviceName { get; set; }
        public string AssignedToName { get; set; }
        public bool Connected { get; set; }
        public DateTime DateTimeConnected  { get; set; }
        public ObservableBluetoothLEDevice Device { get => device; set => device = value; }

        private async Task StartNotification(ObservableGattCharacteristics c)
        {
            c.Characteristic.ValueChanged += accData_ValueChanged;
            GattCommunicationStatus status = await c.Characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status == GattCommunicationStatus.Success)
            {
                App.Debug("Registered for motion change notifications.");
            }
            else
            {
                MetroEventSource.ToastAsync("Communication status = " + status);
            }
        }

        private async Task StopNotification(ObservableGattCharacteristics c)
        {
            c.Characteristic.ValueChanged -= accData_ValueChanged;
            GattCommunicationStatus status = await c.Characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            if (status == GattCommunicationStatus.Success)
            {
                App.Debug("Un-registered for motion change notifications.");
            }
            else
            {
                MetroEventSource.ToastAsync("Communication status = " + status);
            }
        }

        public async Task<ConnectionResult> Connect()
        {
            Debug.WriteLine("ConnectToSelectedDevice: Entering");
            GattSampleContext.Context.StopEnumeration();

            Debug.WriteLine("ConnectToSelectedDevice: Trying to connect to " + Device.Name);

            ConnectionResult result = await Device.Connect();

            if (result.Result.Status == GattCommunicationStatus.Success)
            {
                foreach (ObservableGattDeviceService s in Device.Services)
                {
                    if (s.UUID == "f000aa80-0451-4000-b000-000000000000")
                    {
                        await s.GetAllCharacteristics();
                        foreach (ObservableGattCharacteristics c in s.Characteristics)
                        {
                            if (c.UUID == "f000aa81-0451-4000-b000-000000000000")
                            {
                                MotionCharacteristic = c;
                                await StartNotification(MotionCharacteristic);
                            }
                            if (c.UUID == "f000aa82-0451-4000-b000-000000000000")
                            {
                                GattCommunicationStatus status = await c.Characteristic.WriteValueAsync((new byte[] { 0x7F, 0x03 }).AsBuffer());
                                if (status == GattCommunicationStatus.Success)
                                {
                                    App.Debug("Configured motion reporting frequency.");
                                }
                                else
                                {
                                    MetroEventSource.ToastAsync("Communication status = " + status);
                                }
                            }
                        }
                    }
                }
                DateTimeConnected = DateTime.Now;
                Connected = true;
            }
            return result;
        }

        private async void accData_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            try
            {
                double SCALE200G = (double)0.049;
                //var data = (await sender.ReadValueAsync()).Value.ToArray();
                byte[] data = args.CharacteristicValue.ToArray();
                short x = (short)((data[7] << 8) | data[6]);
                short y = (short)((data[9] << 8) | data[8]);
                short z = (short)((data[11] << 8) | data[10]);

                MovementDataChangedEventArgs measurement = new MovementDataChangedEventArgs();
                measurement.X = Math.Round((double)(x * SCALE200G) / 8, 2);
                measurement.Y = Math.Round((double)(y * SCALE200G) / 8, 2);
                measurement.Z = Math.Round((double)(z * SCALE200G) / 8, 2);
                //String logMsg = "X=" + x + ", Y=" + y + ", Z=" + z + ", abs = " + measurement.Total;
                //App.Debug("x="+ Convert.ToString(data[7], 2).PadLeft(8,'0') + Convert.ToString(data[6], 2).PadLeft(8, '0') +
                //", y=" + Convert.ToString(data[9], 2).PadLeft(8, '0') + Convert.ToString(data[8], 2).PadLeft(8, '0') +
                //", z=" + Convert.ToString(data[11], 2).PadLeft(8, '0') + Convert.ToString(data[10], 2).PadLeft(8, '0'));

                MovementDataChanged(this, measurement);
            } catch(ObjectDisposedException e)
            {
                App.Debug("Error: received data while object disposed of. " + e.Message);
            }
        }

        public class MovementDataChangedEventArgs : EventArgs
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double Total { get { return Math.Round(Math.Sqrt(X*X + Y*Y + Z*Z), 2);} }
        }

        public class ConnectedEventArgs : EventArgs
        {
            public SensorTag sensorTag { get; set; }
            public Boolean success {get; set;}
        }


        public event EventHandler<MovementDataChangedEventArgs> MovementDataChanged;

        public async void Disconnect()
        {
            if (MotionCharacteristic != null)
            {
                await this.StopNotification(MotionCharacteristic);
            }
            if (Device != null)
            {
                Device.BluetoothLEDevice.Dispose();
            }
        }
    }
}
