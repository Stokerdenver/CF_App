using System.ComponentModel.DataAnnotations;

namespace WebAPI.Models
{
    public class WeatherData
    {
        [Key]
        public int id { get; set; }
        public DateTime timestamp { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public double temp_c { get; set; }
        public double wind_kph { get; set; }
        public double precip_mm { get; set; }
        public int is_day { get; set; }
        public string username { get; set; }
        public bool isleader { get; set; }
    }
}
