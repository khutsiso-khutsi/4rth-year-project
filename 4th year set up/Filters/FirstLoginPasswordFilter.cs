using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace _4th_year_set_up.Filters
{
    /// <summary>
    /// Spec: patients, doctors and technicians must change their password at
    /// first login. While the session is flagged "MustChangePassword", every
    /// page except the change-password page itself and Logout sends the user
    /// to Home/ChangePassword, so they can't skip it by typing another URL.
    /// Registered once in Program.cs, so it covers every controller.
    /// </summary>
    public class FirstLoginPasswordFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Session.GetString("MustChangePassword") != "1")
                return;

            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();
            if (controller == "Home" && (action == "ChangePassword" || action == "Logout"))
                return;

            context.Result = new RedirectToActionResult("ChangePassword", "Home", null);
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}