using Microsoft.EntityFrameworkCore;
using WellnessPlatform.Data;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services
{
    public class CorrelationAnalysisService
    {
        private readonly WellnessContext _context;

        public CorrelationAnalysisService(WellnessContext context)
        {
            _context = context;
        }

        public async Task<List<BiomarkerCorrelation>> GetBiomarkerCorrelationsAsync(string userId)
        {
            var healthProfile = await _context.HealthProfiles
                .Include(h => h.BiomarkerResults)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (healthProfile?.BiomarkerResults == null || !healthProfile.BiomarkerResults.Any())
                return new List<BiomarkerCorrelation>();

            var correlations = new List<BiomarkerCorrelation>();
            var biomarkers = healthProfile.BiomarkerResults
                .GroupBy(b => b.BiomarkerName)
                .Select(g => g.OrderByDescending(b => b.TestDate).First())
                .ToList();

            // Calculate correlations between all biomarker pairs
            for (int i = 0; i < biomarkers.Count; i++)
            {
                for (int j = i + 1; j < biomarkers.Count; j++)
                {
                    var correlation = await CalculateBiomarkerCorrelationAsync(
                        healthProfile.Id, 
                        biomarkers[i].BiomarkerName, 
                        biomarkers[j].BiomarkerName);

                    if (correlation != null)
                    {
                        correlations.Add(correlation);
                    }
                }
            }

            return correlations.OrderByDescending(c => Math.Abs(c.CorrelationCoefficient)).ToList();
        }

        public async Task<List<TreatmentEffectiveness>> GetTreatmentEffectivenessAsync(string userId)
        {
            var healthProfile = await _context.HealthProfiles
                .Include(h => h.Treatments)
                    .ThenInclude(ut => ut.Treatment)
                .Include(h => h.BiomarkerResults)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (healthProfile?.Treatments == null)
                return new List<TreatmentEffectiveness>();

            var effectiveness = new List<TreatmentEffectiveness>();

            foreach (var userTreatment in healthProfile.Treatments.Where(ut => ut.EndDate == null))
            {
                var treatment = userTreatment.Treatment;
                var biomarkerChanges = await CalculateBiomarkerChangesAsync(
                    healthProfile.Id, 
                    userTreatment.StartDate);

                var promisChanges = await CalculatePromisChangesAsync(
                    healthProfile.Id, 
                    userTreatment.StartDate);

                effectiveness.Add(new TreatmentEffectiveness
                {
                    TreatmentName = treatment.Name,
                    TreatmentCategory = treatment.Category,
                    StartDate = userTreatment.StartDate,
                    Dosage = userTreatment.Dosage,
                    ResponseRating = userTreatment.ResponseRating,
                    BiomarkerChanges = biomarkerChanges,
                    PromisChanges = promisChanges,
                    EffectivenessScore = CalculateEffectivenessScore(biomarkerChanges, promisChanges, userTreatment.ResponseRating)
                });
            }

            return effectiveness.OrderByDescending(e => e.EffectivenessScore).ToList();
        }

        public async Task<List<HealthTrend>> GetHealthTrendsAsync(string userId, int months = 6)
        {
            var healthProfile = await _context.HealthProfiles
                .Include(h => h.BiomarkerResults)
                .Include(h => h.PromisResults)
                .Include(h => h.LifestyleMetrics)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (healthProfile == null)
                return new List<HealthTrend>();

            var cutoffDate = DateTime.UtcNow.AddMonths(-months);
            var trends = new List<HealthTrend>();

            // Biomarker trends
            var biomarkerTrends = await GetBiomarkerTrendsAsync(healthProfile.Id, cutoffDate);
            trends.AddRange(biomarkerTrends);

            // PROMIS trends
            var promisTrends = await GetPromisTrendsAsync(healthProfile.Id, cutoffDate);
            trends.AddRange(promisTrends);

            // Lifestyle trends
            var lifestyleTrends = await GetLifestyleTrendsAsync(healthProfile.Id, cutoffDate);
            trends.AddRange(lifestyleTrends);

            return trends.OrderByDescending(t => t.Significance).ToList();
        }

        private async Task<BiomarkerCorrelation?> CalculateBiomarkerCorrelationAsync(int healthProfileId, string biomarker1, string biomarker2)
        {
            var data1 = await _context.BiomarkerResults
                .Where(b => b.HealthProfileId == healthProfileId && b.BiomarkerName == biomarker1)
                .OrderBy(b => b.TestDate)
                .ToListAsync();

            var data2 = await _context.BiomarkerResults
                .Where(b => b.HealthProfileId == healthProfileId && b.BiomarkerName == biomarker2)
                .OrderBy(b => b.TestDate)
                .ToListAsync();

            if (data1.Count < 2 || data2.Count < 2)
                return null;

            // Find overlapping dates or use closest dates
            var correlationData = new List<(decimal value1, decimal value2)>();

            foreach (var point1 in data1)
            {
                var closestPoint2 = data2
                    .OrderBy(p2 => Math.Abs((p2.TestDate - point1.TestDate).TotalDays))
                    .FirstOrDefault();

                if (closestPoint2 != null && Math.Abs((closestPoint2.TestDate - point1.TestDate).TotalDays) <= 30)
                {
                    correlationData.Add((point1.Value, closestPoint2.Value));
                }
            }

            if (correlationData.Count < 3)
                return null;

            var correlation = CalculatePearsonCorrelation(correlationData);

            return new BiomarkerCorrelation
            {
                Biomarker1 = biomarker1,
                Biomarker2 = biomarker2,
                CorrelationCoefficient = correlation,
                DataPoints = correlationData.Count,
                Significance = Math.Abs(correlation) > 0.7 ? "Strong" : Math.Abs(correlation) > 0.4 ? "Moderate" : "Weak",
                Trend = correlation > 0 ? "Positive" : "Negative"
            };
        }

        private async Task<List<BiomarkerChange>> CalculateBiomarkerChangesAsync(int healthProfileId, DateTime treatmentStartDate)
        {
            var changes = new List<BiomarkerChange>();

            var biomarkers = await _context.BiomarkerResults
                .Where(b => b.HealthProfileId == healthProfileId)
                .GroupBy(b => b.BiomarkerName)
                .ToListAsync();

            foreach (var biomarkerGroup in biomarkers)
            {
                var beforeTreatment = biomarkerGroup
                    .Where(b => b.TestDate < treatmentStartDate)
                    .OrderByDescending(b => b.TestDate)
                    .FirstOrDefault();

                var afterTreatment = biomarkerGroup
                    .Where(b => b.TestDate >= treatmentStartDate)
                    .OrderByDescending(b => b.TestDate)
                    .FirstOrDefault();

                if (beforeTreatment != null && afterTreatment != null)
                {
                    var change = ((double)(afterTreatment.Value - beforeTreatment.Value) / (double)beforeTreatment.Value) * 100;
                    changes.Add(new BiomarkerChange
                    {
                        BiomarkerName = biomarkerGroup.Key,
                        BeforeValue = beforeTreatment.Value,
                        AfterValue = afterTreatment.Value,
                        ChangePercentage = change,
                        Units = beforeTreatment.Units,
                        Status = afterTreatment.Status
                    });
                }
            }

            return changes;
        }

        private async Task<List<PromisChange>> CalculatePromisChangesAsync(int healthProfileId, DateTime treatmentStartDate)
        {
            var changes = new List<PromisChange>();

            var promisResults = await _context.PromisResults
                .Where(p => p.HealthProfileId == healthProfileId)
                .GroupBy(p => p.Domain)
                .ToListAsync();

            foreach (var domainGroup in promisResults)
            {
                var beforeTreatment = domainGroup
                    .Where(p => p.AssessmentDate < treatmentStartDate)
                    .OrderByDescending(p => p.AssessmentDate)
                    .FirstOrDefault();

                var afterTreatment = domainGroup
                    .Where(p => p.AssessmentDate >= treatmentStartDate)
                    .OrderByDescending(p => p.AssessmentDate)
                    .FirstOrDefault();

                if (beforeTreatment != null && afterTreatment != null)
                {
                    var change = (double)(afterTreatment.TScore - beforeTreatment.TScore);
                    changes.Add(new PromisChange
                    {
                        Domain = domainGroup.Key,
                        BeforeTScore = beforeTreatment.TScore,
                        AfterTScore = afterTreatment.TScore,
                        Change = change,
                        Improvement = change > 0
                    });
                }
            }

            return changes;
        }

        private double CalculateEffectivenessScore(List<BiomarkerChange> biomarkerChanges, List<PromisChange> promisChanges, int? responseRating)
        {
            var score = 0.0;

            // Biomarker improvements
            var biomarkerImprovements = biomarkerChanges.Count(c => c.ChangePercentage > 0);
            var totalBiomarkers = biomarkerChanges.Count;
            if (totalBiomarkers > 0)
            {
                score += (double)biomarkerImprovements / totalBiomarkers * 40;
            }

            // PROMIS improvements
            var promisImprovements = promisChanges.Count(c => c.Improvement);
            var totalPromis = promisChanges.Count;
            if (totalPromis > 0)
            {
                score += (double)promisImprovements / totalPromis * 40;
            }

            // User rating
            if (responseRating.HasValue)
            {
                score += responseRating.Value * 4; // 1-5 rating * 4 = 4-20 points
            }

            return Math.Min(100, score);
        }

        private async Task<List<HealthTrend>> GetBiomarkerTrendsAsync(int healthProfileId, DateTime cutoffDate)
        {
            var trends = new List<HealthTrend>();

            var biomarkers = await _context.BiomarkerResults
                .Where(b => b.HealthProfileId == healthProfileId && b.TestDate >= cutoffDate)
                .GroupBy(b => b.BiomarkerName)
                .ToListAsync();

            foreach (var biomarkerGroup in biomarkers)
            {
                var orderedData = biomarkerGroup.OrderBy(b => b.TestDate).ToList();
                if (orderedData.Count >= 2)
                {
                    var trend = CalculateLinearTrend(orderedData.Select(b => (double)b.Value).ToList());
                    trends.Add(new HealthTrend
                    {
                        MetricName = biomarkerGroup.Key,
                        MetricType = "Biomarker",
                        TrendDirection = trend > 0 ? "Improving" : "Declining",
                        TrendStrength = Math.Abs(trend),
                        Significance = Math.Abs(trend) > 0.1 ? "Significant" : "Stable",
                        DataPoints = orderedData.Count,
                        LastValue = orderedData.Last().Value,
                        Units = orderedData.Last().Units
                    });
                }
            }

            return trends;
        }

        private async Task<List<HealthTrend>> GetPromisTrendsAsync(int healthProfileId, DateTime cutoffDate)
        {
            var trends = new List<HealthTrend>();

            var promisResults = await _context.PromisResults
                .Where(p => p.HealthProfileId == healthProfileId && p.AssessmentDate >= cutoffDate)
                .GroupBy(p => p.Domain)
                .ToListAsync();

            foreach (var domainGroup in promisResults)
            {
                var orderedData = domainGroup.OrderBy(p => p.AssessmentDate).ToList();
                if (orderedData.Count >= 2)
                {
                    var trend = CalculateLinearTrend(orderedData.Select(p => (double)p.TScore).ToList());
                    trends.Add(new HealthTrend
                    {
                        MetricName = domainGroup.Key,
                        MetricType = "PROMIS",
                        TrendDirection = trend > 0 ? "Improving" : "Declining",
                        TrendStrength = Math.Abs(trend),
                        Significance = Math.Abs(trend) > 2 ? "Significant" : "Stable",
                        DataPoints = orderedData.Count,
                        LastValue = orderedData.Last().TScore
                    });
                }
            }

            return trends;
        }

        private async Task<List<HealthTrend>> GetLifestyleTrendsAsync(int healthProfileId, DateTime cutoffDate)
        {
            var trends = new List<HealthTrend>();

            var lifestyleMetrics = await _context.LifestyleMetrics
                .Where(l => l.HealthProfileId == healthProfileId && l.RecordDate >= cutoffDate)
                .OrderBy(l => l.RecordDate)
                .ToListAsync();

            if (lifestyleMetrics.Count >= 2)
            {
                // Calculate trends for different lifestyle metrics
                var metrics = new[] { "SleepHours", "ExerciseMinutes", "StressLevel", "EnergyLevel", "MoodRating" };

                foreach (var metric in metrics)
                {
                    var values = GetLifestyleMetricValues(lifestyleMetrics, metric);
                    if (values.Count >= 2)
                    {
                        var trend = CalculateLinearTrend(values);
                        trends.Add(new HealthTrend
                        {
                            MetricName = metric.Replace("Level", "").Replace("Rating", ""),
                            MetricType = "Lifestyle",
                            TrendDirection = trend > 0 ? "Improving" : "Declining",
                            TrendStrength = Math.Abs(trend),
                            Significance = Math.Abs(trend) > 0.5 ? "Significant" : "Stable",
                            DataPoints = values.Count,
                            LastValue = (decimal)values.Last()
                        });
                    }
                }
            }

            return trends;
        }

        private List<double> GetLifestyleMetricValues(List<LifestyleMetric> metrics, string propertyName)
        {
            var values = new List<double>();
            foreach (var metric in metrics)
            {
                var value = propertyName switch
                {
                    "SleepHours" => metric.SleepHours,
                    "ExerciseMinutes" => metric.ExerciseMinutes,
                    "StressLevel" => metric.StressLevel,
                    "EnergyLevel" => metric.EnergyLevel,
                    "MoodRating" => metric.MoodRating,
                    _ => null
                };

                if (value.HasValue)
                {
                    values.Add((double)value.Value);
                }
            }
            return values;
        }

        private double CalculatePearsonCorrelation(List<(decimal value1, decimal value2)> data)
        {
            if (data.Count < 2) return 0;

            var n = data.Count;
            var sumX = data.Sum(d => (double)d.value1);
            var sumY = data.Sum(d => (double)d.value2);
            var sumXY = data.Sum(d => (double)d.value1 * (double)d.value2);
            var sumX2 = data.Sum(d => Math.Pow((double)d.value1, 2));
            var sumY2 = data.Sum(d => Math.Pow((double)d.value2, 2));

            var numerator = n * sumXY - sumX * sumY;
            var denominator = Math.Sqrt((n * sumX2 - Math.Pow(sumX, 2)) * (n * sumY2 - Math.Pow(sumY, 2)));

            return denominator == 0 ? 0 : numerator / denominator;
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
    }

    public class BiomarkerCorrelation
    {
        public string Biomarker1 { get; set; } = string.Empty;
        public string Biomarker2 { get; set; } = string.Empty;
        public double CorrelationCoefficient { get; set; }
        public int DataPoints { get; set; }
        public string Significance { get; set; } = string.Empty;
        public string Trend { get; set; } = string.Empty;

        public string CorrelationColor => Math.Abs(CorrelationCoefficient) switch
        {
            > 0.7 => "danger",
            > 0.4 => "warning",
            _ => "success"
        };

        public string CorrelationStrength => Math.Abs(CorrelationCoefficient) switch
        {
            > 0.7 => "Strong",
            > 0.4 => "Moderate",
            _ => "Weak"
        };
    }

    public class TreatmentEffectiveness
    {
        public string TreatmentName { get; set; } = string.Empty;
        public TreatmentCategory TreatmentCategory { get; set; }
        public DateTime StartDate { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public int? ResponseRating { get; set; }
        public List<BiomarkerChange> BiomarkerChanges { get; set; } = new();
        public List<PromisChange> PromisChanges { get; set; } = new();
        public double EffectivenessScore { get; set; }

        public string EffectivenessColor => EffectivenessScore switch
        {
            >= 80 => "success",
            >= 60 => "warning",
            _ => "danger"
        };

        public string EffectivenessLevel => EffectivenessScore switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            _ => "Poor"
        };
    }

    public class BiomarkerChange
    {
        public string BiomarkerName { get; set; } = string.Empty;
        public decimal BeforeValue { get; set; }
        public decimal AfterValue { get; set; }
        public double ChangePercentage { get; set; }
        public string Units { get; set; } = string.Empty;
        public BiomarkerStatus Status { get; set; }

        public string ChangeColor => ChangePercentage switch
        {
            > 0 => "success",
            < 0 => "danger",
            _ => "secondary"
        };
    }

    public class PromisChange
    {
        public string Domain { get; set; } = string.Empty;
        public decimal BeforeTScore { get; set; }
        public decimal AfterTScore { get; set; }
        public double Change { get; set; }
        public bool Improvement { get; set; }

        public string ChangeColor => Improvement ? "success" : "danger";
    }

    public class HealthTrend
    {
        public string MetricName { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
        public string TrendDirection { get; set; } = string.Empty;
        public double TrendStrength { get; set; }
        public string Significance { get; set; } = string.Empty;
        public int DataPoints { get; set; }
        public decimal LastValue { get; set; }
        public string Units { get; set; } = string.Empty;

        public string TrendColor => TrendDirection switch
        {
            "Improving" => "success",
            "Declining" => "danger",
            _ => "secondary"
        };

        public string TrendIcon => TrendDirection switch
        {
            "Improving" => "bi-arrow-up-circle",
            "Declining" => "bi-arrow-down-circle",
            _ => "bi-dash-circle"
        };
    }
} 