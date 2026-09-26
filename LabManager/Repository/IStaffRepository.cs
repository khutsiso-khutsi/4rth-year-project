using LabManager.Models;

namespace LabManager.Repositories
{
    public interface IStaffRepository
    {
        // ============================================================
        // DOCTOR
        // ============================================================

        Task<bool> AddDoctor(Doctor doctor);

        Task<bool> UpdateDoctor(Doctor doctor);

        Task<IEnumerable<Doctor>> GetAllDoctor();

        Task<Doctor> GetDoctorById(int id);


        // ============================================================
        // TECHNICIAN
        // ============================================================

        Task<bool> AddTechnician(Technician tech);

        Task<bool> UpdateTechnician(Technician tech);

        Task<IEnumerable<Technician>> GetTechnician();

        Task<Technician> GetTechnicianById(int id);


        // ============================================================
        // TEST TYPE
        // ============================================================

        Task<IEnumerable<TestType>> GetAllTestTypes();


    }

   public interface IUserProfileRepository
    {
            Task<UserProfileViewModel?> GetProfileByEmail(string email);

            Task<bool> UpdateProfile(UserProfileViewModel model);

            Task<string?> GetPasswordHash(string email);

            Task<bool> UpdatePassword(string email, string passwordHash);
   }
    
}