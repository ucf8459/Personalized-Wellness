using WellnessPlatform.Data;
using WellnessPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace WellnessPlatform.Services
{
    public class TreatmentRecommendationService
    {
        private readonly WellnessContext _context;

        public TreatmentRecommendationService(WellnessContext context)
        {
            _context = context;
        }

        public async Task<List<TreatmentRecommendation>> GetRecommendationsAsync(string userId)
        {
            var healthProfile = await _context.HealthProfiles
                .Include(h => h.BiomarkerResults)
                .Include(h => h.PromisResults)
                .Include(h => h.Treatments)
                    .ThenInclude(ut => ut.Treatment)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (healthProfile == null)
                return new List<TreatmentRecommendation>();

            var recommendations = new List<TreatmentRecommendation>();

            // Analyze biomarkers and generate recommendations
            var biomarkerIssues = AnalyzeBiomarkerIssues(healthProfile.BiomarkerResults);
            var promisIssues = AnalyzePromisIssues(healthProfile.PromisResults);
            var currentTreatments = healthProfile.Treatments.Where(ut => ut.EndDate == null).ToList();

            // Get all available treatments
            var allTreatments = await _context.Treatments.ToListAsync();

            // Generate biomarker-based recommendations
            foreach (var issue in biomarkerIssues)
            {
                var matchingTreatments = FindTreatmentsForBiomarkerIssue(issue, allTreatments, currentTreatments);
                recommendations.AddRange(matchingTreatments);
            }

            // Generate PROMIS-based recommendations
            foreach (var issue in promisIssues)
            {
                var matchingTreatments = FindTreatmentsForPromisIssue(issue, allTreatments, currentTreatments);
                recommendations.AddRange(matchingTreatments);
            }

            // Remove duplicates and sort by priority
            return recommendations
                .GroupBy(r => r.Treatment.Id)
                .Select(g => g.OrderByDescending(r => r.PriorityScore).First())
                .OrderByDescending(r => r.PriorityScore)
                .ToList();
        }

        private List<BiomarkerIssue> AnalyzeBiomarkerIssues(List<BiomarkerResult> biomarkers)
        {
            var issues = new List<BiomarkerIssue>();
            var latestBiomarkers = biomarkers
                .GroupBy(b => b.BiomarkerName)
                .Select(g => g.OrderByDescending(b => b.TestDate).First())
                .ToList();

            foreach (var biomarker in latestBiomarkers)
            {
                if (biomarker.Status != BiomarkerStatus.Optimal)
                {
                    var severity = CalculateBiomarkerSeverity(biomarker);
                    var trend = AnalyzeBiomarkerTrend(biomarkers.Where(b => b.BiomarkerName == biomarker.BiomarkerName).ToList());

                    issues.Add(new BiomarkerIssue
                    {
                        BiomarkerName = biomarker.BiomarkerName,
                        CurrentValue = biomarker.Value,
                        OptimalRange = $"{biomarker.OptimalRangeMin}-{biomarker.OptimalRangeMax}",
                        Status = biomarker.Status,
                        Severity = severity,
                        Trend = trend,
                        Units = biomarker.Units
                    });
                }
            }

            return issues;
        }

        private List<PromisIssue> AnalyzePromisIssues(List<PromisResult> promisResults)
        {
            var issues = new List<PromisIssue>();
            var latestPromis = promisResults
                .GroupBy(p => p.Domain)
                .Select(g => g.OrderByDescending(p => p.AssessmentDate).First())
                .ToList();

            foreach (var promis in latestPromis)
            {
                if (promis.TScore < 45) // Below average
                {
                    issues.Add(new PromisIssue
                    {
                        Domain = promis.Domain,
                        TScore = promis.TScore,
                        Severity = promis.TScore < 35 ? "Severe" : promis.TScore < 40 ? "Moderate" : "Mild",
                        PercentileRank = (int)(promis.PercentileRank ?? 0)
                    });
                }
            }

            return issues;
        }

        private int CalculateBiomarkerSeverity(BiomarkerResult biomarker)
        {
            if (biomarker.Status == BiomarkerStatus.Optimal) return 0;

            var deviation = biomarker.Status == BiomarkerStatus.High 
                ? (biomarker.Value - biomarker.OptimalRangeMax!.Value) / biomarker.OptimalRangeMax.Value
                : (biomarker.OptimalRangeMin!.Value - biomarker.Value) / biomarker.OptimalRangeMin.Value;

            return Math.Max(1, Math.Min(5, (int)(Math.Abs((double)deviation) * 10)));
        }

        private string AnalyzeBiomarkerTrend(List<BiomarkerResult> biomarkerHistory)
        {
            if (biomarkerHistory.Count < 2) return "Stable";

            var ordered = biomarkerHistory.OrderBy(b => b.TestDate).ToList();
            var latest = ordered.Last();
            var previous = ordered[ordered.Count - 2];

            var change = ((latest.Value - previous.Value) / previous.Value) * 100m;
            return Math.Abs(change) < 5 ? "Stable" : change > 0 ? "Improving" : "Declining";
        }

        private List<TreatmentRecommendation> FindTreatmentsForBiomarkerIssue(
            BiomarkerIssue issue, 
            List<Treatment> allTreatments, 
            List<UserTreatment> currentTreatments)
        {
            var recommendations = new List<TreatmentRecommendation>();

            // Define treatment-biomarker mappings
            var treatmentMappings = new Dictionary<string, List<string>>
            {
                { "Vitamin D", new List<string> { "Vitamin D3" } },
                { "C-Reactive Protein", new List<string> { "Omega-3 EPA/DHA", "NAD+ Precursor (NMN/NR)" } },
                { "Total Cholesterol", new List<string> { "Omega-3 EPA/DHA" } },
                { "LDL Cholesterol", new List<string> { "Omega-3 EPA/DHA" } },
                { "HbA1c", new List<string> { "NAD+ Precursor (NMN/NR)" } },
                { "TSH", new List<string> { "BPC-157" } }
            };

            if (treatmentMappings.ContainsKey(issue.BiomarkerName))
            {
                foreach (var treatmentName in treatmentMappings[issue.BiomarkerName])
                {
                    var treatment = allTreatments.FirstOrDefault(t => t.Name == treatmentName);
                    if (treatment != null && !currentTreatments.Any(ut => ut.TreatmentId == treatment.Id))
                    {
                        var recommendation = CreateRecommendation(treatment, issue, "biomarker");
                        recommendations.Add(recommendation);
                    }
                }
            }

            return recommendations;
        }

        private List<TreatmentRecommendation> FindTreatmentsForPromisIssue(
            PromisIssue issue, 
            List<Treatment> allTreatments, 
            List<UserTreatment> currentTreatments)
        {
            var recommendations = new List<TreatmentRecommendation>();

            // Define treatment-PROMIS domain mappings
            var treatmentMappings = new Dictionary<string, List<string>>
            {
                { "Physical Function", new List<string> { "BPC-157", "NAD+ Precursor (NMN/NR)" } },
                { "Fatigue", new List<string> { "NAD+ Precursor (NMN/NR)", "Vitamin D3" } },
                { "Depression", new List<string> { "Omega-3 EPA/DHA", "NAD+ Precursor (NMN/NR)" } },
                { "Anxiety", new List<string> { "Omega-3 EPA/DHA" } }
            };

            if (treatmentMappings.ContainsKey(issue.Domain))
            {
                foreach (var treatmentName in treatmentMappings[issue.Domain])
                {
                    var treatment = allTreatments.FirstOrDefault(t => t.Name == treatmentName);
                    if (treatment != null && !currentTreatments.Any(ut => ut.TreatmentId == treatment.Id))
                    {
                        var recommendation = CreateRecommendation(treatment, issue, "promis");
                        recommendations.Add(recommendation);
                    }
                }
            }

            return recommendations;
        }

        private TreatmentRecommendation CreateRecommendation(Treatment treatment, object issue, string issueType)
        {
            var priorityScore = CalculatePriorityScore(treatment, issue, issueType);
            var reasoning = GenerateReasoning(treatment, issue, issueType);

            return new TreatmentRecommendation
            {
                Treatment = treatment,
                PriorityScore = priorityScore,
                Reasoning = reasoning,
                IssueType = issueType,
                EvidenceLevel = treatment.EvidenceLevel,
                SafetyRating = treatment.SafetyRating
            };
        }

        private int CalculatePriorityScore(Treatment treatment, object issue, string issueType)
        {
            var baseScore = 0;

            // Evidence level scoring (higher evidence = higher score)
            baseScore += (6 - (int)treatment.EvidenceLevel) * 10;

            // Safety rating scoring (higher safety = higher score)
            baseScore += treatment.SafetyRating * 5;

            // Issue severity scoring
            if (issueType == "biomarker" && issue is BiomarkerIssue biomarkerIssue)
            {
                baseScore += biomarkerIssue.Severity * 3;
                if (biomarkerIssue.Trend == "Declining") baseScore += 5;
            }
            else if (issueType == "promis" && issue is PromisIssue promisIssue)
            {
                baseScore += promisIssue.Severity == "Severe" ? 15 : promisIssue.Severity == "Moderate" ? 10 : 5;
            }

            // Regulatory status bonus
            if (treatment.RegulatoryStatus == RegulatoryStatus.FDAApproved)
                baseScore += 10;

            return baseScore;
        }

        private string GenerateReasoning(Treatment treatment, object issue, string issueType)
        {
            var reasoning = new List<string>();

            if (issueType == "biomarker" && issue is BiomarkerIssue biomarkerIssue)
            {
                reasoning.Add($"Targets {biomarkerIssue.BiomarkerName} ({biomarkerIssue.CurrentValue} {biomarkerIssue.Units})");
                reasoning.Add($"Optimal range: {biomarkerIssue.OptimalRange} {biomarkerIssue.Units}");
                reasoning.Add($"Current status: {biomarkerIssue.Status} (Trend: {biomarkerIssue.Trend})");
            }
            else if (issueType == "promis" && issue is PromisIssue promisIssue)
            {
                reasoning.Add($"Addresses {promisIssue.Domain} concerns");
                reasoning.Add($"T-score: {promisIssue.TScore} (Percentile: {promisIssue.PercentileRank}%)");
                reasoning.Add($"Severity: {promisIssue.Severity}");
            }

            reasoning.Add($"Evidence: {treatment.EvidenceBadge}");
            reasoning.Add($"Safety: {treatment.SafetyRating}/5");
            reasoning.Add($"Cost: {treatment.CostRange}");

            return string.Join(" | ", reasoning);
        }
    }

    public class TreatmentRecommendation
    {
        public Treatment Treatment { get; set; } = null!;
        public int PriorityScore { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public EvidenceLevel EvidenceLevel { get; set; }
        public int SafetyRating { get; set; }
    }

    public class BiomarkerIssue
    {
        public string BiomarkerName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public string OptimalRange { get; set; } = string.Empty;
        public BiomarkerStatus Status { get; set; }
        public int Severity { get; set; }
        public string Trend { get; set; } = string.Empty;
        public string Units { get; set; } = string.Empty;
    }

    public class PromisIssue
    {
        public string Domain { get; set; } = string.Empty;
        public decimal TScore { get; set; }
        public string Severity { get; set; } = string.Empty;
        public int PercentileRank { get; set; }
    }
} 