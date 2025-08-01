using Microsoft.EntityFrameworkCore;
using WellnessPlatform.Data;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services
{
    public class BiomarkerStatusService
    {
        private readonly WellnessContext _context;

        public BiomarkerStatusService(WellnessContext context)
        {
            _context = context;
        }

        public async Task<List<BiomarkerStatusIndicator>> GetStatusIndicatorsAsync(string userId)
        {
            var healthProfile = await _context.HealthProfiles
                .Include(h => h.BiomarkerResults)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (healthProfile?.BiomarkerResults == null)
                return new List<BiomarkerStatusIndicator>();

            var indicators = new List<BiomarkerStatusIndicator>();
            var latestBiomarkers = healthProfile.BiomarkerResults
                .GroupBy(b => b.BiomarkerName)
                .Select(g => g.OrderByDescending(b => b.TestDate).First())
                .ToList();

            foreach (var biomarker in latestBiomarkers)
            {
                var trend = await CalculateTrendAsync(healthProfile.Id, biomarker.BiomarkerName);
                var severity = CalculateSeverity(biomarker);
                var alertLevel = DetermineAlertLevel(biomarker, trend);

                indicators.Add(new BiomarkerStatusIndicator
                {
                    BiomarkerName = biomarker.BiomarkerName,
                    CurrentValue = biomarker.Value,
                    Status = biomarker.Status,
                    Trend = trend,
                    Severity = severity,
                    AlertLevel = alertLevel,
                    LastUpdated = biomarker.TestDate,
                    Units = biomarker.Units,
                    OptimalRange = $"{biomarker.OptimalRangeMin}-{biomarker.OptimalRangeMax}",
                    ReferenceRange = $"{biomarker.ReferenceRangeMin}-{biomarker.ReferenceRangeMax}"
                });
            }

            return indicators.OrderByDescending(i => i.AlertLevel).ToList();
        }

        public async Task<List<BiomarkerAlert>> GetActiveAlertsAsync(string userId)
        {
            var indicators = await GetStatusIndicatorsAsync(userId);
            var alerts = new List<BiomarkerAlert>();

            foreach (var indicator in indicators)
            {
                if (indicator.AlertLevel >= AlertLevel.Warning)
                {
                    alerts.Add(new BiomarkerAlert
                    {
                        BiomarkerName = indicator.BiomarkerName,
                        AlertLevel = indicator.AlertLevel,
                        Message = GenerateAlertMessage(indicator),
                        Timestamp = indicator.LastUpdated,
                        IsActive = true
                    });
                }
            }

            return alerts.OrderByDescending(a => a.AlertLevel).ToList();
        }

        public async Task<BiomarkerSummary> GetBiomarkerSummaryAsync(string userId)
        {
            var indicators = await GetStatusIndicatorsAsync(userId);
            
            return new BiomarkerSummary
            {
                TotalBiomarkers = indicators.Count,
                OptimalCount = indicators.Count(i => i.Status == BiomarkerStatus.Optimal),
                WarningCount = indicators.Count(i => i.AlertLevel == AlertLevel.Warning),
                CriticalCount = indicators.Count(i => i.AlertLevel == AlertLevel.Critical),
                ImprovingCount = indicators.Count(i => i.Trend == TrendDirection.Improving),
                DecliningCount = indicators.Count(i => i.Trend == TrendDirection.Declining),
                LastUpdated = indicators.Any() ? indicators.Max(i => i.LastUpdated) : DateTime.UtcNow
            };
        }

        private async Task<TrendDirection> CalculateTrendAsync(int healthProfileId, string biomarkerName)
        {
            var history = await _context.BiomarkerResults
                .Where(b => b.HealthProfileId == healthProfileId && b.BiomarkerName == biomarkerName)
                .OrderByDescending(b => b.TestDate)
                .Take(3)
                .ToListAsync();

            if (history.Count < 2) return TrendDirection.Stable;

            var recent = history[0];
            var previous = history[1];

            var change = ((recent.Value - previous.Value) / previous.Value) * 100;

            if (Math.Abs(change) < 5) return TrendDirection.Stable;
            return change > 0 ? TrendDirection.Improving : TrendDirection.Declining;
        }

        private int CalculateSeverity(BiomarkerResult biomarker)
        {
            if (biomarker.Status == BiomarkerStatus.Optimal) return 0;

            var deviation = biomarker.Status == BiomarkerStatus.High
                ? (biomarker.Value - biomarker.OptimalRangeMax!.Value) / biomarker.OptimalRangeMax.Value
                : (biomarker.OptimalRangeMin!.Value - biomarker.Value) / biomarker.OptimalRangeMin.Value;

            return Math.Max(1, Math.Min(5, (int)(Math.Abs(deviation) * 10)));
        }

        private AlertLevel DetermineAlertLevel(BiomarkerResult biomarker, TrendDirection trend)
        {
            // Critical biomarkers that need immediate attention
            var criticalBiomarkers = new[] { "C-Reactive Protein", "HbA1c", "Vitamin D" };
            
            if (criticalBiomarkers.Contains(biomarker.BiomarkerName))
            {
                if (biomarker.Status == BiomarkerStatus.High || biomarker.Status == BiomarkerStatus.Low)
                    return AlertLevel.Critical;
                if (biomarker.Status == BiomarkerStatus.Normal)
                    return AlertLevel.Warning;
            }

            // Non-critical biomarkers
            if (biomarker.Status == BiomarkerStatus.High || biomarker.Status == BiomarkerStatus.Low)
                return AlertLevel.Warning;

            if (biomarker.Status == BiomarkerStatus.Normal && trend == TrendDirection.Declining)
                return AlertLevel.Info;

            return AlertLevel.Normal;
        }

        private string GenerateAlertMessage(BiomarkerStatusIndicator indicator)
        {
            return indicator.AlertLevel switch
            {
                AlertLevel.Critical => $"CRITICAL: {indicator.BiomarkerName} is {indicator.Status} ({indicator.CurrentValue} {indicator.Units})",
                AlertLevel.Warning => $"WARNING: {indicator.BiomarkerName} is {indicator.Status} ({indicator.CurrentValue} {indicator.Units})",
                AlertLevel.Info => $"INFO: {indicator.BiomarkerName} is {indicator.Status} but trending {indicator.Trend}",
                _ => $"Normal: {indicator.BiomarkerName} is {indicator.Status}"
            };
        }
    }

    public class BiomarkerStatusIndicator
    {
        public string BiomarkerName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public BiomarkerStatus Status { get; set; }
        public TrendDirection Trend { get; set; }
        public int Severity { get; set; }
        public AlertLevel AlertLevel { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Units { get; set; } = string.Empty;
        public string OptimalRange { get; set; } = string.Empty;
        public string ReferenceRange { get; set; } = string.Empty;

        public string StatusColor => Status switch
        {
            BiomarkerStatus.Optimal => "success",
            BiomarkerStatus.Normal => "info",
            BiomarkerStatus.High => "warning",
            BiomarkerStatus.Low => "danger",
            _ => "secondary"
        };

        public string TrendIcon => Trend switch
        {
            TrendDirection.Improving => "bi-arrow-up-circle text-success",
            TrendDirection.Declining => "bi-arrow-down-circle text-danger",
            _ => "bi-dash-circle text-secondary"
        };

        public string AlertIcon => AlertLevel switch
        {
            AlertLevel.Critical => "bi-exclamation-triangle-fill text-danger",
            AlertLevel.Warning => "bi-exclamation-circle text-warning",
            AlertLevel.Info => "bi-info-circle text-info",
            _ => "bi-check-circle text-success"
        };
    }

    public class BiomarkerAlert
    {
        public string BiomarkerName { get; set; } = string.Empty;
        public AlertLevel AlertLevel { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsActive { get; set; }
    }

    public class BiomarkerSummary
    {
        public int TotalBiomarkers { get; set; }
        public int OptimalCount { get; set; }
        public int WarningCount { get; set; }
        public int CriticalCount { get; set; }
        public int ImprovingCount { get; set; }
        public int DecliningCount { get; set; }
        public DateTime LastUpdated { get; set; }

        public double OptimalPercentage => TotalBiomarkers > 0 ? (double)OptimalCount / TotalBiomarkers * 100 : 0;
        public double WarningPercentage => TotalBiomarkers > 0 ? (double)WarningCount / TotalBiomarkers * 100 : 0;
        public double CriticalPercentage => TotalBiomarkers > 0 ? (double)CriticalCount / TotalBiomarkers * 100 : 0;
    }

    public enum TrendDirection
    {
        Improving,
        Declining,
        Stable
    }

    public enum AlertLevel
    {
        Normal = 0,
        Info = 1,
        Warning = 2,
        Critical = 3
    }
} 