using LabManager.DataAccess;
using LabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace LabManager.Repository
{
    public  class StaffRepository : IStaffRepository
    {
        private readonly ISqlDataAcess _dataAccess;


        public StaffRepository(ISqlDataAcess dataAccess)
        {
            _dataAccess = dataAccess;   
        }


        public async Task<bool> AddDoctor(Doctor doctor)
        {
            try
            {


                await _dataAccess.SaveData("", new { doctor.FullName, doctor.HpcsaNumber, doctor.Email });
                return true;
            }
            catch (Exception ex) {
            
             return false ; 
           
            }
        }
        public async Task<bool> AddTechnician(Technician tech)
        {
            try
            {


                await _dataAccess.SaveData("", new { tech.FullName,tech.Email,tech.TestTypes });
                return true;
            }
            catch (Exception ex)
            {

                return false;

            }
        }

       public async  Task<bool> UpdateDoctor(Doctor doctor)
        {

            try
            {
                await _dataAccess.SaveData("", doctor);
                return true;
            }
            catch
            {
                return false;   
            }

        }
        public async Task<bool> UpdateTechnician(Technician tech)
        {

            try
            {
                await _dataAccess.SaveData("", tech);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctor()
        {
            string query = "";
            return await _dataAccess.GetData<Doctor ,dynamic>(query,new {});
        }

        public async Task<IEnumerable<Technician>> GetTechnician()
        {
            string query = "";
            return await _dataAccess.GetData<Technician, dynamic>(query, new { });

        }

       public async  Task<Doctor> GetDoctorById(int id)
        {

            string query = "";
           IEnumerable<Doctor> result = await _dataAccess.GetData<Doctor,dynamic>(query, new {Id= id });

            return result.FirstOrDefault();

        }
        public async Task<Technician> GetTechnicianById(int id)
        {
            string query = "";
            IEnumerable<Technician> result = await _dataAccess.GetData<Technician, dynamic>(query, new { Id = id });

            return result.FirstOrDefault();

        }
    }
}
