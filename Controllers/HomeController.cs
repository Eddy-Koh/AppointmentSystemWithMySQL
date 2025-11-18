using System.Web.Mvc;

namespace AppointmentBookingSystem.Controllers
{
    public class HomeController : Controller
    {
        // About the home page display message
        public ActionResult Index()
        {
            if (Session["Username"] == null)
            { ViewBag.Message = "You need to login to access the system."; }
            else
            { ViewBag.Message = $"Welcome, {Session["Username"]} ({Session["Role"]}) !"; }

            return View();
        }
    }
}
