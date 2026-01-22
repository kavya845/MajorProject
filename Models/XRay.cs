namespace XRayDiagnosticSystem.Models
{
    public class XRay
    {
        public int XRayId { get; set; }
        public int PatientId { get; set; }
        public string ImagePath { get; set; }
        public DateTime UploadDate { get; set; }
        public string TechnicianNotes { get; set; }
        public string Status { get; set; }
        public string BodyPart { get; set; } // Hand, Chest
        // Navigation property for easier UI display
        public string PatientName { get; set; }
    }
}
