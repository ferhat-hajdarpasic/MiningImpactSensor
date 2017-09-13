using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace SensorTag
{
    class SensorTagDeviceIdParser
    {
        public static String parse(DeviceInformation info)
        {
            string id = info.Id;
            string serviceGuid = "";
            string instanceUuid = "";
            string vid = "";
            string pid = "";
            string MacAddress = info.Id.Substring("BluetoothLE#BluetoothLE".Length).Replace(":", "");

            // something like this:
            // \\?\BTHLEDevice#{0000fff0-0000-1000-8000-00805f9b34fb}_Dev_VID&01000d_PID&0000_REV&0110_b4994c5d8fc1#7&2839f98&c&0023#{6e3bb679-4372-40c8-9eaa-4509df260cd8}
            //"BluetoothLE#BluetoothLEf4:5c:89:b3:ca:72-24:71:89:04:7e:84"
            if (id.StartsWith(@"\\?\BTHLEDevice#"))
            {
                int i = id.IndexOf('{');
                if (i > 0 && i < id.Length - 1)
                {
                    i++;
                    int j = id.IndexOf('}', i);
                    if (j > i)
                    {
                        serviceGuid = id.Substring(i, j - i);
                        if (j < id.Length - 1)
                        {
                            i = id.IndexOf('{', j);
                            string tail = id.Substring(j + 1);
                            if (i > 0 && i < id.Length - 1)
                            {
                                i++;
                                j = id.IndexOf('}', i);
                                if (j > i)
                                {
                                    instanceUuid = id.Substring(i, j - i);
                                }
                            }
                            i = tail.IndexOf('#');
                            if (i > 0)
                            {
                                tail = tail.Substring(0, i);
                            }
                            string[] parts = tail.Split('_');
                            foreach (string p in parts)
                            {
                                if (p.StartsWith("VID&"))
                                {
                                    vid = p.Substring(4);
                                }
                                else if (p.StartsWith("PID&"))
                                {
                                    pid = p.Substring(4);
                                }
                                else if (p.StartsWith("REV&"))
                                {
                                    pid = p.Substring(4);
                                }
                                else if (p.Length == 12)
                                {
                                    MacAddress = p;
                                }
                            }
                        }
                    }
                }
            }
            return MacAddress;
        }
    }
}
