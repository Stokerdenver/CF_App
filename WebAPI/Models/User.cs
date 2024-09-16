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
        public string reg_number {  get; set; }
        public string f_carModel { get; set; }
    }
}
