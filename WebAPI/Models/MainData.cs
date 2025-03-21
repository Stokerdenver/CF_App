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
        public string username { get; set; }
        public bool isleader { get; set; }
        public string? current_car{ get; set; }
        public double bearing { get; set; }


    }
}
