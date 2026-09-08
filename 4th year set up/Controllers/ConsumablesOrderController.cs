using LabManager.Models;
using LabManager.Repository;
using Microsoft.AspNetCore.Mvc;

namespace _4th_year_set_up.Controllers
{
    public class ConsumablesOrderController : Controller
    {
        private readonly IOrderRepository _order;


        public ConsumablesOrderController(IOrderRepository order)
        {
            _order = order;
        }

        public async Task<IActionResult> Add(ConsumableOrder order)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(order);
                }

                bool addOrder = await _order.AddOrder(order);

                if (addOrder) {


                }
                else
                {

                }


            }
            catch
            {

            }

            return View(order);
        }



    

         public async Task<IActionResult> Edit(int id)
        {
            var result = await _order.GetOrderById(id); 
            return View(result);    
        }

        [HttpPost]
        public async  Task<IActionResult> Edit(ConsumableOrder order)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(order);
                }

                bool updateOrder = await _order.UpdateOrder(order);

                if (updateOrder)
                {


                }
                else
                {

                }


            }
            catch
            {

            }

            return View(order);
        }


        public async Task<IActionResult> Delete(int id) { 
        

            var result= await _order.DeleteOrder(id);

            return RedirectToAction(nameof(DisplayAll));
            



        }

        public async Task<IActionResult> DisplayAll()
        {
            var result = await _order.GetAllOrders();   
            return View(result);    
        }

    }
}
