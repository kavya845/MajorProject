
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using XRayDiagnosticSystem.Data;
using XRayDiagnosticSystem.Models;

namespace XRayDiagnosticSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly DatabaseHelper _db;

        public AdminController(DatabaseHelper db)
        {
            _db = db;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            // Dashboard Stats
            var stats = new
            {
                TotalPatients = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM hospital.Patients"),
                TotalScans = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM hospital.XRays"),
                Alerts = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM hospital.Reports WHERE Severity='Critical'")
            };
            ViewBag.Stats = stats;

            return View();
        }

        public async Task<IActionResult> Patients()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var dt = await _db.ExecuteQueryAsync("SELECT PatientID, FullName, Age, Gender, ContactNumber, Address, CreatedAt, Username FROM hospital.Patients ORDER BY CreatedAt DESC");
            // Convert to List<Patient> ... (Simulated for brevity, assume View receives DataTable or List)
            return View(dt);
        }

        public async Task<IActionResult> AuditLogs()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");
            var dt = await _db.ExecuteQueryAsync("SELECT TOP 100 * FROM hospital.AuditLogs ORDER BY Timestamp DESC");
            return View(dt);
        }
    }
}
