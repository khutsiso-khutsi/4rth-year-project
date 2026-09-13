using LabManager.DataAccess;
using LabManager.Models;
using LabManager.Repository;

namespace LabManager.Repositories
{
    public class StaffRepository : IStaffRepository
    {
        private readonly ISqlDataAcess _db;

        public StaffRepository(ISqlDataAcess db)
        {
            _db = db;
        }


        // ============================================================
        // ADD DOCTOR
        // ============================================================

        public async Task<bool> AddDoctor(Doctor doctor)
        {
            try
            {
                await _db.SaveData(
                    "sp_AddDoctor",
                    new
                    {
                        doctor.FirstName,
                        doctor.LastName,
                        doctor.HCPSANumber,
                        doctor.EmailAddress,
                        doctor.ContactNumber
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }


        // ============================================================
        // UPDATE DOCTOR
        // ============================================================

        public async Task<bool> UpdateDoctor(Doctor doctor)
        {
            try
            {
                await _db.SaveData(
                    "sp_UpdateDoctor",
                    new
                    {
                        doctor.Id,
                        doctor.FirstName,
                        doctor.LastName,
                        doctor.HCPSANumber,
                        doctor.EmailAddress,
                        doctor.ContactNumber
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }


        // ============================================================
        // GET ALL DOCTORS
        // ============================================================

        public async Task<IEnumerable<Doctor>> GetAllDoctor()
        {
            return await _db.GetData<Doctor, object>(
                "sp_GetDoctors",
                new { });
        }


        // ============================================================
        // GET DOCTOR BY ID
        // ============================================================

        public async Task<Doctor> GetDoctorById(int id)
        {
            var result = await _db.GetData<Doctor, object>(
                "sp_GetDoctorById",
                new
                {
                    Id = id
                });

            return result.FirstOrDefault();
        }


        // ============================================================
        // ADD TECHNICIAN
        // ============================================================

        public async Task<bool> AddTechnician(Technician tech)
        {
            try
            {
                await _db.SaveData(
                    "sp_AddTechnician",
                    new
                    {
                        tech.FirstName,
                        tech.LastName,
                        tech.EmployeeNumber,
                        tech.EmailAddress,
                        tech.TestTypeID
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }


        // ============================================================
        // UPDATE TECHNICIAN
        // ============================================================

        public async Task<bool> UpdateTechnician(Technician tech)
        {
            try
            {
                await _db.SaveData(
                    "sp_UpdateTechnician",
                    new
                    {
                        tech.Id,
                        tech.FirstName,
                        tech.LastName,
                        tech.EmployeeNumber,
                        tech.EmailAddress,
                        tech.TestTypeID
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }


        // ============================================================
        // GET ALL TECHNICIANS
        // ============================================================

        public async Task<IEnumerable<Technician>> GetTechnician()
        {
            return await _db.GetData<Technician, object>(
                "sp_GetTechnicians",
                new { });
        }


        // ============================================================
        // GET TECHNICIAN BY ID
        // ============================================================

        public async Task<Technician> GetTechnicianById(int id)
        {
            var result = await _db.GetData<Technician, object>(
                "sp_GetTechnicianById",
                new
                {
                    Id = id
                });

            return result.FirstOrDefault();
        }


        // ============================================================
        // GET TEST TYPES
        // ============================================================

        public async Task<IEnumerable<TestType>> GetAllTestTypes()
        {
            return await _db.GetData<TestType, object>(
                "sp_GetTestTypes",
                new { });
        }
    }
}