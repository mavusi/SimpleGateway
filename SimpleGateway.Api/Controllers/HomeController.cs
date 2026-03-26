using Microsoft.AspNetCore.Mvc;

namespace SimpleGateway.Api.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
