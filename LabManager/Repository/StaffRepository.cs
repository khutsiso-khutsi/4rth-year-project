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
            return await _db.GetData<TestType, dynamic>(
                "sp_GetTestTypes",
                new { });
        }


    }


    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly ISqlDataAcess _dataAccess;

        public UserProfileRepository(ISqlDataAcess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        // =========================================================
        // GET USER PROFILE
        // =========================================================

        public async Task<UserProfileViewModel?> GetProfileByEmail(string email)
        {
            var result = await _dataAccess.GetData<UserProfileViewModel, dynamic>(
                "sp_GetUserProfileByEmail",
                new
                {
                    Email = email
                });

            return result.FirstOrDefault();
        }

        // =========================================================
        // UPDATE PROFILE
        // =========================================================

        public async Task<bool> UpdateProfile(UserProfileViewModel model)
        {
            try
            {
                await _dataAccess.SaveData(
                    "sp_UpdateUserProfile",
                    new
                    {
                        UserID = model.UserID,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        CellphoneNumber = model.CellphoneNumber,
                        HomeAddress = model.HomeAddress
                    });

                return true;
            }
            catch
            {
                throw;
            }
        }


        // =========================================================
        // GET CURRENT PASSWORD HASH
        // =========================================================

        public async Task<string?> GetPasswordHash(string email)
        {
            try
            {
                var result = await _dataAccess.GetData<string, dynamic>(
                    "sp_GetUserPasswordHash",
                    new
                    {
                        Email = email
                    });

                return result.FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }


        // =========================================================
        // UPDATE PASSWORD
        // =========================================================

        public async Task<bool> UpdatePassword(
            string email,
            string passwordHash)
        {
            try
            {
                await _dataAccess.SaveData(
                    "sp_UpdateUserPassword",
                    new
                    {
                        Email = email,
                        PasswordHash = passwordHash
                    });

                return true;
            }
            catch
            {
                throw;
            }
        }
    }
}