using _4th_year_set_up.Models;
using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;

namespace _4th_year_set_up.Controllers
{
    public class StaffController : Controller
    {
        private readonly IStaffRepository _staffRepository;

        public StaffController(IStaffRepository staff)
        {
            _staffRepository = staff;   
        }


        public async Task<IActionResult> Add(Doctor  staff)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(staff);

                }

                bool addConsumables = await _staffRepository.AddDoctor(staff);


                if (addConsumables)
                {


                }
                else
                {

                }


            }
            catch (Exception ex)
             {

            }

            return View(staff);
        }


        public async Task<IActionResult> Add(Technician tech)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(tech);

                }

                bool addConsumables = await _staffRepository.AddTechnician(tech);


                if (addConsumables)
                {


                }
                else
                {

                }


            }
            catch (Exception ex)
            {

            }

            return View(tech);

        }


        public async Task<IActionResult> EditDoctor(int id)
        {
            var results = await _staffRepository.GetDoctorById(id);
        
            return View(results);
           
            
        }

            [HttpPost]

        public async Task<IActionResult> Edit(Doctor doctor)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(doctor);
                }

                bool updateCondtion = await _staffRepository.UpdateDoctor(doctor);

                if (updateCondtion)
                {
                    TempData["msg"] = "";
                }
                else
                {

                    TempData["msg"] = "";
                }

            }
            catch
            {
                TempData["msg"] = "";
            }

            return View(doctor);


        }



        public async Task<IActionResult> EditTechnician(int id)
        {

            var results = await _staffRepository.GetTechnicianById(id);

            return View(results);

        }

        [HttpPost]

        public async Task<IActionResult> Edit(Technician technician)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(technician);
                }

                bool updateCondtion = await _staffRepository.UpdateTechnician(technician);

                if (updateCondtion)
                {
                    TempData["msg"] = "";
                }
                else
                {

                    TempData["msg"] = "";
                }
            }

            catch
            {
                TempData["msg"] = "";
            }

            return View(technician);

        }


        public async Task<IActionResult> DisplayAllDoctor() {


            var result = await _staffRepository.GetAllDoctor();

            return View(result);

        }


        public async Task<IActionResult> DisplayAllTech()
        {


            var result = await _staffRepository.GetTechnician();

            return View(result);

        }



    }
}
