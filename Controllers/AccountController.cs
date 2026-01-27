using Microsoft.AspNetCore.Mvc;

namespace XRayDiagnosticSystem.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Redirect to AuthController for proper authentication
            return RedirectToAction("Login", "Auth");
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Login");
        }
    }
}
