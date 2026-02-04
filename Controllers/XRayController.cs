using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using XRayDiagnosticSystem.Data;
using XRayDiagnosticSystem.Models;

namespace XRayDiagnosticSystem.Controllers
{
    public class XRayController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly IWebHostEnvironment _env;

        public XRayController(DatabaseHelper db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var dt = await _db.ExecuteQueryAsync("SELECT PatientID, FullName FROM hospital.Patients");
            ViewBag.Patients = dt.Rows;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(int patientId, IFormFile xRayImage, string bodyPart)
        {
            if (xRayImage != null && xRayImage.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + xRayImage.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await xRayImage.CopyToAsync(fileStream);
                }

                SqlParameter[] pars = {
                    new SqlParameter("@PatientId", patientId),
                    new SqlParameter("@Path", "/uploads/" + uniqueFileName),
                    new SqlParameter("@Notes", (object)DBNull.Value),
                    new SqlParameter("@BodyPart", bodyPart)
                };

                var xRayId = await _db.ExecuteScalarAsync("INSERT INTO hospital.XRays (PatientID, ImagePath, TechnicianNotes, BodyPart) VALUES (@PatientId, @Path, @Notes, @BodyPart); SELECT SCOPE_IDENTITY();", pars);
                
                return RedirectToAction("AutomatedAnalyze", "Diagnosis", new { xRayId = xRayId });
            }

            return View();
        }
    }
}
