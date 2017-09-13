using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.UI.Popups;

namespace SensorTag
{
    class GattSampleContext
    {
        private bool isPeripheralRoleSupported = true;
        public event PropertyChangedEventHandler PropertyChanged;
        private DeviceWatcher deviceWatcher;
        private SemaphoreSlim DevNodeLock = new SemaphoreSlim(1, 1);
        private SemaphoreSlim BluetoothLEDevicesLock = new SemaphoreSlim(1, 1);
        private List<DeviceInformation> unusedDevices = new List<DeviceInformation>();
        private List<DeviceInformation> devNodes = new List<DeviceInformation>();
        public ObservableBluetoothLEDevice SelectedBluetoothLEDevice { get; set; } = null;
        public ObservableGattCharacteristics SelectedCharacteristic { get; set; } = null;
        public ObservableCollection<ObservableBluetoothLEDevice> BluetoothLEDevices { get; set; } = new ObservableCollection<ObservableBluetoothLEDevice>();

        private const string BluetoothDeviceAddress = "System.DeviceInterface.Bluetooth.DeviceAddress";
        private const string BTLEDeviceWatcherAQSString = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
        private const string BatteryLevelGUID = "{995EF0B0-7EB3-4A8B-B9CE-068BB3F4AF69} 10";
        private BluetoothLEAdvertisementWatcher advertisementWatcher;

        public static GattSampleContext Context { get; private set; } = new GattSampleContext();
        /// <summary>
        /// Gets a value indicating whether peripheral mode is supported by this device
        /// </summary>
        public bool IsPeripheralRoleSupported
        {
            get
            {
                return isPeripheralRoleSupported;
            }

            private set
            {
                if (isPeripheralRoleSupported != value)
                {
                    isPeripheralRoleSupported = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("IsPeripheralRoleSupported"));
                }
            }
        }

        private bool isCentralRoleSupported = true;

