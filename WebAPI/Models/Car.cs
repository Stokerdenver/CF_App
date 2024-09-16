using System.ComponentModel.DataAnnotations;

namespace WebAPI.Models
{
    public class Car
    {
        [Key]
        public string reg_number {  get; set; }
        public string model { get; set; }
        public int release_year { get; set; }
    }
}
