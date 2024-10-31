using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dis_1.Model
{
    class UserC
    {
        public int id { get; set; }
        public string name { get; set; }
        public string sex { get; set; }
        public int age { get; set; }
        public int driving_exp { get; set; }

        // Свойство для автомобилей
        public List<CarC> Cars { get; set; }

    }
}
