namespace XRayDiagnosticSystem.Models
{
    public class DiagnosisRule
    {
        public int RuleId { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public string RequiredTags { get; set; } = string.Empty; // Comma separated tags
        public string? Description { get; set; }
        public int BaseConfidence { get; set; }
        public string Severity { get; set; } = "Normal";
        public string BodyPart { get; set; } = string.Empty;
    }
}
