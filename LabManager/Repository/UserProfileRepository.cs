using Dapper;
using LabManager.DataAccess;
using LabManager.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LabManager.Repository
{
    public interface IUserRepository
    {
        Task<UserProfileViewModel> GetProfile(string email);

        Task<bool> UpdateAccountInfo(string currentEmail, UserProfileViewModel model);

        Task<bool> UpdateContactDetails(string email, string cellphoneNumber, string homeAddress);

        Task<string> GetPasswordHash(string email);

        Task<bool> UpdatePasswordHash(string email, string passwordHash);
    }


    public class UserRepository : IUserRepository
    {
        private readonly ISqlDataAcess _dataAcess;

        public UserRepository(ISqlDataAcess dataAcess)
        {
            _dataAcess = dataAcess;
        }

        // =====================================================
        // GET PROFILE (by the signed-in user's email)
        // =====================================================
        public async Task<UserProfileViewModel> GetProfile(string email)
        {
            try
            {
                IEnumerable<UserProfileViewModel> result =
                    await _dataAcess.GetData<UserProfileViewModel, dynamic>(
                        "sp_GetUserProfile",
                        new
                        {
                            Email = email
                        });

                return result.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        // =====================================================
        // UPDATE ACCOUNT INFORMATION
        // currentEmail identifies the row; model.Email is the new value
        // =====================================================
        public async Task<bool> UpdateAccountInfo(string currentEmail, UserProfileViewModel model)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_UpdateUserAccountInfo",
                    new
                    {
                        CurrentEmail = currentEmail,
                        model.FirstName,
                        model.LastName,
                        model.Email,
                        model.IDNumber,
                        model.DateOfBirth
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // UPDATE CELLPHONE + HOME ADDRESS
        // =====================================================
        public async Task<bool> UpdateContactDetails(string email, string cellphoneNumber, string homeAddress)
        {
            try
            {
                await _dataAcess.SaveData(
                    "sp_UpdateUserContact",
                    new
                    {
                        Email = email,
                        CellphoneNumber = cellphoneNumber,
                        HomeAddress = homeAddress
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // PASSWORD
        // =====================================================
        public async Task<string> GetPasswordHash(string email)
        {
            try
            {
                IEnumerable<UserCredentials> result =
                    await _dataAcess.GetData<UserCredentials, dynamic>(
                        "sp_GetUserPasswordHash",
                        new
                        {
                            Email = email
                        });

                return result.FirstOrDefault()?.PasswordHash;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdatePasswordHash(string email, string passwordHash)
        {
            try
            {
                await _dataAcess.SaveData(
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
                return false;
            }
        }
    }
}
