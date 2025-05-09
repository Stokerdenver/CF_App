using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dis_1.Model
{
    public class MainDataResponse
    {
        public bool isleader { get; set; }
        public double? predicted_speed { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public int speed { get; set; }
        public double bearing { get; set; }
        public DateTime timestamp { get; set; }
    }
}
