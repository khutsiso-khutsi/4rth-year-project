using _4th_year_set_up.Models;
using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class ConsumablesController : Controller
    {
        private readonly IConsumablesRepository _consumables;

        public ConsumablesController(IConsumablesRepository consumables)
        {
            _consumables = consumables;
        }

        [HttpPost]
        public async Task<IActionResult> Add(Consumable consumable)
        {
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        return View(consumable);

                    }

                    bool addConsumables = await _consumables.AddConsumables(consumable);


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

                return View(consumable);
            }




        }

        public async Task<IActionResult> Add(Supplier supplier) {


            try
            {
                if (!ModelState.IsValid)
                {
                    return View(supplier);

                }

                bool addSupplier = await _consumables.AddSupplier(supplier);


                if (addSupplier)
                {



                }
                else
                {

                }


            }
            catch (Exception ex)
            {

            }

            return View(supplier);



        }


        public async Task<IActionResult> Edit(int id)
        {
            var results = await _consumables.GetById(id);
            return View(results);   
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Consumable consumable)
        {


            try
            {
                if (!ModelState.IsValid)
                {
                    return View(consumable);

                }

                bool updateConsumables = await _consumables.UpdateConsomables(consumable);


                if (updateConsumables)
                {



                }
                else
                {

                }


            }
            catch (Exception ex)
            {

            }

            return View(consumable);





        }

        public async Task<IActionResult> Delete(int id)
        {
            var result  = await _consumables.DeleteConsomubles(id);

            return RedirectToAction(nameof(DisplayAll));

        }


        public async Task<IActionResult> DisplayAll()
        {
            var results = await _consumables.GetAllConsumables();

            return View(results);
        }

    }
}
