using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorTag
{
    class PersistedDevice
    {
        public String DeviceId { get; set; }
        public String AssignedToName { get; set; }
        public Boolean Selected { get; set; }
    }
}
