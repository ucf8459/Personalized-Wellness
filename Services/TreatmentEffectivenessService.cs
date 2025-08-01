using Microsoft.EntityFrameworkCore;
using WellnessPlatform.Data;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services
{
    public class TreatmentEffectivenessService
    {
        private readonly WellnessContext _context;

        public TreatmentEffectivenessService(WellnessContext context)
        {
            _context = context;
        }

        public async Task<List<TreatmentOutcome>> GetTreatmentOutcomesAsync(string userId)
        {
            var healthProfile = await _context.HealthProfiles
                .Include(h => h.Treatments)
                    .ThenInclude(ut => ut.Treatment)
                .Include(h => h.BiomarkerResults)
                .Include(h => h.PromisResults)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (healthProfile?.Treatments == null)
                return new List<TreatmentOutcome>();

            var outcomes = new List<TreatmentOutcome>();

            foreach (var userTreatment in healthProfile.Treatments)
            {
                var treatment = userTreatment.Treatment;
                var biomarkerOutcomes = await AnalyzeBiomarkerOutcomesAsync(healthProfile.Id, userTreatment.StartDate, userTreatment.EndDate);
                var promisOutcomes = await AnalyzePromisOutcomesAsync(healthProfile.Id, userTreatment.StartDate, userTreatment.EndDate);
                var sideEffectAnalysis = await AnalyzeSideEffectsAsync(userTreatment);
                var adherenceScore = CalculateAdherenceScore(userTreatment);

                outcomes.Add(new TreatmentOutcome
                {
                    TreatmentId = treatment.Id,
                    TreatmentName = treatment.Name,
                    TreatmentCategory = treatment.Category,
                    StartDate = userTreatment.StartDate,
                    EndDate = userTreatment.EndDate,
                    Dosage = userTreatment.Dosage,
                    Frequency = userTreatment.Frequency,
                    ResponseRating = userTreatment.ResponseRating,
                    ProviderSupervised = userTreatment.ProviderSupervised,
                    BiomarkerOutcomes = biomarkerOutcomes,
                    PromisOutcomes = promisOutcomes,
                    SideEffectAnalysis = sideEffectAnalysis,
                    AdherenceScore = adherenceScore,
                    OverallEffectiveness = CalculateOverallEffectiveness(biomarkerOutcomes, promisOutcomes, sideEffectAnalysis, adherenceScore, userTreatment.ResponseRating)
                });
            }

            return outcomes.OrderByDescending(o => o.OverallEffectiveness).ToList();
        }

        public async Task<TreatmentTimeline> GetTreatmentTimelineAsync(string userId, int treatmentId)
        {
            var userTreatment = await _context.UserTreatments
                .Include(ut => ut.Treatment)
                .FirstOrDefaultAsync(ut => ut.Id == treatmentId && ut.HealthProfile.UserId == userId);

            if (userTreatment == null)
                return new TreatmentTimeline();

            var timeline = new TreatmentTimeline
            {
                TreatmentName = userTreatment.Treatment.Name,
                StartDate = userTreatment.StartDate,
                EndDate = userTreatment.EndDate,
                Dosage = userTreatment.Dosage,
                Frequency = userTreatment.Frequency
            };

            // Get biomarker measurements during treatment period
            var biomarkerData = await _context.BiomarkerResults
                .Where(b => b.HealthProfileId == userTreatment.HealthProfileId &&
                           b.TestDate >= userTreatment.StartDate &&
                           (userTreatment.EndDate == null || b.TestDate <= userTreatment.EndDate))
                .OrderBy(b => b.TestDate)
                .ToListAsync();

            // Get PROMIS assessments during treatment period
            var promisData = await _context.PromisResults
                .Where(p => p.HealthProfileId == userTreatment.HealthProfileId &&
                           p.AssessmentDate >= userTreatment.StartDate &&
                           (userTreatment.EndDate == null || p.AssessmentDate <= userTreatment.EndDate))
                .OrderBy(p => p.AssessmentDate)
                .ToListAsync();

            timeline.BiomarkerTimeline = CreateBiomarkerTimeline(biomarkerData);
            timeline.PromisTimeline = CreatePromisTimeline(promisData);
            timeline.EffectivenessTrends = CalculateEffectivenessTrends(timeline.BiomarkerTimeline, timeline.PromisTimeline);

            return timeline;
        }

        public async Task<List<TreatmentComparison>> CompareTreatmentsAsync(string userId)
        {
            var outcomes = await GetTreatmentOutcomesAsync(userId);
            var comparisons = new List<TreatmentComparison>();

            for (int i = 0; i < outcomes.Count; i++)
            {
                for (int j = i + 1; j < outcomes.Count; j++)
                {
                    var comparison = new TreatmentComparison
                    {
                        Treatment1 = outcomes[i].TreatmentName,
                        Treatment2 = outcomes[j].TreatmentName,
                        EffectivenessDifference = outcomes[i].OverallEffectiveness - outcomes[j].OverallEffectiveness,
                        BiomarkerImprovement1 = outcomes[i].BiomarkerOutcomes.Average(o => o.ImprovementPercentage),
                        BiomarkerImprovement2 = outcomes[j].BiomarkerOutcomes.Average(o => o.ImprovementPercentage),
                        PromisImprovement1 = outcomes[i].PromisOutcomes.Average(o => o.TScoreChange),
                        PromisImprovement2 = outcomes[j].PromisOutcomes.Average(o => o.TScoreChange),
                        SideEffectSeverity1 = outcomes[i].SideEffectAnalysis.AverageSeverity,
                        SideEffectSeverity2 = outcomes[j].SideEffectAnalysis.AverageSeverity,
                        AdherenceDifference = outcomes[i].AdherenceScore - outcomes[j].AdherenceScore
                    };

                    comparison.Recommendation = GenerateComparisonRecommendation(comparison);
                    comparisons.Add(comparison);
                }
            }

            return comparisons.OrderByDescending(c => Math.Abs(c.EffectivenessDifference)).ToList();
        }

        public async Task<PersonalizedTreatmentRecommendation> GetPersonalizedRecommendationsAsync(string userId)
        {
            var outcomes = await GetTreatmentOutcomesAsync(userId);
            var healthProfile = await _context.HealthProfiles
                .Include(h => h.BiomarkerResults)
                .Include(h => h.PromisResults)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (healthProfile == null)
                return new PersonalizedTreatmentRecommendation();

            var currentBiomarkers = healthProfile.BiomarkerResults
                .GroupBy(b => b.BiomarkerName)
                .Select(g => g.OrderByDescending(b => b.TestDate).First())
                .ToList();

            var currentPromis = healthProfile.PromisResults
                .GroupBy(p => p.Domain)
                .Select(g => g.OrderByDescending(p => p.AssessmentDate).First())
                .ToList();

            var recommendations = new List<TreatmentSuggestion>();

            // Analyze which treatments worked best for similar health profiles
            foreach (var outcome in outcomes.Where(o => o.OverallEffectiveness >= 70))
            {
                var treatment = await _context.Treatments.FindAsync(outcome.TreatmentId);
                var suggestion = new TreatmentSuggestion
                {
                    TreatmentName = treatment.Name,
                    TreatmentCategory = treatment.Category,
                    EvidenceLevel = treatment.EvidenceLevel,
                    SafetyRating = treatment.SafetyRating,
                    RecommendedDosage = treatment.TypicalDosage,
                    ExpectedEffectiveness = outcome.OverallEffectiveness,
                    ExpectedBiomarkerImprovements = outcome.BiomarkerOutcomes.Count(o => o.ImprovementPercentage > 0),
                    ExpectedPromisImprovements = outcome.PromisOutcomes.Count(o => o.TScoreChange > 0),
                    MonitoringRequired = treatment.MonitoringRequired == "Yes",
                    CommonSideEffects = treatment.CommonSideEffects,
                    Contraindications = treatment.Contraindications
                };

                recommendations.Add(suggestion);
            }

            return new PersonalizedTreatmentRecommendation
            {
                CurrentHealthStatus = AnalyzeCurrentHealthStatus(currentBiomarkers, currentPromis),
                RecommendedTreatments = recommendations.OrderByDescending(r => r.ExpectedEffectiveness).ToList(),
                HealthGoals = GenerateHealthGoals(currentBiomarkers, currentPromis),
                MonitoringPlan = GenerateMonitoringPlan(recommendations)
            };
        }

        private async Task<List<BiomarkerOutcome>> AnalyzeBiomarkerOutcomesAsync(int healthProfileId, DateTime startDate, DateTime? endDate)
        {
            var outcomes = new List<BiomarkerOutcome>();

            var biomarkers = await _context.BiomarkerResults
                .Where(b => b.HealthProfileId == healthProfileId)
                .GroupBy(b => b.BiomarkerName)
                .ToListAsync();

            foreach (var biomarkerGroup in biomarkers)
            {
                var beforeTreatment = biomarkerGroup
                    .Where(b => b.TestDate < startDate)
                    .OrderByDescending(b => b.TestDate)
                    .FirstOrDefault();

                var afterTreatment = biomarkerGroup
                    .Where(b => b.TestDate >= startDate && (endDate == null || b.TestDate <= endDate))
                    .OrderByDescending(b => b.TestDate)
                    .FirstOrDefault();

                if (beforeTreatment != null && afterTreatment != null)
                {
                    var improvement = ((double)(afterTreatment.Value - beforeTreatment.Value) / (double)beforeTreatment.Value) * 100;
                    var statusChange = (int)afterTreatment.Status - (int)beforeTreatment.Status;

                    outcomes.Add(new BiomarkerOutcome
                    {
                        BiomarkerName = biomarkerGroup.Key,
                        BeforeValue = beforeTreatment.Value,
                        AfterValue = afterTreatment.Value,
                        ImprovementPercentage = improvement,
                        StatusChange = statusChange,
                        Units = beforeTreatment.Units,
                        FinalStatus = afterTreatment.Status,
                        IsImprovement = improvement > 0 && statusChange >= 0
                    });
                }
            }

            return outcomes;
        }

        private async Task<List<PromisOutcome>> AnalyzePromisOutcomesAsync(int healthProfileId, DateTime startDate, DateTime? endDate)
        {
            var outcomes = new List<PromisOutcome>();

            var promisResults = await _context.PromisResults
                .Where(p => p.HealthProfileId == healthProfileId)
                .GroupBy(p => p.Domain)
                .ToListAsync();

            foreach (var domainGroup in promisResults)
            {
                var beforeTreatment = domainGroup
                    .Where(p => p.AssessmentDate < startDate)
                    .OrderByDescending(p => p.AssessmentDate)
                    .FirstOrDefault();

                var afterTreatment = domainGroup
                    .Where(p => p.AssessmentDate >= startDate && (endDate == null || p.AssessmentDate <= endDate))
                    .OrderByDescending(p => p.AssessmentDate)
                    .FirstOrDefault();

                if (beforeTreatment != null && afterTreatment != null)
                {
                    var tScoreChange = (double)(afterTreatment.TScore - beforeTreatment.TScore);
                    var percentileChange = (double)((afterTreatment.PercentileRank ?? 0) - (beforeTreatment.PercentileRank ?? 0));

                    outcomes.Add(new PromisOutcome
                    {
                        Domain = domainGroup.Key,
                        BeforeTScore = beforeTreatment.TScore,
                        AfterTScore = afterTreatment.TScore,
                        TScoreChange = tScoreChange,
                        BeforePercentile = beforeTreatment.PercentileRank ?? 0,
                        AfterPercentile = afterTreatment.PercentileRank ?? 0,
                        PercentileChange = percentileChange,
                        SeverityChange = 0, // SeverityLevel is string, cannot subtract
                        IsImprovement = tScoreChange > 0
                    });
                }
            }

            return outcomes;
        }

        private async Task<SideEffectAnalysis> AnalyzeSideEffectsAsync(UserTreatment userTreatment)
        {
            var sideEffects = userTreatment.SideEffectsNoted?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
            
            return new SideEffectAnalysis
            {
                SideEffects = sideEffects.ToList(),
                SideEffectCount = sideEffects.Length,
                AverageSeverity = sideEffects.Length > 0 ? CalculateSideEffectSeverity(sideEffects) : 0,
                MostCommonSideEffects = GetMostCommonSideEffects(sideEffects),
                SeverityLevel = DetermineSeverityLevel(sideEffects.Length, CalculateSideEffectSeverity(sideEffects))
            };
        }

        private double CalculateAdherenceScore(UserTreatment userTreatment)
        {
            if (userTreatment.EndDate.HasValue)
            {
                var expectedDuration = (userTreatment.EndDate.Value - userTreatment.StartDate).TotalDays;
                var actualDuration = (DateTime.UtcNow - userTreatment.StartDate).TotalDays;
                return Math.Min(100, (actualDuration / expectedDuration) * 100);
            }
            return 100; // Ongoing treatment
        }

        private double CalculateOverallEffectiveness(List<BiomarkerOutcome> biomarkerOutcomes, List<PromisOutcome> promisOutcomes, SideEffectAnalysis sideEffectAnalysis, double adherenceScore, int? responseRating)
        {
            var score = 0.0;

            // Biomarker improvements (40% weight)
            if (biomarkerOutcomes.Any())
            {
                var biomarkerScore = biomarkerOutcomes.Count(o => o.IsImprovement) / (double)biomarkerOutcomes.Count * 40;
                score += biomarkerScore;
            }

            // PROMIS improvements (30% weight)
            if (promisOutcomes.Any())
            {
                var promisScore = promisOutcomes.Count(o => o.IsImprovement) / (double)promisOutcomes.Count * 30;
                score += promisScore;
            }

            // Side effects (20% weight)
            var sideEffectScore = (100 - sideEffectAnalysis.AverageSeverity) / 100.0 * 20;
            score += sideEffectScore;

            // Adherence (10% weight)
            score += adherenceScore / 100.0 * 10;

            // User rating bonus
            if (responseRating.HasValue)
            {
                score += responseRating.Value * 2; // 1-5 rating * 2 = 2-10 bonus points
            }

            return Math.Min(100, score);
        }

        private List<BiomarkerTimelinePoint> CreateBiomarkerTimeline(List<BiomarkerResult> biomarkerData)
        {
            return biomarkerData.Select(b => new BiomarkerTimelinePoint
            {
                Date = b.TestDate,
                BiomarkerName = b.BiomarkerName,
                Value = b.Value,
                Status = b.Status,
                Units = b.Units
            }).ToList();
        }

        private List<PromisTimelinePoint> CreatePromisTimeline(List<PromisResult> promisData)
        {
            return promisData.Select(p => new PromisTimelinePoint
            {
                                        Date = p.AssessmentDate,
                        Domain = p.Domain,
                        TScore = p.TScore,
                        PercentileRank = p.PercentileRank ?? 0,
                SeverityLevel = 0 // SeverityLevel is string, using default
            }).ToList();
        }

        private List<EffectivenessTrend> CalculateEffectivenessTrends(List<BiomarkerTimelinePoint> biomarkerTimeline, List<PromisTimelinePoint> promisTimeline)
        {
            var trends = new List<EffectivenessTrend>();

            // Calculate biomarker trends
            var biomarkerGroups = biomarkerTimeline.GroupBy(b => b.BiomarkerName);
            foreach (var group in biomarkerGroups)
            {
                var orderedData = group.OrderBy(b => b.Date).ToList();
                if (orderedData.Count >= 2)
                {
                    var trend = CalculateLinearTrend(orderedData.Select(b => (double)b.Value).ToList());
                    trends.Add(new EffectivenessTrend
                    {
                        MetricName = group.Key,
                        MetricType = "Biomarker",
                        TrendDirection = trend > 0 ? "Improving" : "Declining",
                        TrendStrength = Math.Abs(trend),
                        DataPoints = orderedData.Count
                    });
                }
            }

            // Calculate PROMIS trends
            var promisGroups = promisTimeline.GroupBy(p => p.Domain);
            foreach (var group in promisGroups)
            {
                var orderedData = group.OrderBy(p => p.Date).ToList();
                if (orderedData.Count >= 2)
                {
                    var trend = CalculateLinearTrend(orderedData.Select(p => (double)p.TScore).ToList());
                    trends.Add(new EffectivenessTrend
                    {
                        MetricName = group.Key,
                        MetricType = "PROMIS",
                        TrendDirection = trend > 0 ? "Improving" : "Declining",
                        TrendStrength = Math.Abs(trend),
                        DataPoints = orderedData.Count
                    });
                }
            }

            return trends;
        }

        private double CalculateLinearTrend(List<double> values)
        {
            if (values.Count < 2) return 0;

            var n = values.Count;
            var xValues = Enumerable.Range(0, n).Select(x => (double)x).ToList();
            var sumX = xValues.Sum();
            var sumY = values.Sum();
            var sumXY = xValues.Zip(values, (x, y) => x * y).Sum();
            var sumX2 = xValues.Sum(x => x * x);

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope;
        }

        private double CalculateSideEffectSeverity(string[] sideEffects)
        {
            var severityKeywords = new Dictionary<string, int>
            {
                { "mild", 1 }, { "minor", 1 }, { "slight", 1 },
                { "moderate", 2 }, { "medium", 2 },
                { "severe", 3 }, { "serious", 3 }, { "significant", 3 }
            };

            var totalSeverity = 0;
            foreach (var effect in sideEffects)
            {
                var severity = severityKeywords.FirstOrDefault(kvp => effect.ToLower().Contains(kvp.Key)).Value;
                totalSeverity += severity;
            }

            return sideEffects.Length > 0 ? (double)totalSeverity / sideEffects.Length : 0;
        }

        private List<string> GetMostCommonSideEffects(string[] sideEffects)
        {
            return sideEffects
                .GroupBy(s => s.ToLower().Trim())
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();
        }

        private string DetermineSeverityLevel(int sideEffectCount, double averageSeverity)
        {
            if (sideEffectCount == 0) return "None";
            if (averageSeverity <= 1) return "Mild";
            if (averageSeverity <= 2) return "Moderate";
            return "Severe";
        }

        private string GenerateComparisonRecommendation(TreatmentComparison comparison)
        {
            if (comparison.EffectivenessDifference > 20)
                return $"{comparison.Treatment1} is significantly more effective than {comparison.Treatment2}";
            else if (comparison.EffectivenessDifference < -20)
                return $"{comparison.Treatment2} is significantly more effective than {comparison.Treatment1}";
            else
                return "Both treatments show similar effectiveness";
        }

        private string AnalyzeCurrentHealthStatus(List<BiomarkerResult> biomarkers, List<PromisResult> promisResults)
        {
            var optimalBiomarkers = biomarkers.Count(b => b.Status == BiomarkerStatus.Optimal);
            var totalBiomarkers = biomarkers.Count;
            var optimalPercentage = totalBiomarkers > 0 ? (double)optimalBiomarkers / totalBiomarkers * 100 : 0;

            if (optimalPercentage >= 80) return "Excellent";
            if (optimalPercentage >= 60) return "Good";
            if (optimalPercentage >= 40) return "Fair";
            return "Needs Improvement";
        }

        private List<string> GenerateHealthGoals(List<BiomarkerResult> biomarkers, List<PromisResult> promisResults)
        {
            var goals = new List<string>();

            var suboptimalBiomarkers = biomarkers.Where(b => b.Status != BiomarkerStatus.Optimal).ToList();
            foreach (var biomarker in suboptimalBiomarkers.Take(3))
            {
                goals.Add($"Improve {biomarker.BiomarkerName} to optimal range");
            }

            var lowPromisScores = promisResults.Where(p => p.TScore < 45).ToList();
            foreach (var promis in lowPromisScores.Take(2))
            {
                goals.Add($"Improve {promis.Domain} PROMIS score");
            }

            return goals;
        }

        private string GenerateMonitoringPlan(List<TreatmentSuggestion> recommendations)
        {
            var monitoringRequirements = recommendations
                .Where(r => r.MonitoringRequired)
                .Select(r => r.TreatmentName)
                .ToList();

            if (monitoringRequirements.Any())
            {
                return $"Monitor: {string.Join(", ", monitoringRequirements)}";
            }

            return "Standard monitoring recommended";
        }
    }

    public class TreatmentOutcome
    {
        public int TreatmentId { get; set; }
        public string TreatmentName { get; set; } = string.Empty;
        public TreatmentCategory TreatmentCategory { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public int? ResponseRating { get; set; }
        public bool ProviderSupervised { get; set; }
        public List<BiomarkerOutcome> BiomarkerOutcomes { get; set; } = new();
        public List<PromisOutcome> PromisOutcomes { get; set; } = new();
        public SideEffectAnalysis SideEffectAnalysis { get; set; } = new();
        public double AdherenceScore { get; set; }
        public double OverallEffectiveness { get; set; }

        public string EffectivenessColor => OverallEffectiveness switch
        {
            >= 80 => "success",
            >= 60 => "warning",
            _ => "danger"
        };

        public string EffectivenessLevel => OverallEffectiveness switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            _ => "Poor"
        };
    }

    public class BiomarkerOutcome
    {
        public string BiomarkerName { get; set; } = string.Empty;
        public decimal BeforeValue { get; set; }
        public decimal AfterValue { get; set; }
        public double ImprovementPercentage { get; set; }
        public int StatusChange { get; set; }
        public string Units { get; set; } = string.Empty;
        public BiomarkerStatus FinalStatus { get; set; }
        public bool IsImprovement { get; set; }

        public string ChangeColor => IsImprovement ? "success" : "danger";
    }

    public class PromisOutcome
    {
        public string Domain { get; set; } = string.Empty;
        public decimal BeforeTScore { get; set; }
        public decimal AfterTScore { get; set; }
        public double TScoreChange { get; set; }
        public decimal BeforePercentile { get; set; }
        public decimal AfterPercentile { get; set; }
        public double PercentileChange { get; set; }
        public int SeverityChange { get; set; }
        public bool IsImprovement { get; set; }

        public string ChangeColor => IsImprovement ? "success" : "danger";
    }

    public class SideEffectAnalysis
    {
        public List<string> SideEffects { get; set; } = new();
        public int SideEffectCount { get; set; }
        public double AverageSeverity { get; set; }
        public List<string> MostCommonSideEffects { get; set; } = new();
        public string SeverityLevel { get; set; } = string.Empty;

        public string SeverityColor => SeverityLevel switch
        {
            "None" => "success",
            "Mild" => "warning",
            "Moderate" => "warning",
            "Severe" => "danger",
            _ => "secondary"
        };
    }

    public class TreatmentTimeline
    {
        public string TreatmentName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public List<BiomarkerTimelinePoint> BiomarkerTimeline { get; set; } = new();
        public List<PromisTimelinePoint> PromisTimeline { get; set; } = new();
        public List<EffectivenessTrend> EffectivenessTrends { get; set; } = new();
    }

    public class BiomarkerTimelinePoint
    {
        public DateTime Date { get; set; }
        public string BiomarkerName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public BiomarkerStatus Status { get; set; }
        public string Units { get; set; } = string.Empty;
    }

    public class PromisTimelinePoint
    {
        public DateTime Date { get; set; }
        public string Domain { get; set; } = string.Empty;
        public decimal TScore { get; set; }
        public decimal PercentileRank { get; set; }
        public int SeverityLevel { get; set; }
    }

    public class EffectivenessTrend
    {
        public string MetricName { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
        public string TrendDirection { get; set; } = string.Empty;
        public double TrendStrength { get; set; }
        public int DataPoints { get; set; }

        public string TrendColor => TrendDirection switch
        {
            "Improving" => "success",
            "Declining" => "danger",
            _ => "secondary"
        };
    }

    public class TreatmentComparison
    {
        public string Treatment1 { get; set; } = string.Empty;
        public string Treatment2 { get; set; } = string.Empty;
        public double EffectivenessDifference { get; set; }
        public double BiomarkerImprovement1 { get; set; }
        public double BiomarkerImprovement2 { get; set; }
        public double PromisImprovement1 { get; set; }
        public double PromisImprovement2 { get; set; }
        public double SideEffectSeverity1 { get; set; }
        public double SideEffectSeverity2 { get; set; }
        public double AdherenceDifference { get; set; }
        public string Recommendation { get; set; } = string.Empty;

        public string ComparisonColor => EffectivenessDifference switch
        {
            > 0 => "success",
            < 0 => "danger",
            _ => "secondary"
        };
    }

    public class PersonalizedTreatmentRecommendation
    {
        public string CurrentHealthStatus { get; set; } = string.Empty;
        public List<TreatmentSuggestion> RecommendedTreatments { get; set; } = new();
        public List<string> HealthGoals { get; set; } = new();
        public string MonitoringPlan { get; set; } = string.Empty;
    }

    public class TreatmentSuggestion
    {
        public string TreatmentName { get; set; } = string.Empty;
        public TreatmentCategory TreatmentCategory { get; set; }
        public EvidenceLevel EvidenceLevel { get; set; }
        public int SafetyRating { get; set; }
        public string RecommendedDosage { get; set; } = string.Empty;
        public double ExpectedEffectiveness { get; set; }
        public int ExpectedBiomarkerImprovements { get; set; }
        public int ExpectedPromisImprovements { get; set; }
        public bool MonitoringRequired { get; set; }
        public string CommonSideEffects { get; set; } = string.Empty;
        public string Contraindications { get; set; } = string.Empty;

        public string EffectivenessColor => ExpectedEffectiveness switch
        {
            >= 80 => "success",
            >= 60 => "warning",
            _ => "danger"
        };
    }
} 