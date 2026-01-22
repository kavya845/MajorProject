using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using XRayDiagnosticSystem.Data;
using XRayDiagnosticSystem.Helpers;
using XRayDiagnosticSystem.Models;

namespace XRayDiagnosticSystem.Controllers
{
    public class DiagnosisController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly RuleEngine _engine;
        private readonly MLService _ml;
        private readonly IWebHostEnvironment _env;

        public DiagnosisController(DatabaseHelper db, RuleEngine engine, MLService ml, IWebHostEnvironment env)
        {
            _db = db;
            _engine = engine;
            _ml = ml;
            _env = env;
        }

        public async Task<IActionResult> Analyze(int xRayId)
        {
            var dt = await _db.ExecuteQueryAsync("SELECT x.*, p.FullName as PatientName FROM XRays x JOIN Patients p ON x.PatientID = p.PatientID WHERE x.XRayID = @id", new SqlParameter[] { new SqlParameter("@id", xRayId) });
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var xRay = new XRay
            {
                XRayId = Convert.ToInt32(row["XRayID"]),
                PatientId = Convert.ToInt32(row["PatientID"]),
                ImagePath = row["ImagePath"]?.ToString() ?? "",
                TechnicianNotes = row["TechnicianNotes"]?.ToString() ?? "",
                PatientName = row["PatientName"]?.ToString() ?? "Unknown"
            };

            // AI Preview Scan
            string absolutePath = Path.Combine(_env.WebRootPath, xRay.ImagePath.TrimStart('/'));
            ViewBag.MLResults = await _ml.PredictAsync(absolutePath);

            return View(xRay);
        }

        public async Task<IActionResult> AutomatedAnalyze(int xRayId)
        {
            // 1. Get Image Path and Anatomy Metadata (BodyPart)
            var xRayDt = await _db.ExecuteQueryAsync("SELECT ImagePath, BodyPart FROM XRays WHERE XRayID = @id", new SqlParameter[] { new SqlParameter("@id", xRayId) });
            if (xRayDt == null || xRayDt.Rows.Count == 0) return NotFound();
            string imgPath = xRayDt.Rows[0]["ImagePath"]?.ToString() ?? "";
            string bodyPart = xRayDt.Rows[0]["BodyPart"]?.ToString() ?? "Chest";
            string absPath = Path.Combine(_env.WebRootPath, imgPath.TrimStart('/'));

            // 2. Perform Feature Analysis (Deterministic Mapping from AI Heuristics)
            var predictions = await _ml.PredictAsync(absPath);
            bool isAbnormal = predictions.Any(p => p.Severity == "Critical" || p.Severity == "Moderate");
            string aiDetectedPart = predictions.FirstOrDefault()?.Anatomy ?? "Unknown";

            // 3. VISUAL SANITY CHECK (Cross-reference Manual Selection with Image Scan)
            bool isMismatch = false;
            if (aiDetectedPart == "Chest" && bodyPart != "Chest") isMismatch = true;
            if (aiDetectedPart == "Hand/Extremity" && bodyPart == "Chest") isMismatch = true;

            // 4. DETERMINISTIC RULE-BASED ENGINE (Switch Statement)
            string diagnosisResult = "Normal";
            string severity = "Normal";
            string recommendation = "Routine follow-up prescribed.";
            string doctorComments = $"Visual Verification: {aiDetectedPart}. Clinical Context: {bodyPart} focus. ";

            if (isMismatch)
            {
                diagnosisResult = "ANATOMICAL MISMATCH DETECTED";
                severity = "Moderate";
                recommendation = "RE-UPLOAD SCAN: The uploaded image visual profile (likely " + aiDetectedPart + ") does not match the selected focus (" + bodyPart + "). Analysis halted for safety.";
                doctorComments += "SAFETY ALERT: Visual profile mismatch. System refused to apply " + bodyPart + " rules to a scan appearing as " + aiDetectedPart + ".";
            }
            else
            {
                switch (bodyPart)
                {
                    case "Hand":
                    case "Leg":
                        if (isAbnormal)
                        {
                            diagnosisResult = "Potential Fracture";
                            severity = "Critical";
                            recommendation = "IMMEDIATE ORTHOPEDIC REVIEW: Suspected cortical interruption. Immobilize joint and consult surgeon.";
                            doctorComments += $"Findings suggest acute skeletal structural instability in the {bodyPart}.";
                        }
                        else
                        {
                            diagnosisResult = "Normal";
                            doctorComments += $"No visible fracture or joint displacement detected in the {bodyPart}.";
                        }
                        break;

                    case "Chest":
                    default:
                        if (isAbnormal)
                        {
                            diagnosisResult = "Pulmonary Infection";
                            severity = "Critical";
                            recommendation = "IMMEDIATE RADIOLOGY VERIFICATION: Findings suggest extensive pulmonary consolidation. Possible pneumonia.";
                            doctorComments += "Increased lung opacity suggests high-density fluid accumulation.";
                        }
                        else
                        {
                            diagnosisResult = "Normal";
                            doctorComments += "Chest cavity appears clear with no significant focal opacities.";
                        }
                        break;
                }
            }

            // 4. Save and Complete using ADO.NET
            SqlParameter[] pars = {
                new SqlParameter("@XRayId", xRayId),
                new SqlParameter("@Result", diagnosisResult),
                new SqlParameter("@Comments", doctorComments),
                new SqlParameter("@Conf", isAbnormal ? 92 : 98),
                new SqlParameter("@Sev", severity),
                new SqlParameter("@Rec", recommendation)
            };

            await _db.ExecuteNonQueryAsync("INSERT INTO Reports (XRayID, DiagnosisResult, DoctorComments, Confidence, Severity, Recommendations) VALUES (@XRayId, @Result, @Comments, @Conf, @Sev, @Rec)", pars);
            await _db.ExecuteNonQueryAsync("UPDATE XRays SET Status = 'Completed' WHERE XRayID = @XRayId", new SqlParameter[] { new SqlParameter("@XRayId", xRayId) });

            return RedirectToAction("Reports");
        }

        public async Task<IActionResult> Reports()
        {
            var dt = await _db.ExecuteQueryAsync(@"
                SELECT r.*, x.ImagePath, p.FullName as PatientName, p.Age 
                FROM Reports r 
                JOIN XRays x ON r.XRayID = x.XRayID 
                JOIN Patients p ON x.PatientID = p.PatientID
                ORDER BY r.GeneratedDate DESC");
            
            return View(dt);
        }
    }
}
