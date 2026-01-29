
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using XRayDiagnosticSystem.Data;
using XRayDiagnosticSystem.Models;

namespace XRayDiagnosticSystem.Controllers
{
    public class AuthController : Controller
    {
        private readonly DatabaseHelper _db;

        public AuthController(DatabaseHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1. Check Admin
            var adminDt = await _db.ExecuteQueryAsync("SELECT AdminID, Username, Password FROM hospital.Admins WHERE Username=@u AND Password=@p", 
                new SqlParameter[] { new SqlParameter("@u", username), new SqlParameter("@p", password) });
            
            if (adminDt.Rows.Count > 0)
            {
                HttpContext.Session.SetString("Role", "Admin");
                HttpContext.Session.SetInt32("UserID", (int)adminDt.Rows[0]["AdminID"]);
                HttpContext.Session.SetString("Username", adminDt.Rows[0]["Username"]?.ToString() ?? username);
                
                // Audit Log
                await LogAudit("Login", "Admins", (int)adminDt.Rows[0]["AdminID"], $"Admin '{username}' logged in", "Admin");
                
                return RedirectToAction("Index", "Admin");
            }

            // 2. Check Patient
            var patDt = await _db.ExecuteQueryAsync("SELECT PatientID, Username, Password FROM hospital.Patients WHERE Username=@u AND Password=@p", 
                new SqlParameter[] { new SqlParameter("@u", username), new SqlParameter("@p", password) });

            if (patDt.Rows.Count > 0)
            {
                HttpContext.Session.SetString("Role", "Patient");
                HttpContext.Session.SetInt32("UserID", (int)patDt.Rows[0]["PatientID"]);
                HttpContext.Session.SetString("Username", patDt.Rows[0]["Username"]?.ToString() ?? username);
                
                // Audit Log
                await LogAudit("Login", "Patients", (int)patDt.Rows[0]["PatientID"], $"Patient '{username}' logged in", "Patient");
                
                return RedirectToAction("Index", "PatientPanel");
            }

            ViewBag.Error = "Invalid username or password. Please check your credentials and try again.";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Patient patient)
        {
            // Simple registration - assumes valid input
            // Check username uniqueness
            var check = await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM hospital.Patients WHERE Username=@u", new SqlParameter[] { new SqlParameter("@u", patient.Username) });
            if (Convert.ToInt32(check ?? 0) > 0)
            {
                ViewBag.Error = "Username already exists";
                return View(patient);
            }

            string sql = "INSERT INTO hospital.Patients (FullName, Age, Gender, ContactNumber, Address, Username, Password) VALUES (@fn, @age, @gen, @con, @add, @us, @pass); SELECT SCOPE_IDENTITY();";
            SqlParameter[] pars = {
                new SqlParameter("@fn", patient.FullName),
                new SqlParameter("@age", patient.Age),
                new SqlParameter("@gen", patient.Gender),
                new SqlParameter("@con", patient.ContactNumber ?? ""),
                new SqlParameter("@add", patient.Address ?? ""),
                new SqlParameter("@us", patient.Username),
                new SqlParameter("@pass", patient.Password)
            };

            var newId = await _db.ExecuteScalarAsync(sql, pars);
            
            // Audit Log
            await LogAudit("Register", "Patients", Convert.ToInt32(newId), $"New patient registered: {patient.FullName} (Username: {patient.Username})", "System");
            
            TempData["Success"] = "Registration successful! You can now log in to your account.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            var username = HttpContext.Session.GetString("Username") ?? "Unknown";
            var role = HttpContext.Session.GetString("Role") ?? "Guest";
            var userId = HttpContext.Session.GetInt32("UserID");
            
            // Audit Log
            if (userId.HasValue)
            {
                await LogAudit("Logout", role == "Admin" ? "Admins" : "Patients", userId.Value, $"{role} '{username}' logged out", role);
            }
            
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private async Task LogAudit(string action, string table, int recordId, string details, string userRole)
        {
            try
            {
                string sql = "INSERT INTO hospital.AuditLogs (Action, TableName, RecordID, Details, UserRole) VALUES (@Act, @Tab, @Rec, @Det, @Role)";
                SqlParameter[] pars = {
                    new SqlParameter("@Act", action),
                    new SqlParameter("@Tab", table),
                    new SqlParameter("@Rec", recordId),
                    new SqlParameter("@Det", details),
                    new SqlParameter("@Role", userRole)
                };
                await _db.ExecuteNonQueryAsync(sql, pars);
            }
            catch
            {
                // Silently fail audit logging to not disrupt user flow
            }
        }
    }
}
