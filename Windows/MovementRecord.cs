using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorTag
{
    public class SingleRecord
    {
        public DateTime Time { get; set; }
        public MovementMeasurement Value { get; set; }
    }
    public class MovementRecord
    {
        private List<SingleRecord> _recording = new List<SingleRecord>();
        public String DeviceAddress { get; set; }
        public String AssignedName { get; set; }
        public List<SingleRecord> Recording { get { return _recording; } }
    }
}
