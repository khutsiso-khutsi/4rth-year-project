
using LabManager.DataAccess;
using LabManager.Models;
using LabManager.Repository;

namespace LabManager.Repository
{
    public class StaffRepository : IStaffRepository
    {
        private readonly ISqlDataAcess _db;

        public StaffRepository(ISqlDataAcess db)
        {
            _db = db;
        }

        public async Task<int> CreateDoctorAccount(
            Doctor doctor,
            string passwordHash,
            DateTime expiryUtc)
        {
            var result = await _db.GetData<int, dynamic>(
                "sp_CreateStaffUser",
                new
                {
                    doctor.FirstName,
                    doctor.LastName,
                    doctor.EmailAddress,
                    Role = "Doctor",
                    PasswordHash = passwordHash,
                    TemporaryPasswordExpiry = expiryUtc,
                    doctor.HCPSANumber,
                    doctor.ContactNumber,
                    EmployeeNumber = (string?)null,
                    TestTypeID = (int?)null
                });

            return result.FirstOrDefault();
        }

        public async Task<int> CreateTechnicianAccount(
            Technician technician,
            string passwordHash,
            DateTime expiryUtc)
        {
            var result = await _db.GetData<int, dynamic>(
                "sp_CreateStaffUser",
                new
                {
                    technician.FirstName,
                    technician.LastName,
                    technician.EmailAddress,
                    Role = "Technician",
                    PasswordHash = passwordHash,
                    TemporaryPasswordExpiry = expiryUtc,
                    HCPSANumber = (string?)null,
                    ContactNumber = (string?)null,
                    technician.EmployeeNumber,
                    technician.TestTypeID
                });

            return result.FirstOrDefault();
        }

        public async Task<bool> UpdateDoctor(Doctor doctor)
        {
            await _db.SaveData("sp_UpdateDoctor", new
            {
                Id = doctor.Id,
                doctor.FirstName,
                doctor.LastName,
                doctor.HCPSANumber,
                doctor.EmailAddress,
                doctor.ContactNumber
            });

            return true;
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctor()
        {
            return await _db.GetData<Doctor, dynamic>(
                "sp_GetAllDoctors", new { });
        }

        public async Task<Doctor?> GetDoctorById(int id)
        {
            var result = await _db.GetData<Doctor, dynamic>(
                "sp_GetDoctorById", new { Id = id });

            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<TestType>> GetTestType()
        {
            return await _db.GetData<TestType, dynamic>(
                "sp_GetTestTypes", new { });
        }

        public async Task<bool> UpdateTechnician(Technician technician)
        {
            await _db.SaveData("sp_UpdateTechnician", new
            {
                Id = technician.Id,
                technician.FirstName,
                technician.LastName,
                technician.EmployeeNumber,
                technician.EmailAddress,
                technician.TestTypeID
            });

            return true;
        }

        public async Task<IEnumerable<Technician>> GetTechnician()
        {
            return await _db.GetData<Technician, dynamic>(
                "sp_GetAllTechnicians", new { }); 
        }

        public async Task<Technician?> GetTechnicianById(int id)
        {
            var result = await _db.GetData<Technician, object>(
                "sp_GetTechnicianById", new { Id = id });

            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<TestType>> GetAllTestTypes()
        {
            return await _db.GetData<TestType, dynamic>(
                "sp_GetTestTypes", new { });
        }

        // Returns a mapping of TechnicianId -> List of TestTypeIds. The stored procedure
        // should return rows with columns TechnicianId and TestTypeId.
        public async Task<Dictionary<int, List<int>>> GetTechnicianTestTypeMap()
        {
            // Local DTO to match expected columns from the query
            var rows = await _db.GetData<TechnicianTestTypeRow, dynamic>(
                "sp_GetTechnicianTestTypeMap", new { });

            var map = new Dictionary<int, List<int>>();

            foreach (var r in rows)
            {
                if (!map.TryGetValue(r.TechnicianId, out var list))
                {
                    list = new List<int>();
                    map[r.TechnicianId] = list;
                }

                if (r.TestTypeId > 0 && !list.Contains(r.TestTypeId)) list.Add(r.TestTypeId);
            }

            return map;
        }

        // Replaces the technician's assigned test types. Deletes existing mappings then inserts each provided id.
        public async Task SetTechnicianTestTypes(int technicianId, IEnumerable<int> testTypeIds)
        {
            var ids = (testTypeIds ?? Enumerable.Empty<int>()).Where(i => i > 0).Distinct();

            await _db.SaveData(
                "sp_SetTechnicianTestTypes",
                new { TechnicianId = technicianId, TestTypeIds = string.Join(",", ids) });
        }
    }

    // Private DTO used to read rows from sp_GetTechnicianTestTypeMap
    internal class TechnicianTestTypeRow
    {
        public int TechnicianId { get; set; }
        public int TestTypeId { get; set; }
    }
}
