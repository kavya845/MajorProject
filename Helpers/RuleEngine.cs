using System.Data;
using XRayDiagnosticSystem.Data;
using XRayDiagnosticSystem.Models;

namespace XRayDiagnosticSystem.Helpers
{
    public class RuleEngine
    {
        private readonly DatabaseHelper _db;

        public RuleEngine(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<List<DiagnosisRule>> AnalyzeAsync(List<string> observations, string? bodyPart = null)
        {
            string query = "SELECT * FROM DiagnosisRules";
            if (!string.IsNullOrEmpty(bodyPart)) query += " WHERE BodyPart = @type OR BodyPart IS NULL";
            
            var dt = await _db.ExecuteQueryAsync(query, bodyPart != null ? new Microsoft.Data.SqlClient.SqlParameter[] { new Microsoft.Data.SqlClient.SqlParameter("@type", bodyPart) } : null);
            var allRules = new List<DiagnosisRule>();
            foreach (DataRow row in dt.Rows)
            {
                allRules.Add(new DiagnosisRule
                {
                    RuleId = Convert.ToInt32(row["RuleID"]),
                    ConditionName = row["ConditionName"]?.ToString() ?? "Unknown",
                    RequiredTags = row["RequiredTags"]?.ToString() ?? "",
                    Description = row["Description"]?.ToString() ?? "",
                    Severity = row["Severity"] != DBNull.Value ? (row["Severity"]?.ToString() ?? "Moderate") : "Moderate",
                    BaseConfidence = row["BaseConfidence"] != DBNull.Value ? Convert.ToInt32(row["BaseConfidence"]) : 80
                });
            }

            var matchingRules = new List<DiagnosisRule>();
            foreach (var rule in allRules)
            {
                var requiredTags = rule.RequiredTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(t => t.Trim().ToLower());
                
                // If all required tags for a rule are present in the observations
                if (requiredTags.All(rt => observations.Any(o => o.ToLower() == rt)))
                {
                    matchingRules.Add(rule);
                }
            }

            return matchingRules;
        }
    }
}
