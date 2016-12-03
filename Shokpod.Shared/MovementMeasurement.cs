using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorTag
{
    public class MovementMeasurement
    {
        public MovementMeasurement(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Acceleration { get { return Math.Round(Math.Sqrt(X * X + Y * Y + Z * Z), 2); } }
    }
}
