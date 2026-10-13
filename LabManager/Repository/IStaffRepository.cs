using LabManager.Models;


public interface IStaffRepository
{
    Task<int> CreateDoctorAccount(
        Doctor doctor,
        string passwordHash,
        DateTime expiryUtc);

    Task<int> CreateTechnicianAccount(
        Technician technician,
        string passwordHash,
        DateTime expiryUtc);

    Task<bool> UpdateDoctor(Doctor doctor);
    Task<IEnumerable<Doctor>> GetAllDoctor();
    Task<Doctor?> GetDoctorById(int id);

    Task<bool> UpdateTechnician(Technician technician);
    Task<IEnumerable<Technician>> GetTechnician();
    Task<Technician?> GetTechnicianById(int id);

    Task<IEnumerable<TestType>> GetAllTestTypes();

    Task<IEnumerable<TestType>> GetTestType();


}

public interface IUserProfileRepository
{
    Task<UserProfileViewModel?> GetProfileByEmail(string email);
    Task<bool> UpdateProfile(UserProfileViewModel model);
    Task<string?> GetPasswordHash(string email);
    Task<bool> UpdatePassword(string email, string passwordHash);
}
