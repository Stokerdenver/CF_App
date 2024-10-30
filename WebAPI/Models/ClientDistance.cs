using System.ComponentModel.DataAnnotations;

namespace WebAPI.Models
{
    public class ClientDistance
    {
        [Key]
        public int id { get; set; }
        public string client_id1 { get; set; }
        public string client_id2 { get; set; }
        public double distance { get; set; }
        public DateTime timestamp { get; set; }
     
    }
}
