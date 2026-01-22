namespace XRayDiagnosticSystem.Models
{
    public class DiagnosisRule
    {
        public int RuleId { get; set; }
        public string ConditionName { get; set; }
        public string RequiredTags { get; set; } // Comma separated tags
        public string Description { get; set; }
        public int BaseConfidence { get; set; }
        public string Severity { get; set; }
        public string BodyPart { get; set; }
    }
}
