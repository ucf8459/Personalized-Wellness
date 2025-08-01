using Microsoft.AspNetCore.Identity;
using WellnessPlatform.Models;

namespace WellnessPlatform.Data
{
    public class DataSeeder
    {
        public static async Task SeedDataAsync(WellnessContext context, UserManager<IdentityUser> userManager)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            // Check if we already have data
            if (context.HealthProfiles.Any())
                return;
            
            // Create a demo user
            var demoUser = new IdentityUser
            {
                UserName = "demo@wellness.com",
                Email = "demo@wellness.com",
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(demoUser, "Demo123!");
            if (!result.Succeeded)
                return;
            
            // Create health profile for demo user
            var healthProfile = new HealthProfile
            {
                UserId = demoUser.Id,
                DateCreated = DateTime.UtcNow.AddMonths(-6),
                LastUpdated = DateTime.UtcNow
            };
            
            context.HealthProfiles.Add(healthProfile);
            await context.SaveChangesAsync();
            
            // Add sample treatments based on specification
            var treatments = new List<Treatment>
            {
                new Treatment
                {
                    Name = "Vitamin D3",
                    Category = TreatmentCategory.Supplement,
                    EvidenceLevel = EvidenceLevel.SystematicReviews,
                    RegulatoryStatus = RegulatoryStatus.FDAApproved,
                    Mechanism = "Supports calcium absorption, immune function, and bone health",
                    TypicalDosage = "2000-4000 IU daily",
                    MonitoringRequired = "25(OH)D levels every 3-6 months",
                    CommonSideEffects = "Rare at appropriate doses; kidney stones with excessive intake",
                    Contraindications = "Hypercalcemia, sarcoidosis",
                    CostRange = "$10-25/month",
                    SafetyRating = 5
                },
                new Treatment
                {
                    Name = "Omega-3 EPA/DHA",
                    Category = TreatmentCategory.Supplement,
                    EvidenceLevel = EvidenceLevel.SystematicReviews,
                    RegulatoryStatus = RegulatoryStatus.FDAApproved,
                    Mechanism = "Anti-inflammatory effects, supports cardiovascular and brain health",
                    TypicalDosage = "1-3g daily (combined EPA/DHA)",
                    MonitoringRequired = "Lipid panel, bleeding time if on anticoagulants",
                    CommonSideEffects = "Fish taste, GI upset, possible increased bleeding",
                    Contraindications = "Fish allergy, severe bleeding disorders",
                    CostRange = "$20-50/month",
                    SafetyRating = 5
                },
                new Treatment
                {
                    Name = "NAD+ Precursor (NMN/NR)",
                    Category = TreatmentCategory.Supplement,
                    EvidenceLevel = EvidenceLevel.CohortStudies,
                    RegulatoryStatus = RegulatoryStatus.Experimental,
                    Mechanism = "Cellular energy metabolism, potential anti-aging effects",
                    TypicalDosage = "250-500mg daily",
                    MonitoringRequired = "Liver function, metabolic markers",
                    CommonSideEffects = "Flushing, GI upset, possible nausea",
                    Contraindications = "Pregnancy, breastfeeding, cancer history",
                    CostRange = "$50-150/month",
                    SafetyRating = 4
                },
                new Treatment
                {
                    Name = "Rapamycin (Sirolimus)",
                    Category = TreatmentCategory.Medication,
                    EvidenceLevel = EvidenceLevel.CaseReports,
                    RegulatoryStatus = RegulatoryStatus.OffLabel,
                    Mechanism = "mTOR pathway inhibition, potential lifespan extension",
                    TypicalDosage = "1-6mg weekly (longevity protocol)",
                    MonitoringRequired = "CBC, metabolic panel, lipids every 3 months",
                    CommonSideEffects = "Mouth sores, immunosuppression, elevated cholesterol",
                    Contraindications = "Active infection, pregnancy, severe liver disease",
                    CostRange = "$40-80/month",
                    SafetyRating = 2
                },
                new Treatment
                {
                    Name = "BPC-157",
                    Category = TreatmentCategory.Peptide,
                    EvidenceLevel = EvidenceLevel.PreclinicalOnly,
                    RegulatoryStatus = RegulatoryStatus.Experimental,
                    Mechanism = "Tissue repair and healing, gut health support",
                    TypicalDosage = "250-500mcg daily (subcutaneous)",
                    MonitoringRequired = "Clinical assessment, liver function",
                    CommonSideEffects = "Injection site reactions, possible fatigue",
                    Contraindications = "Pregnancy, breastfeeding, active cancer",
                    CostRange = "$100-200/month",
                    SafetyRating = 3
                }
            };
            
            context.Treatments.AddRange(treatments);
            await context.SaveChangesAsync();
            
            // Add sample biomarker data from specification
            var baseDate = DateTime.UtcNow.AddMonths(-6);
            var biomarkers = new List<BiomarkerResult>();
            
            // Generate biomarker data for the last 6 months (3 test dates)
            for (int monthOffset = 0; monthOffset <= 6; monthOffset += 3)
            {
                var testDate = baseDate.AddMonths(monthOffset);
                
                // Cardiovascular markers
                biomarkers.AddRange(new[]
                {
                    new BiomarkerResult
                    {
                        HealthProfileId = healthProfile.Id,
                        TestDate = testDate,
                        BiomarkerName = "Total Cholesterol",
                        Value = 185 + (monthOffset * -2), // Improving over time
                        ReferenceRangeMin = 150,
                        ReferenceRangeMax = 250,
                        OptimalRangeMin = 150,
                        OptimalRangeMax = 200,
                        Units = "mg/dL",
                        Status = BiomarkerStatus.Optimal
                    },
                    new BiomarkerResult
                    {
                        HealthProfileId = healthProfile.Id,
                        TestDate = testDate,
                        BiomarkerName = "LDL Cholesterol",
                        Value = 95 + (monthOffset * -1),
                        ReferenceRangeMin = 0,
                        ReferenceRangeMax = 130,
                        OptimalRangeMin = 0,
                        OptimalRangeMax = 100,
                        Units = "mg/dL",
                        Status = BiomarkerStatus.Optimal
                    },
                    new BiomarkerResult
                    {
                        HealthProfileId = healthProfile.Id,
                        TestDate = testDate,
                        BiomarkerName = "C-Reactive Protein",
                        Value = (decimal)(2.1 - (monthOffset * 0.3)), // Improving with treatment
                        ReferenceRangeMin = 0,
                        ReferenceRangeMax = 3.0m,
                        OptimalRangeMin = 0,
                        OptimalRangeMax = 1.0m,
                        Units = "mg/L",
                        Status = monthOffset >= 3 ? BiomarkerStatus.Optimal : BiomarkerStatus.High
                    },
                    // Metabolic markers
                    new BiomarkerResult
                    {
                        HealthProfileId = healthProfile.Id,
                        TestDate = testDate,
                        BiomarkerName = "HbA1c",
                        Value = 5.2m,
                        ReferenceRangeMin = 4.0m,
                        ReferenceRangeMax = 6.4m,
                        OptimalRangeMin = 4.0m,
                        OptimalRangeMax = 5.7m,
                        Units = "%",
                        Status = BiomarkerStatus.Optimal
                    },
                    new BiomarkerResult
                    {
                        HealthProfileId = healthProfile.Id,
                        TestDate = testDate,
                        BiomarkerName = "Fasting Glucose",
                        Value = 88,
                        ReferenceRangeMin = 70,
                        ReferenceRangeMax = 110,
                        OptimalRangeMin = 70,
                        OptimalRangeMax = 100,
                        Units = "mg/dL",
                        Status = BiomarkerStatus.Optimal
                    },
                    // Hormonal markers
                    new BiomarkerResult
                    {
                        HealthProfileId = healthProfile.Id,
                        TestDate = testDate,
                        BiomarkerName = "Vitamin D",
                        Value = (decimal)(32 + (monthOffset * 4)), // Improving with supplementation
                        ReferenceRangeMin = 20,
                        ReferenceRangeMax = 80,
                        OptimalRangeMin = 40,
                        OptimalRangeMax = 60,
                        Units = "ng/mL",
                        Status = monthOffset >= 3 ? BiomarkerStatus.Optimal : BiomarkerStatus.Low
                    },
                    new BiomarkerResult
                    {
                        HealthProfileId = healthProfile.Id,
                        TestDate = testDate,
                        BiomarkerName = "TSH",
                        Value = (decimal)(2.8 - (monthOffset * 0.1)),
                        ReferenceRangeMin = 0.4m,
                        ReferenceRangeMax = 4.5m,
                        OptimalRangeMin = 1.0m,
                        OptimalRangeMax = 2.5m,
                        Units = "mIU/L",
                        Status = monthOffset >= 6 ? BiomarkerStatus.Optimal : BiomarkerStatus.High
                    }
                });
            }
            
            context.BiomarkerResults.AddRange(biomarkers);
            
            // Add sample PROMIS scores from specification
            var promisResults = new List<PromisResult>
            {
                new PromisResult
                {
                    HealthProfileId = healthProfile.Id,
                    AssessmentDate = DateTime.UtcNow.AddMonths(-3),
                    Domain = "Physical Function",
                    TScore = 55.2m,
                    PercentileRank = 65,
                    SeverityLevel = "Normal",
                    ItemsAnswered = 8
                },
                new PromisResult
                {
                    HealthProfileId = healthProfile.Id,
                    AssessmentDate = DateTime.UtcNow.AddMonths(-3),
                    Domain = "Fatigue",
                    TScore = 45.8m,
                    PercentileRank = 35,
                    SeverityLevel = "Mild",
                    ItemsAnswered = 8
                },
                new PromisResult
                {
                    HealthProfileId = healthProfile.Id,
                    AssessmentDate = DateTime.UtcNow.AddMonths(-3),
                    Domain = "Pain Interference",
                    TScore = 40.1m,
                    PercentileRank = 25,
                    SeverityLevel = "Normal",
                    ItemsAnswered = 6
                },
                new PromisResult
                {
                    HealthProfileId = healthProfile.Id,
                    AssessmentDate = DateTime.UtcNow.AddMonths(-3),
                    Domain = "Depression",
                    TScore = 42.3m,
                    PercentileRank = 28,
                    SeverityLevel = "Normal",
                    ItemsAnswered = 8
                },
                new PromisResult
                {
                    HealthProfileId = healthProfile.Id,
                    AssessmentDate = DateTime.UtcNow.AddMonths(-3),
                    Domain = "Anxiety",
                    TScore = 48.9m,
                    PercentileRank = 45,
                    SeverityLevel = "Normal",
                    ItemsAnswered = 7
                }
            };
            
            context.PromisResults.AddRange(promisResults);
            
            // Add some user treatments
            var userTreatments = new List<UserTreatment>
            {
                new UserTreatment
                {
                    HealthProfileId = healthProfile.Id,
                    TreatmentId = treatments[0].Id, // Vitamin D3
                    StartDate = DateTime.UtcNow.AddMonths(-4),
                    Dosage = "4000 IU daily",
                    Frequency = "Once daily with breakfast",
                    ResponseRating = 4,
                    ProviderSupervised = false
                },
                new UserTreatment
                {
                    HealthProfileId = healthProfile.Id,
                    TreatmentId = treatments[1].Id, // Omega-3
                    StartDate = DateTime.UtcNow.AddMonths(-3),
                    Dosage = "2g daily",
                    Frequency = "Twice daily with meals",
                    ResponseRating = 4,
                    ProviderSupervised = false
                }
            };
            
            context.UserTreatments.AddRange(userTreatments);
            
            await context.SaveChangesAsync();
        }
    }
}