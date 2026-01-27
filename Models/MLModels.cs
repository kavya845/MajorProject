namespace XRayDiagnosticSystem.Models
{
    public class MLPrediction
    {
        public string? Label { get; set; }
        public float Probability { get; set; }
        public string? Severity { get; set; }
        public string? Anatomy { get; set; } // Detected anatomy focus
    }

    public class ScanImageData
    {
        public string ImagePath { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
