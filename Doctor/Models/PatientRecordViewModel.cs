using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doctor.Models
{
    public class PatientRecordViewModel
    {
        public int PatientID { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Surname { get; set; } = string.Empty;
        [Required, RegularExpression(@"^\d{13}$")] public string IDNumber { get; set; } = string.Empty;
        [Required, DataType(DataType.Date)] public DateTime DateOfBirth { get; set; }
        [Required] public string CellphoneNumber { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        public List<string> KnownMedicalConditions { get; set; } = new();
    }
}
