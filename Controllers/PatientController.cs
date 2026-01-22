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
            var dt = await _db.ExecuteQueryAsync("SELECT * FROM Patients ORDER BY CreatedAt DESC");
            List<Patient> patients = new List<Patient>();
            foreach (DataRow row in dt.Rows)
            {
                patients.Add(new Patient
                {
                    PatientId = Convert.ToInt32(row["PatientID"]),
                    FullName = row["FullName"].ToString(),
                    Age = Convert.ToInt32(row["Age"]),
                    Gender = row["Gender"].ToString(),
                    ContactNumber = row["ContactNumber"].ToString(),
                    Address = row["Address"].ToString(),
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
                SqlParameter[] pars = {
                    new SqlParameter("@Name", patient.FullName),
                    new SqlParameter("@Age", patient.Age),
                    new SqlParameter("@Gender", patient.Gender),
                    new SqlParameter("@Contact", patient.ContactNumber),
                    new SqlParameter("@Address", patient.Address)
                };
                await _db.ExecuteNonQueryAsync("INSERT INTO Patients (FullName, Age, Gender, ContactNumber, Address) VALUES (@Name, @Age, @Gender, @Contact, @Address)", pars);
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }
    }
}
