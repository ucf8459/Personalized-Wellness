using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using WellnessPlatform.Data;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services
{
    public class DataImportExportService
    {
        private readonly WellnessContext _context;
        private readonly DataValidationService _validationService;

        public DataImportExportService(WellnessContext context, DataValidationService validationService)
        {
            _context = context;
            _validationService = validationService;
        }

        #region Import Methods

        public async Task<ImportResult> ImportCsvDataAsync(string csvContent, string userId, ImportType importType)
        {
            var result = new ImportResult();
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length < 2)
            {
                result.Errors.Add("CSV file must have at least a header row and one data row");
                return result;
            }

            var headers = ParseCsvLine(lines[0]);
            var healthProfile = await GetOrCreateHealthProfileAsync(userId);

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var dataRow = ParseCsvLine(lines[i]);
                    if (dataRow.Length != headers.Length)
                    {
                        result.Errors.Add($"Row {i + 1}: Column count mismatch");
                        continue;
                    }

                    var dataDict = headers.Zip(dataRow, (h, v) => new { Header = h, Value = v })
                                        .ToDictionary(x => x.Header, x => x.Value);

                    switch (importType)
                    {
                        case ImportType.Biomarkers:
                            await ImportBiomarkerRowAsync(dataDict, healthProfile.Id, result);
                            break;
                        case ImportType.Promis:
                            await ImportPromisRowAsync(dataDict, healthProfile.Id, result);
                            break;
                        case ImportType.Lifestyle:
                            await ImportLifestyleRowAsync(dataDict, healthProfile.Id, result);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {i + 1}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            return result;
        }

        public async Task<ImportResult> ImportExcelDataAsync(byte[] excelData, string userId, ImportType importType)
        {
            var result = new ImportResult();
            
            try
            {
                // For now, we'll convert Excel to CSV format
                // In a production environment, you'd use a library like EPPlus or ClosedXML
                var csvContent = ConvertExcelToCsv(excelData);
                return await ImportCsvDataAsync(csvContent, userId, importType);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Excel import failed: {ex.Message}");
                return result;
            }
        }

        public async Task<ImportResult> ParsePdfLabReportAsync(byte[] pdfData, string userId)
        {
            var result = new ImportResult();
            
            try
            {
                // Extract text from PDF (simplified - in production use a PDF library)
                var pdfText = ExtractTextFromPdf(pdfData);
                var biomarkers = ParseLabReportText(pdfText);
                
                var healthProfile = await GetOrCreateHealthProfileAsync(userId);
                
                foreach (var biomarker in biomarkers)
                {
                    try
                    {
                        var validationResult = _validationService.ValidateBiomarkerResult(biomarker);
                        if (validationResult == ValidationResult.Success)
                        {
                            biomarker.HealthProfileId = healthProfile.Id;
                            _context.BiomarkerResults.Add(biomarker);
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.Errors.Add(validationResult.ErrorMessage ?? "Validation failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Biomarker import error: {ex.Message}");
                    }
                }
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"PDF parsing failed: {ex.Message}");
            }
            
            return result;
        }

        public async Task<BulkImportResult> ImportBulkDataAsync(List<ImportFile> files, string userId)
        {
            var result = new BulkImportResult();
            var totalFiles = files.Count;
            var processedFiles = 0;

            foreach (var file in files)
            {
                try
                {
                    ImportResult fileResult = file.Type switch
                    {
                        ImportType.Biomarkers => await ImportCsvDataAsync(file.Content, userId, ImportType.Biomarkers),
                        ImportType.Promis => await ImportCsvDataAsync(file.Content, userId, ImportType.Promis),
                        ImportType.Lifestyle => await ImportCsvDataAsync(file.Content, userId, ImportType.Lifestyle),
                        ImportType.PdfLabReport => await ParsePdfLabReportAsync(file.Data, userId),
                        _ => new ImportResult { Errors = { "Unsupported file type" } }
                    };

                    result.FileResults.Add(new FileImportResult
                    {
                        FileName = file.FileName,
                        Success = fileResult.Errors.Count == 0,
                        SuccessCount = fileResult.SuccessCount,
                        Errors = fileResult.Errors
                    });

                    processedFiles++;
                    result.Progress = (double)processedFiles / totalFiles * 100;
                }
                catch (Exception ex)
                {
                    result.FileResults.Add(new FileImportResult
                    {
                        FileName = file.FileName,
                        Success = false,
                        Errors = { ex.Message }
                    });
                }
            }

            return result;
        }

        #endregion

        #region Export Methods

        public async Task<ExportResult> ExportHealthDataAsync(string userId, ExportFormat format, DateTime? startDate = null, DateTime? endDate = null)
        {
            var healthProfile = await _context.HealthProfiles
                .Include(h => h.BiomarkerResults)
                .Include(h => h.PromisResults)
                .Include(h => h.Treatments)
                .Include(h => h.LifestyleMetrics)
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (healthProfile == null)
                return new ExportResult { Errors = { "No health profile found" } };

            var exportData = new HealthDataExport
            {
                UserId = userId,
                ExportDate = DateTime.UtcNow,
                BiomarkerResults = healthProfile.BiomarkerResults
                    .Where(b => !startDate.HasValue || b.TestDate >= startDate)
                    .Where(b => !endDate.HasValue || b.TestDate <= endDate)
                    .ToList(),
                PromisResults = healthProfile.PromisResults
                    .Where(p => !startDate.HasValue || p.AssessmentDate >= startDate)
                    .Where(p => !endDate.HasValue || p.AssessmentDate <= endDate)
                    .ToList(),
                UserTreatments = healthProfile.Treatments
                    .Where(t => !startDate.HasValue || t.StartDate >= startDate)
                    .Where(t => !endDate.HasValue || t.StartDate <= endDate)
                    .ToList(),
                LifestyleMetrics = healthProfile.LifestyleMetrics
                    .Where(l => !startDate.HasValue || l.RecordDate >= startDate)
                    .Where(l => !endDate.HasValue || l.RecordDate <= endDate)
                    .ToList()
            };

            return format switch
            {
                ExportFormat.Csv => ExportToCsv(exportData),
                ExportFormat.Json => ExportToJson(exportData),
                ExportFormat.Pdf => ExportToPdf(exportData),
                ExportFormat.Excel => ExportToExcel(exportData),
                _ => new ExportResult { Errors = { "Unsupported export format" } }
            };
        }

        public async Task<ExportResult> ExportBiomarkerReportAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var biomarkers = await _context.BiomarkerResults
                .Where(b => b.HealthProfile.UserId == userId)
                .Where(b => !startDate.HasValue || b.TestDate >= startDate)
                .Where(b => !endDate.HasValue || b.TestDate <= endDate)
                .OrderBy(b => b.BiomarkerName)
                .ThenBy(b => b.TestDate)
                .ToListAsync();

            var report = new BiomarkerReport
            {
                GeneratedDate = DateTime.UtcNow,
                DateRange = new DateRange { Start = startDate, End = endDate },
                Biomarkers = biomarkers.GroupBy(b => b.BiomarkerName)
                    .Select(g => new BiomarkerReportSummary
                    {
                        BiomarkerName = g.Key,
                        TestCount = g.Count(),
                        LatestValue = g.OrderByDescending(b => b.TestDate).First().Value,
                        LatestDate = g.OrderByDescending(b => b.TestDate).First().TestDate,
                        AverageValue = g.Average(b => b.Value),
                        MinValue = g.Min(b => b.Value),
                        MaxValue = g.Max(b => b.Value),
                        OptimalCount = g.Count(b => b.Status == BiomarkerStatus.Optimal),
                        NormalCount = g.Count(b => b.Status == BiomarkerStatus.Normal),
                        SuboptimalCount = g.Count(b => b.Status == BiomarkerStatus.Low || b.Status == BiomarkerStatus.High),
                        Units = g.First().Units
                    }).ToList()
            };

            return ExportToCsv(report);
        }

        #endregion

        #region Private Helper Methods

        private async Task<HealthProfile> GetOrCreateHealthProfileAsync(string userId)
        {
            var healthProfile = await _context.HealthProfiles
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (healthProfile == null)
            {
                healthProfile = new HealthProfile
                {
                    UserId = userId,
                    DateCreated = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                _context.HealthProfiles.Add(healthProfile);
                await _context.SaveChangesAsync();
            }

            return healthProfile;
        }

        private async Task ImportBiomarkerRowAsync(Dictionary<string, string> data, int healthProfileId, ImportResult result)
        {
            if (!DateTime.TryParse(data.GetValueOrDefault("TestDate"), out var testDate))
            {
                result.Errors.Add("Invalid TestDate format");
                return;
            }

            if (!decimal.TryParse(data.GetValueOrDefault("Value"), out var value))
            {
                result.Errors.Add("Invalid Value format");
                return;
            }

            var biomarker = new BiomarkerResult
            {
                HealthProfileId = healthProfileId,
                BiomarkerName = data.GetValueOrDefault("BiomarkerName", ""),
                Value = value,
                Units = data.GetValueOrDefault("Units", ""),
                TestDate = testDate,
                ReferenceRangeMin = decimal.TryParse(data.GetValueOrDefault("ReferenceRangeMin"), out var refMin) ? refMin : null,
                ReferenceRangeMax = decimal.TryParse(data.GetValueOrDefault("ReferenceRangeMax"), out var refMax) ? refMax : null,
                OptimalRangeMin = decimal.TryParse(data.GetValueOrDefault("OptimalRangeMin"), out var optMin) ? optMin : null,
                OptimalRangeMax = decimal.TryParse(data.GetValueOrDefault("OptimalRangeMax"), out var optMax) ? optMax : null,
                Status = Enum.TryParse<BiomarkerStatus>(data.GetValueOrDefault("Status"), out var status) ? status : BiomarkerStatus.Normal
            };

            var validationResult = _validationService.ValidateBiomarkerResult(biomarker);
            if (validationResult == ValidationResult.Success)
            {
                _context.BiomarkerResults.Add(biomarker);
                result.SuccessCount++;
            }
            else
            {
                result.Errors.Add(validationResult.ErrorMessage ?? "Validation failed");
            }
        }

        private async Task ImportPromisRowAsync(Dictionary<string, string> data, int healthProfileId, ImportResult result)
        {
            if (!DateTime.TryParse(data.GetValueOrDefault("AssessmentDate"), out var assessmentDate))
            {
                result.Errors.Add("Invalid AssessmentDate format");
                return;
            }

            if (!decimal.TryParse(data.GetValueOrDefault("TScore"), out var tScore))
            {
                result.Errors.Add("Invalid TScore format");
                return;
            }

            var promis = new PromisResult
            {
                HealthProfileId = healthProfileId,
                Domain = data.GetValueOrDefault("Domain", ""),
                TScore = tScore,
                PercentileRank = decimal.TryParse(data.GetValueOrDefault("PercentileRank"), out var percentile) ? percentile : null,
                ItemsAnswered = int.TryParse(data.GetValueOrDefault("ItemsAnswered"), out var items) ? items : 0,
                AssessmentDate = assessmentDate,
                SeverityLevel = data.GetValueOrDefault("SeverityLevel", "")
            };

            var validationResult = _validationService.ValidatePromisResult(promis);
            if (validationResult == ValidationResult.Success)
            {
                _context.PromisResults.Add(promis);
                result.SuccessCount++;
            }
            else
            {
                result.Errors.Add(validationResult.ErrorMessage ?? "Validation failed");
            }
        }

        private async Task ImportLifestyleRowAsync(Dictionary<string, string> data, int healthProfileId, ImportResult result)
        {
            if (!DateTime.TryParse(data.GetValueOrDefault("RecordDate"), out var recordDate))
            {
                result.Errors.Add("Invalid RecordDate format");
                return;
            }

            var lifestyle = new LifestyleMetric
            {
                HealthProfileId = healthProfileId,
                RecordDate = recordDate,
                SleepHours = decimal.TryParse(data.GetValueOrDefault("SleepHours"), out var sleep) ? sleep : null,
                SleepQuality = int.TryParse(data.GetValueOrDefault("SleepQuality"), out var sleepQuality) ? sleepQuality : null,
                ExerciseMinutes = int.TryParse(data.GetValueOrDefault("ExerciseMinutes"), out var exercise) ? exercise : null,
                ExerciseIntensity = int.TryParse(data.GetValueOrDefault("ExerciseIntensity"), out var intensity) ? intensity : null,
                StressLevel = int.TryParse(data.GetValueOrDefault("StressLevel"), out var stress) ? stress : null,
                EnergyLevel = int.TryParse(data.GetValueOrDefault("EnergyLevel"), out var energy) ? energy : null,
                MoodRating = int.TryParse(data.GetValueOrDefault("MoodRating"), out var mood) ? mood : null,
                Weight = decimal.TryParse(data.GetValueOrDefault("Weight"), out var weight) ? weight : null,
                BodyFatPercentage = decimal.TryParse(data.GetValueOrDefault("BodyFatPercentage"), out var bodyFat) ? bodyFat : null
            };

            var validationResult = _validationService.ValidateLifestyleMetric(lifestyle);
            if (validationResult == ValidationResult.Success)
            {
                _context.LifestyleMetrics.Add(lifestyle);
                result.SuccessCount++;
            }
            else
            {
                result.Errors.Add(validationResult.ErrorMessage ?? "Validation failed");
            }
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim());
            return result.ToArray();
        }

        private string ConvertExcelToCsv(byte[] excelData)
        {
            // Simplified conversion - in production, use a proper Excel library
            return Encoding.UTF8.GetString(excelData);
        }

        private string ExtractTextFromPdf(byte[] pdfData)
        {
            // Simplified text extraction - in production, use a PDF library like iTextSharp or PdfSharp
            return Encoding.UTF8.GetString(pdfData);
        }

        private List<BiomarkerResult> ParseLabReportText(string pdfText)
        {
            var biomarkers = new List<BiomarkerResult>();
            var lines = pdfText.Split('\n');

            foreach (var line in lines)
            {
                // Simple regex pattern for lab results
                var match = Regex.Match(line, @"(\w+)\s*([\d.]+)\s*([^\s]+)\s*([\d.-]+)\s*-\s*([\d.-]+)");
                if (match.Success)
                {
                    var biomarker = new BiomarkerResult
                    {
                        BiomarkerName = match.Groups[1].Value,
                        Value = decimal.Parse(match.Groups[2].Value),
                        Units = match.Groups[3].Value,
                        ReferenceRangeMin = decimal.Parse(match.Groups[4].Value),
                        ReferenceRangeMax = decimal.Parse(match.Groups[5].Value),
                        TestDate = DateTime.UtcNow,
                        Status = BiomarkerStatus.Normal
                    };
                    biomarkers.Add(biomarker);
                }
            }

            return biomarkers;
        }

        private ExportResult ExportToCsv(HealthDataExport data)
        {
            var csv = new StringBuilder();
            
            // Biomarkers
            csv.AppendLine("BiomarkerName,Value,Units,TestDate,Status,ReferenceRangeMin,ReferenceRangeMax");
            foreach (var biomarker in data.BiomarkerResults)
            {
                csv.AppendLine($"{biomarker.BiomarkerName},{biomarker.Value},{biomarker.Units},{biomarker.TestDate:yyyy-MM-dd},{biomarker.Status},{biomarker.ReferenceRangeMin},{biomarker.ReferenceRangeMax}");
            }

            return new ExportResult
            {
                Content = csv.ToString(),
                FileName = $"health_data_{DateTime.UtcNow:yyyyMMdd}.csv",
                ContentType = "text/csv"
            };
        }

        private ExportResult ExportToCsv(BiomarkerReport report)
        {
            var csv = new StringBuilder();
            csv.AppendLine("BiomarkerName,TestCount,LatestValue,LatestDate,AverageValue,MinValue,MaxValue,OptimalCount,NormalCount,SuboptimalCount,Units");
            
            foreach (var biomarker in report.Biomarkers)
            {
                csv.AppendLine($"{biomarker.BiomarkerName},{biomarker.TestCount},{biomarker.LatestValue},{biomarker.LatestDate:yyyy-MM-dd},{biomarker.AverageValue:F2},{biomarker.MinValue},{biomarker.MaxValue},{biomarker.OptimalCount},{biomarker.NormalCount},{biomarker.SuboptimalCount},{biomarker.Units}");
            }

            return new ExportResult
            {
                Content = csv.ToString(),
                FileName = $"biomarker_report_{DateTime.UtcNow:yyyyMMdd}.csv",
                ContentType = "text/csv"
            };
        }

        private ExportResult ExportToJson(HealthDataExport data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            return new ExportResult
            {
                Content = json,
                FileName = $"health_data_{DateTime.UtcNow:yyyyMMdd}.json",
                ContentType = "application/json"
            };
        }

        private ExportResult ExportToPdf(HealthDataExport data)
        {
            // Simplified PDF generation - in production, use a PDF library
            var pdfContent = $"Health Data Report\nGenerated: {DateTime.UtcNow}\n\nBiomarkers: {data.BiomarkerResults.Count}\nPROMIS Results: {data.PromisResults.Count}";
            
            return new ExportResult
            {
                Content = pdfContent,
                FileName = $"health_data_{DateTime.UtcNow:yyyyMMdd}.pdf",
                ContentType = "application/pdf"
            };
        }

        private ExportResult ExportToExcel(HealthDataExport data)
        {
            // Simplified Excel generation - in production, use an Excel library
            var excelContent = $"Health Data Report\nGenerated: {DateTime.UtcNow}\n\nBiomarkers: {data.BiomarkerResults.Count}\nPROMIS Results: {data.PromisResults.Count}";
            
            return new ExportResult
            {
                Content = excelContent,
                FileName = $"health_data_{DateTime.UtcNow:yyyyMMdd}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        #endregion
    }

    public enum ImportType
    {
        Biomarkers,
        Promis,
        Lifestyle,
        PdfLabReport
    }

    public enum ExportFormat
    {
        Csv,
        Json,
        Pdf,
        Excel
    }

    public class ImportResult
    {
        public int SuccessCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool IsSuccessful => Errors.Count == 0;
    }

    public class BulkImportResult
    {
        public List<FileImportResult> FileResults { get; set; } = new();
        public double Progress { get; set; }
        public bool IsSuccessful => FileResults.All(f => f.Success);
    }

    public class FileImportResult
    {
        public string FileName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int SuccessCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class ExportResult
    {
        public string Content { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public bool IsSuccessful => Errors.Count == 0;
    }

    public class ImportFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public byte[] Data { get; set; } = new byte[0];
        public ImportType Type { get; set; }
    }

    public class HealthDataExport
    {
        public string UserId { get; set; } = string.Empty;
        public DateTime ExportDate { get; set; }
        public List<BiomarkerResult> BiomarkerResults { get; set; } = new();
        public List<PromisResult> PromisResults { get; set; } = new();
        public List<UserTreatment> UserTreatments { get; set; } = new();
        public List<LifestyleMetric> LifestyleMetrics { get; set; } = new();
    }

    public class BiomarkerReport
    {
        public DateTime GeneratedDate { get; set; }
        public DateRange DateRange { get; set; } = new();
        public List<BiomarkerReportSummary> Biomarkers { get; set; } = new();
    }

    public class BiomarkerReportSummary
    {
        public string BiomarkerName { get; set; } = string.Empty;
        public int TestCount { get; set; }
        public decimal LatestValue { get; set; }
        public DateTime LatestDate { get; set; }
        public decimal AverageValue { get; set; }
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
        public int OptimalCount { get; set; }
        public int NormalCount { get; set; }
        public int SuboptimalCount { get; set; }
        public string Units { get; set; } = string.Empty;
    }

    public class DateRange
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }
} 