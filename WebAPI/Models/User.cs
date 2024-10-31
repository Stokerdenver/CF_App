using System.ComponentModel.DataAnnotations;

namespace WebAPI.Models
{
    public class User
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public string sex { get; set; }
        public int age { get; set; }
        public int driving_exp { get; set; }

        // Связь "один ко многим" - один пользователь может иметь много автомобилей
        public ICollection<Car> Cars { get; set; } = new List<Car>();
    }
}
