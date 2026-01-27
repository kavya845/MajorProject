
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using XRayDiagnosticSystem.Data;

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
    }
}
