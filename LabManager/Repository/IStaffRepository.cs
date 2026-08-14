using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Repository
{
    public interface IStaffRepository
    {
        Task<bool> AddDoctor(Doctor doctor) ;
        Task<bool> AddTechnician(Technician tech);

        Task<bool> UpdateDoctor(Doctor doctor);
        Task<bool> UpdateTechnician(Technician tech);

        Task<IEnumerable<Doctor>> GetAllDoctor();

        Task<IEnumerable<Technician>> GetTechnician();

        Task<Doctor> GetDoctorById(int id);
        Task<Technician> GetTechnicianById(int id);




    }
}
