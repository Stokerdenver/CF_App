using System.ComponentModel.DataAnnotations;

namespace WebAPI.Models
{
    public class MainData
    {
        [Key]
        public int id { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public int speed { get; set; }
        public DateTime timestamp { get; set; }
    }
}
