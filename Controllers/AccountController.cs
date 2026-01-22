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
            // Simple hardcoded login for demo
            if (username == "admin" && password == "admin123")
            {
                // In a real app, use Cookie Auth
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Invalid credentials";
            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Login");
        }
    }
}
