using System.ComponentModel.DataAnnotations;

namespace LabManager.Models
{
    public class Staff
    {
        [Key]
        public int Id { get; set; }

     

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        public string Role { get; set; }

        public string Status { get; set; }


        [Display(Name = "HPCSA Number")]
        public string HPCSANumber { get; set; }

        [Display(Name = "Employee Number")]
        public string EmployeeNumber { get; set; }

        [Display(Name = "Test Types")]
        public string TestTypes { get; set; }
    }
}