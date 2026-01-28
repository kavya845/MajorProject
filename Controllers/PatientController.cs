using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using XRayDiagnosticSystem.Data;
using XRayDiagnosticSystem.Models;

namespace XRayDiagnosticSystem.Controllers
{
    public class PatientController : Controller
    {
        private readonly DatabaseHelper _db;

        public PatientController(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var dt = await _db.ExecuteQueryAsync("SELECT PatientID, FullName, Age, Gender, ContactNumber, Address, CreatedAt FROM hospital.Patients ORDER BY CreatedAt DESC");
            List<Patient> patients = new List<Patient>();
            foreach (DataRow row in dt.Rows)
            {
                patients.Add(new Patient
                {
                    PatientId = Convert.ToInt32(row["PatientID"]),
                    FullName = row["FullName"]?.ToString() ?? "",
                    Age = Convert.ToInt32(row["Age"]),
                    Gender = row["Gender"]?.ToString(),
                    ContactNumber = row["ContactNumber"]?.ToString(),
                    Address = row["Address"]?.ToString(),
                    CreatedAt = Convert.ToDateTime(row["CreatedAt"])
                });
            }
            return View(patients);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Patient patient)
        {
            if (ModelState.IsValid)
            {
                var outId = new SqlParameter("@NewID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                SqlParameter[] pars = {
                    new SqlParameter("@FullName", patient.FullName),
                    new SqlParameter("@Age", patient.Age),
                    new SqlParameter("@Gender", patient.Gender),
                    new SqlParameter("@ContactNumber", patient.ContactNumber),
                    new SqlParameter("@Address", patient.Address),
                    outId
                };
                
                await _db.ExecuteNonQueryAsync("hospital.sp_CreatePatient", pars, CommandType.StoredProcedure);
                
                int newId = (int)outId.Value;
                await LogAudit("Create", "Patients", newId, $"Created patient {patient.FullName}");

                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dt = await _db.ExecuteQueryAsync("SELECT PatientID, FullName, Age, Gender, ContactNumber, Address, CreatedAt FROM hospital.Patients WHERE PatientID = @id", new SqlParameter[] { new SqlParameter("@id", id) });
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var patient = new Patient
            {
                PatientId = Convert.ToInt32(row["PatientID"]),
                FullName = row["FullName"]?.ToString() ?? "",
                Age = Convert.ToInt32(row["Age"]),
                Gender = row["Gender"]?.ToString(),
                ContactNumber = row["ContactNumber"]?.ToString(),
                Address = row["Address"]?.ToString(),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"])
            };
            return View(patient);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Patient patient)
        {
            if (ModelState.IsValid)
            {
                SqlParameter[] pars = {
                    new SqlParameter("@PatientID", patient.PatientId),
                    new SqlParameter("@FullName", patient.FullName),
                    new SqlParameter("@Age", patient.Age),
                    new SqlParameter("@Gender", patient.Gender),
                    new SqlParameter("@ContactNumber", patient.ContactNumber),
                    new SqlParameter("@Address", patient.Address)
                };
                
                await _db.ExecuteNonQueryAsync("hospital.sp_UpdatePatient", pars, CommandType.StoredProcedure);
                await LogAudit("Update", "Patients", patient.PatientId, $"Updated details for {patient.FullName}");

                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _db.ExecuteNonQueryAsync("hospital.sp_DeletePatient", new SqlParameter[] { new SqlParameter("@PatientID", id) }, CommandType.StoredProcedure);
            await LogAudit("Delete", "Patients", id, "Deleted patient record");
            return RedirectToAction(nameof(Index));
        }

        private async Task LogAudit(string action, string table, int recordId, string details)
        {
            string sql = "INSERT INTO hospital.AuditLogs (Action, TableName, RecordID, Details, UserRole) VALUES (@Act, @Tab, @Rec, @Det, 'Admin')";
            SqlParameter[] pars = {
                new SqlParameter("@Act", action),
                new SqlParameter("@Tab", table),
                new SqlParameter("@Rec", recordId),
                new SqlParameter("@Det", details)
            };
            await _db.ExecuteNonQueryAsync(sql, pars);
        }
    }
}
