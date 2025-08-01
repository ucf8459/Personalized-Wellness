using System.ComponentModel.DataAnnotations;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services
{
    public class DataValidationService
    {
        public ValidationResult ValidateBiomarkerResult(BiomarkerResult biomarker)
        {
            var errors = new List<ValidationResult>();

            // Required field validation
            if (string.IsNullOrWhiteSpace(biomarker.BiomarkerName))
                errors.Add(new ValidationResult("Biomarker name is required"));

            if (string.IsNullOrWhiteSpace(biomarker.Units))
                errors.Add(new ValidationResult("Units are required"));

            // Date validation
            if (biomarker.TestDate > DateTime.UtcNow)
                errors.Add(new ValidationResult("Test date cannot be in the future"));

            if (biomarker.TestDate < DateTime.UtcNow.AddYears(-10))
                errors.Add(new ValidationResult("Test date cannot be more than 10 years ago"));

            // Value validation
            if (biomarker.Value <= 0)
                errors.Add(new ValidationResult("Biomarker value must be greater than 0"));

            if (biomarker.Value > 10000)
                errors.Add(new ValidationResult("Biomarker value seems unusually high"));

            // Range validation
            if (biomarker.ReferenceRangeMin.HasValue && biomarker.ReferenceRangeMax.HasValue)
            {
                if (biomarker.ReferenceRangeMin >= biomarker.ReferenceRangeMax)
                    errors.Add(new ValidationResult("Reference range minimum must be less than maximum"));

                if (biomarker.Value < biomarker.ReferenceRangeMin || biomarker.Value > biomarker.ReferenceRangeMax)
                    errors.Add(new ValidationResult($"Value {biomarker.Value} is outside reference range {biomarker.ReferenceRangeMin}-{biomarker.ReferenceRangeMax}"));
            }

            if (biomarker.OptimalRangeMin.HasValue && biomarker.OptimalRangeMax.HasValue)
            {
                if (biomarker.OptimalRangeMin >= biomarker.OptimalRangeMax)
                    errors.Add(new ValidationResult("Optimal range minimum must be less than maximum"));

                if (biomarker.ReferenceRangeMin.HasValue && biomarker.ReferenceRangeMax.HasValue)
                {
                    if (biomarker.OptimalRangeMin < biomarker.ReferenceRangeMin || biomarker.OptimalRangeMax > biomarker.ReferenceRangeMax)
                        errors.Add(new ValidationResult("Optimal range must be within reference range"));
                }
            }

            // Biomarker-specific validation
            ValidateBiomarkerSpecific(biomarker, errors);

            return errors.Any() ? new ValidationResult(string.Join("; ", errors.Select(e => e.ErrorMessage))) : ValidationResult.Success;
        }

        public ValidationResult ValidatePromisResult(PromisResult promis)
        {
            var errors = new List<ValidationResult>();

            // Required field validation
            if (string.IsNullOrWhiteSpace(promis.Domain))
                errors.Add(new ValidationResult("PROMIS domain is required"));

            // Date validation
            if (promis.AssessmentDate > DateTime.UtcNow)
                errors.Add(new ValidationResult("Assessment date cannot be in the future"));

            if (promis.AssessmentDate < DateTime.UtcNow.AddYears(-5))
                errors.Add(new ValidationResult("Assessment date cannot be more than 5 years ago"));

            // T-Score validation (PROMIS T-scores typically range from 20-80)
            if (promis.TScore < 20 || promis.TScore > 80)
                errors.Add(new ValidationResult("T-score must be between 20 and 80"));

            // Percentile validation
            if (promis.PercentileRank.HasValue && (promis.PercentileRank < 0 || promis.PercentileRank > 100))
                errors.Add(new ValidationResult("Percentile rank must be between 0 and 100"));

            // Items answered validation
            if (promis.ItemsAnswered <= 0)
                errors.Add(new ValidationResult("At least one item must be answered"));

            if (promis.ItemsAnswered > 50)
                errors.Add(new ValidationResult("Items answered cannot exceed 50"));

            // Domain-specific validation
            ValidatePromisDomain(promis, errors);

            return errors.Any() ? new ValidationResult(string.Join("; ", errors.Select(e => e.ErrorMessage))) : ValidationResult.Success;
        }

        public ValidationResult ValidateTreatment(Treatment treatment)
        {
            var errors = new List<ValidationResult>();

            // Required field validation
            if (string.IsNullOrWhiteSpace(treatment.Name))
                errors.Add(new ValidationResult("Treatment name is required"));

            if (string.IsNullOrWhiteSpace(treatment.Mechanism))
                errors.Add(new ValidationResult("Treatment mechanism is required"));

            // Safety rating validation
            if (treatment.SafetyRating < 1 || treatment.SafetyRating > 5)
                errors.Add(new ValidationResult("Safety rating must be between 1 and 5"));

            // Cost range validation
            if (!string.IsNullOrWhiteSpace(treatment.CostRange) && !treatment.CostRange.Contains("$"))
                errors.Add(new ValidationResult("Cost range should include dollar sign"));

            // Dosage validation
            if (!string.IsNullOrWhiteSpace(treatment.TypicalDosage))
            {
                if (!treatment.TypicalDosage.Contains("mg") && !treatment.TypicalDosage.Contains("mcg") && 
                    !treatment.TypicalDosage.Contains("IU") && !treatment.TypicalDosage.Contains("g"))
                    errors.Add(new ValidationResult("Dosage should include appropriate units (mg, mcg, IU, g)"));
            }

            return errors.Any() ? new ValidationResult(string.Join("; ", errors.Select(e => e.ErrorMessage))) : ValidationResult.Success;
        }

        public ValidationResult ValidateUserTreatment(UserTreatment userTreatment)
        {
            var errors = new List<ValidationResult>();

            // Date validation
            if (userTreatment.StartDate > DateTime.UtcNow)
                errors.Add(new ValidationResult("Start date cannot be in the future"));

            if (userTreatment.EndDate.HasValue)
            {
                if (userTreatment.EndDate < userTreatment.StartDate)
                    errors.Add(new ValidationResult("End date cannot be before start date"));

                if (userTreatment.EndDate > DateTime.UtcNow)
                    errors.Add(new ValidationResult("End date cannot be in the future"));
            }

            // Response rating validation
            if (userTreatment.ResponseRating.HasValue && (userTreatment.ResponseRating < 1 || userTreatment.ResponseRating > 5))
                errors.Add(new ValidationResult("Response rating must be between 1 and 5"));

            // Dosage validation
            if (!string.IsNullOrWhiteSpace(userTreatment.Dosage))
            {
                if (!userTreatment.Dosage.Contains("mg") && !userTreatment.Dosage.Contains("mcg") && 
                    !userTreatment.Dosage.Contains("IU") && !userTreatment.Dosage.Contains("g"))
                    errors.Add(new ValidationResult("Dosage should include appropriate units (mg, mcg, IU, g)"));
            }

            return errors.Any() ? new ValidationResult(string.Join("; ", errors.Select(e => e.ErrorMessage))) : ValidationResult.Success;
        }

        public ValidationResult ValidateLifestyleMetric(LifestyleMetric metric)
        {
            var errors = new List<ValidationResult>();

            // Date validation
            if (metric.RecordDate > DateTime.UtcNow)
                errors.Add(new ValidationResult("Date cannot be in the future"));

            if (metric.RecordDate < DateTime.UtcNow.AddYears(-1))
                errors.Add(new ValidationResult("Date cannot be more than 1 year ago"));

            // Metric-specific validation
            ValidateLifestyleMetricSpecific(metric, errors);

            return errors.Any() ? new ValidationResult(string.Join("; ", errors.Select(e => e.ErrorMessage))) : ValidationResult.Success;
        }

        private void ValidateBiomarkerSpecific(BiomarkerResult biomarker, List<ValidationResult> errors)
        {
            switch (biomarker.BiomarkerName.ToLower())
            {
                case "vitamin d":
                    if (biomarker.Value > 200)
                        errors.Add(new ValidationResult("Vitamin D value seems unusually high"));
                    if (biomarker.Units != "ng/mL")
                        errors.Add(new ValidationResult("Vitamin D should be measured in ng/mL"));
                    break;

                case "c-reactive protein":
                    if (biomarker.Value > 50)
                        errors.Add(new ValidationResult("CRP value seems unusually high"));
                    if (biomarker.Units != "mg/L")
                        errors.Add(new ValidationResult("CRP should be measured in mg/L"));
                    break;

                case "hba1c":
                    if (biomarker.Value > 15)
                        errors.Add(new ValidationResult("HbA1c value seems unusually high"));
                    if (biomarker.Units != "%")
                        errors.Add(new ValidationResult("HbA1c should be measured in %"));
                    break;

                case "total cholesterol":
                    if (biomarker.Value > 1000)
                        errors.Add(new ValidationResult("Total cholesterol value seems unusually high"));
                    if (biomarker.Units != "mg/dL")
                        errors.Add(new ValidationResult("Total cholesterol should be measured in mg/dL"));
                    break;
            }
        }

        private void ValidatePromisDomain(PromisResult promis, List<ValidationResult> errors)
        {
            var validDomains = new[] { "Physical Function", "Fatigue", "Depression", "Anxiety", "Pain", "Sleep" };
            
            if (!validDomains.Contains(promis.Domain))
                errors.Add(new ValidationResult($"Invalid PROMIS domain: {promis.Domain}. Valid domains are: {string.Join(", ", validDomains)}"));

            // Domain-specific T-score ranges
            switch (promis.Domain)
            {
                case "Physical Function":
                    if (promis.TScore < 20 || promis.TScore > 60)
                        errors.Add(new ValidationResult("Physical Function T-score should be between 20-60"));
                    break;
                case "Fatigue":
                case "Depression":
                case "Anxiety":
                    if (promis.TScore < 30 || promis.TScore > 80)
                        errors.Add(new ValidationResult($"{promis.Domain} T-score should be between 30-80"));
                    break;
            }
        }

        private void ValidateLifestyleMetricSpecific(LifestyleMetric metric, List<ValidationResult> errors)
        {
            // Sleep validation
            if (metric.SleepHours.HasValue)
            {
                if (metric.SleepHours < 0 || metric.SleepHours > 24)
                    errors.Add(new ValidationResult("Sleep hours must be between 0 and 24"));
            }

            if (metric.SleepQuality.HasValue)
            {
                if (metric.SleepQuality < 1 || metric.SleepQuality > 10)
                    errors.Add(new ValidationResult("Sleep quality must be between 1 and 10"));
            }

            // Exercise validation
            if (metric.ExerciseMinutes.HasValue)
            {
                if (metric.ExerciseMinutes < 0 || metric.ExerciseMinutes > 480) // 8 hours max
                    errors.Add(new ValidationResult("Exercise minutes must be between 0 and 480"));
            }

            if (metric.ExerciseIntensity.HasValue)
            {
                if (metric.ExerciseIntensity < 1 || metric.ExerciseIntensity > 10)
                    errors.Add(new ValidationResult("Exercise intensity must be between 1 and 10"));
            }

            // Stress and mood validation
            if (metric.StressLevel.HasValue)
            {
                if (metric.StressLevel < 1 || metric.StressLevel > 10)
                    errors.Add(new ValidationResult("Stress level must be between 1 and 10"));
            }

            if (metric.EnergyLevel.HasValue)
            {
                if (metric.EnergyLevel < 1 || metric.EnergyLevel > 10)
                    errors.Add(new ValidationResult("Energy level must be between 1 and 10"));
            }

            if (metric.MoodRating.HasValue)
            {
                if (metric.MoodRating < 1 || metric.MoodRating > 10)
                    errors.Add(new ValidationResult("Mood rating must be between 1 and 10"));
            }

            // Weight and body composition validation
            if (metric.Weight.HasValue)
            {
                if (metric.Weight < 50 || metric.Weight > 500) // kg
                    errors.Add(new ValidationResult("Weight must be between 50 and 500 kg"));
            }

            if (metric.BodyFatPercentage.HasValue)
            {
                if (metric.BodyFatPercentage < 0 || metric.BodyFatPercentage > 50)
                    errors.Add(new ValidationResult("Body fat percentage must be between 0 and 50"));
            }
        }

        public List<string> GetValidationSummary(List<ValidationResult> results)
        {
            return results.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
        }
    }
} 