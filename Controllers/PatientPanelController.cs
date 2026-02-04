
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using XRayDiagnosticSystem.Data;
using XRayDiagnosticSystem.Models;

namespace XRayDiagnosticSystem.Controllers
{
    public class PatientPanelController : Controller
    {
        private readonly DatabaseHelper _db;

        public PatientPanelController(DatabaseHelper db)
        {
            _db = db;
        }

        private int? GetCurrentUserId() => HttpContext.Session.GetInt32("UserID");
        private bool IsPatient() => HttpContext.Session.GetString("Role") == "Patient";

        public async Task<IActionResult> Index()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Auth");
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            // Load My Scans
            string sql = @"
                SELECT x.*, r.DiagnosisResult, r.Severity 
                FROM hospital.XRays x 
                LEFT JOIN hospital.Reports r ON x.XRayID = r.XRayID 
                WHERE x.PatientID = @pid 
                ORDER BY x.UploadDate DESC";
            
            var dt = await _db.ExecuteQueryAsync(sql, new SqlParameter[] { new SqlParameter("@pid", userId) });
            return View(dt);
        }

        [HttpGet]
        public IActionResult UploadScan()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Auth");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadScan(IFormFile file, string bodyPart)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Auth");
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (file != null && file.Length > 0)
            {
                // Save File
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save DB
                string sql = "INSERT INTO hospital.XRays (PatientID, ImagePath, BodyPart, UploadDate, Status) VALUES (@pid, @path, @part, GETDATE(), 'Pending'); SELECT SCOPE_IDENTITY();";
                SqlParameter[] pars = {
                    new SqlParameter("@pid", userId),
                    new SqlParameter("@path", "/uploads/" + fileName),
                    new SqlParameter("@part", bodyPart)
                };
                
                int newXrayId = Convert.ToInt32(await _db.ExecuteScalarAsync(sql, pars));

                // Auto-trigger Analysis redirection
                return RedirectToAction("AutomatedAnalyze", "Diagnosis", new { xRayId = newXrayId });
            }
            
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Auth");
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var dt = await _db.ExecuteQueryAsync("SELECT PatientID, FullName, Age, Gender, ContactNumber, Address FROM hospital.Patients WHERE PatientID = @id", 
                new SqlParameter[] { new SqlParameter("@id", userId) });
            
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var patient = new Patient
            {
                PatientId = Convert.ToInt32(row["PatientID"]),
                FullName = row["FullName"]?.ToString() ?? "",
                Age = Convert.ToInt32(row["Age"]),
                Gender = row["Gender"]?.ToString(),
                ContactNumber = row["ContactNumber"]?.ToString(),
                Address = row["Address"]?.ToString()
            };
            return View(patient);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(Patient patient)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Auth");
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                SqlParameter[] pars = {
                    new SqlParameter("@PatientID", userId), // Ensure we only update the current user
                    new SqlParameter("@FullName", patient.FullName),
                    new SqlParameter("@Age", patient.Age),
                    new SqlParameter("@Gender", patient.Gender),
                    new SqlParameter("@ContactNumber", patient.ContactNumber),
                    new SqlParameter("@Address", patient.Address)
                };
                
                await _db.ExecuteNonQueryAsync("hospital.sp_UpdatePatient", pars, CommandType.StoredProcedure);
                
                // Optional: Update session if name changed
                HttpContext.Session.SetString("Username", patient.FullName); // Use FullName as display name

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        private async Task LogAudit(string action, string table, int recordId, string details, string role)
        {
            string sql = "INSERT INTO hospital.AuditLogs (Action, TableName, RecordID, Details, UserRole) VALUES (@Act, @Tab, @Rec, @Det, @Role)";
            SqlParameter[] pars = {
                new SqlParameter("@Act", action),
                new SqlParameter("@Tab", table),
                new SqlParameter("@Rec", recordId),
                new SqlParameter("@Det", details),
                new SqlParameter("@Role", role)
            };
            await _db.ExecuteNonQueryAsync(sql, pars);
        }
    }
}
