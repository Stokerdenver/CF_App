using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebAPI.Models
{
    public class Car
    {
        [Key]
        public string reg_number {  get; set; }
        public string model { get; set; }
        public int release_year { get; set; }
        
        // Внешний ключ для связи с User
        public int user_id { get; set; }

        [ForeignKey("user_id")]

        [JsonIgnore] // Игнорируем свойство User при сериализации
        public User? User { get; set; }
    }
}
