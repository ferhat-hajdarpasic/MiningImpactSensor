using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorTag
{
    public class MovementRecord
    {
        public String DeviceAddress { get; set; }
        public String AssignedName { get; set; }
        public DateTime Time { get; set; }
        public MovementMeasurement Value { get; set; }
    }
}