        /// <summary>
        /// Gets a value indicating whether central role is supported by this device
        /// </summary>
        public bool IsCentralRoleSupported
        {
            get
            {
                return isCentralRoleSupported;
            }

            private set
            {
                if (isCentralRoleSupported != value)
                {
                    isCentralRoleSupported = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("IsCentralRoleSupported"));
                }
            }
        }

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        private async void Init()
        {
            Windows.Devices.Bluetooth.BluetoothAdapter adapter = await Windows.Devices.Bluetooth.BluetoothAdapter.GetDefaultAsync();

            if (adapter == null)
            {
                MessageDialog msg = new MessageDialog("Error getting access to Bluetooth adaptor. Do you have a have bluetooth enabled?", "Error");
                await msg.ShowAsync();

                IsPeripheralRoleSupported = false;
                IsCentralRoleSupported = false;
            }
            else
            {
                IsPeripheralRoleSupported = adapter.IsPeripheralRoleSupported;
                IsCentralRoleSupported = adapter.IsCentralRoleSupported;
            }

            // Start the dev node watcher
            string[] requestedProperties =
                {
                    BluetoothDeviceAddress
                };

            return;
        }

        private async Task UpdateBatteryLevel(DeviceInformation dev)
        {
            if (dev.Properties.Keys.Contains(BatteryLevelGUID) &&
                dev.Properties[BatteryLevelGUID] != null &&
                dev.Properties.Keys.Contains(BluetoothDeviceAddress) &&
                dev.Properties[BluetoothDeviceAddress] != null)
            {
                try
                {
                    await BluetoothLEDevicesLock.WaitAsync();

                    foreach (ObservableBluetoothLEDevice device in BluetoothLEDevices)
                    {
                        string addr = GetDelimitedAddr((string)dev.Properties[BluetoothDeviceAddress]);
                        if (device.BluetoothAddressAsString == addr)
                        {
                            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                                Windows.UI.Core.CoreDispatcherPriority.Normal,
                                () =>
                                {
                                    device.BatteryLevel = Convert.ToInt32((byte)dev.Properties[BatteryLevelGUID]);
                                });
                            break;
                        }
                    }
                }
                finally
                {
                    BluetoothLEDevicesLock.Release();
                }
            }
        }

        private async Task UpdateBatteryLevel(ObservableBluetoothLEDevice dev)
        {
            foreach (DeviceInformation devNode in devNodes)
            {
                string addr = dev.BluetoothAddressAsString.Replace(":", String.Empty);

                if (devNode.Properties.Keys.Contains(BatteryLevelGUID) &&
                    devNode.Properties[BatteryLevelGUID] != null &&
                    devNode.Properties.Keys.Contains(BluetoothDeviceAddress) &&
                    devNode.Properties[BluetoothDeviceAddress] != null)
                {
                    if ((string)devNode.Properties[BluetoothDeviceAddress] == addr)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            Windows.UI.Core.CoreDispatcherPriority.Normal,
                            () =>
                            {
                                dev.BatteryLevel = Convert.ToInt32((byte)devNode.Properties[BatteryLevelGUID]);
                            });
                        break;
                    }
                }
            }
        }


        // Need to add this device. Can't do that here as we have the lock
        private string GetDelimitedAddr(string addr)
        {
            // Add : delimiters to raw address
            var list = Enumerable.Range(0, addr.Length / 2).Select(i => addr.Substring(i * 2, 2)).ToList();
            return string.Join(":", list);
        }

        public void StartEnumeration()
        {
            // Additional properties we would like about the device.
            string[] requestedProperties =
                {
                    "System.Devices.Aep.Category",
                    "System.Devices.Aep.ContainerId",
                    "System.Devices.Aep.DeviceAddress",
                    "System.Devices.Aep.IsConnected",
                    "System.Devices.Aep.IsPaired",
                    "System.Devices.Aep.IsPresent",
                    "System.Devices.Aep.ProtocolId",
                    "System.Devices.Aep.Bluetooth.Le.IsConnectable",
                    "System.Devices.Aep.SignalStrength"
                };

            // BT_Code: Currently Bluetooth APIs don't provide a selector to get ALL devices that are both paired and non-paired.
            deviceWatcher =
                    DeviceInformation.CreateWatcher(
                        BTLEDeviceWatcherAQSString,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);

            // Register event handlers before starting the watcher.
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            advertisementWatcher = new BluetoothLEAdvertisementWatcher();
            advertisementWatcher.Received += AdvertisementWatcher_Received;

            BluetoothLEDevices.Clear();

            deviceWatcher.Start();
            advertisementWatcher.Start();
            IsEnumerating = true;
            EnumerationFinished = false;
        }
        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            try
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    await AddDeviceToList(deviceInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeviceWatcher_Added: " + ex.Message);
            }
        }

        /// <summary>
        /// Executes when a device is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInfoUpdate"></param>
        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation di = null;
            bool addNewDI = false;

            try
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    ObservableBluetoothLEDevice dev;

                    // Need to lock as another DeviceWatcher might be modifying BluetoothLEDevices 
                    try
                    {
                        await BluetoothLEDevicesLock.WaitAsync();
                        dev = BluetoothLEDevices.FirstOrDefault(device => device.DeviceInfo.Id == deviceInfoUpdate.Id);
                        if (dev != null)
                        {   // Found a device in the list, updating it
                            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                                Windows.UI.Core.CoreDispatcherPriority.Normal,
                                async () =>
                                {
                                    dev.Update(deviceInfoUpdate);
                                    await UpdateBatteryLevel(dev);
                                });
                        }
                        else
                        {
                            // Need to add this device. Can't do that here as we have the lock
                            addNewDI = true;
                        }
                    }
                    finally
                    {
                        BluetoothLEDevicesLock.Release();
                    }

                    if (addNewDI == true)
                    {
                        try
                        {
                            await BluetoothLEDevicesLock.WaitAsync();
                            di = unusedDevices.FirstOrDefault(device => device.Id == deviceInfoUpdate.Id);
                            if (di != null)
                            {   // We found this device before.
                                unusedDevices.Remove(di);
                                di.Update(deviceInfoUpdate);
                            }
                        }
                        finally
                        {
                            BluetoothLEDevicesLock.Release();
                        }

                        if (di != null)
                        {
                            await AddDeviceToList(di);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeviceWatcher_Updated: " + ex.Message);
            }
        }

        /// <summary>
        /// Executes when a device is removed from enumeration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInfoUpdate"></param>
        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            try
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    ObservableBluetoothLEDevice dev;

                    try
                    {
                        // Need to lock as another DeviceWatcher might be modifying BluetoothLEDevices 
                        await BluetoothLEDevicesLock.WaitAsync();

                        // Find the corresponding DeviceInformation in the collection and remove it.
                        dev = BluetoothLEDevices.FirstOrDefault(device => device.DeviceInfo.Id == deviceInfoUpdate.Id);
                        if (dev != null)
                        {   // Found it in our displayed devices
                            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                                Windows.UI.Core.CoreDispatcherPriority.Normal,
                                () =>
                                {
                                    Debug.Assert(BluetoothLEDevices.Remove(dev), "DeviceWatcher_Removed: Failed to remove device from list");
                                });
                        }
                        else
                        {   // Did not find in diplayed list, let's check the unused list
                            DeviceInformation di = unusedDevices.FirstOrDefault(device => device.Id == deviceInfoUpdate.Id);

                            if (di != null)
                            {   // Found in unused devices, remove it
                                Debug.Assert(unusedDevices.Remove(di), "DeviceWatcher_Removed: Failed to remove device from unused");
                            }
                        }
                    }
                    finally
                    {
                        BluetoothLEDevicesLock.Release();
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeviceWatcher_Removed: " + ex.Message);
            }
        }

        /// <summary>
        /// Executes when Enumeration has finished
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object e)
        {
            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == deviceWatcher)
            {
                StopEnumeration();
                EnumerationFinished = true;
                Debug.WriteLine("Enumeration finished!");
            }
        }
        private void DeviceWatcher_Stopped(DeviceWatcher sender, object e)
        {
            // Implimented for completeness
        }
        private async void AdvertisementWatcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {

                await BluetoothLEDevicesLock.WaitAsync();

                foreach (ObservableBluetoothLEDevice d in BluetoothLEDevices)
                {
                    if (d.BluetoothAddressAsUlong == args.BluetoothAddress)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            Windows.UI.Core.CoreDispatcherPriority.Normal,
                            () =>
                            {
                                d.ServiceCount = args.Advertisement.ServiceUuids.Count();
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AdvertisementWatcher_Received: ", ex.Message);
            }
            finally
            {
                BluetoothLEDevicesLock.Release();
            }
        }
        private bool isEnumerating = false;

        /// <summary>
        /// Gets a value indicating whether app is currently enumerating
        /// </summary>
        public bool IsEnumerating
        {
            get
            {
                return isEnumerating;
            }

            private set
            {
                if (isEnumerating != value)
                {
                    isEnumerating = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("IsEnumerating"));
                }
            }
        }

        /// <summary>
        /// Source for <see cref="EnumerationFinished"/> property
        /// </summary>
        private bool enumorationFinished = false;

        /// <summary>
        /// Gets a value indicating whether the app is finished enumerating
        /// </summary>
        public bool EnumerationFinished
        {
            get
            {
                return enumorationFinished;
            }

            private set
            {
                if (enumorationFinished != value)
                {
                    enumorationFinished = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("EnumerationFinished"));
                }
            }
        }
        private async Task AddDeviceToList(DeviceInformation deviceInfo)
        {
            ObservableBluetoothLEDevice dev = new ObservableBluetoothLEDevice(deviceInfo);

            // Let's make it connectable by default, we have error handles in case it doesn't work
            bool shouldDisplay =
                ((dev.DeviceInfo.Properties.Keys.Contains("System.Devices.Aep.Bluetooth.Le.IsConnectable") &&
                    (bool)dev.DeviceInfo.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"])) ||
                ((dev.DeviceInfo.Properties.Keys.Contains("System.Devices.Aep.IsConnected") &&
                    (bool)dev.DeviceInfo.Properties["System.Devices.Aep.IsConnected"]));

            if (shouldDisplay)
            {
                // Need to lock as another DeviceWatcher might be modifying BluetoothLEDevices 
                try
                {
                    await BluetoothLEDevicesLock.WaitAsync();

                    await UpdateBatteryLevel(dev);

                    if (!BluetoothLEDevices.Contains(dev))
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            Windows.UI.Core.CoreDispatcherPriority.Normal,
                            () =>
                            {
                                BluetoothLEDevices.Add(dev);
                                Debug.WriteLine($"Added device name = {dev.DeviceInfo.Name}, address = {dev.BluetoothAddressAsString}");
                            });
                    }
                }
                finally
                {
                    BluetoothLEDevicesLock.Release();
                }
            }
            else
            {
                try
                {
                    await BluetoothLEDevicesLock.WaitAsync();
                    unusedDevices.Add(deviceInfo);
                }
                finally
                {
                    BluetoothLEDevicesLock.Release();
                }
            }
        }

        public void StopEnumeration()
        {
            if (deviceWatcher != null)
            {
                // Unregister the event handlers.
                deviceWatcher.Added -= DeviceWatcher_Added;
                deviceWatcher.Updated -= DeviceWatcher_Updated;
                deviceWatcher.Removed -= DeviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped -= DeviceWatcher_Stopped;

                advertisementWatcher.Received += AdvertisementWatcher_Received;

                // Stop the watchers
                deviceWatcher.Stop();
                deviceWatcher = null;

                advertisementWatcher.Stop();
                advertisementWatcher = null;
                IsEnumerating = false;
                EnumerationFinished = false;
            }
        }

    }
}
