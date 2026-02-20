using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class TrainingDataService : ITrainingDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TrainingDataService> _logger;

        public TrainingDataService(ApplicationDbContext context, ILogger<TrainingDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TrainingData>> GenerateTrainingDataAsync()
        {
            var trainingData = new List<TrainingData>();

            try
            {
                // Generate training data from existing OPZ documents and equipment matches
                await GenerateFromOPZMatches(trainingData);
                
                // Generate training data from equipment specifications
                await GenerateFromEquipmentSpecs(trainingData);
                
                // Generate training data from document specifications
                await GenerateFromDocumentSpecs(trainingData);

                // Save generated training data
                _context.TrainingData.AddRange(trainingData);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generated {trainingData.Count} training data entries");
                return trainingData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate training data");
                return new List<TrainingData>();
            }
        }

        public async Task<TrainingData> CreateTrainingDataAsync(string question, string answer, string context, string dataType)
        {
            var trainingData = new TrainingData
            {
                Question = question,
                Answer = answer,
                Context = context,
                DataType = dataType
            };

            _context.TrainingData.Add(trainingData);
            await _context.SaveChangesAsync();

            return trainingData;
        }

        public async Task<List<TrainingData>> GetTrainingDataAsync(string? dataType = null)
        {
            var query = _context.TrainingData.AsQueryable();

            if (!string.IsNullOrEmpty(dataType))
            {
                query = query.Where(td => td.DataType == dataType);
            }

            return await query.OrderByDescending(td => td.CreatedAt).ToListAsync();
        }

        public async Task<string> ExportTrainingDataAsJsonAsync(string? dataType = null)
        {
            var trainingData = await GetTrainingDataAsync(dataType);
            
            var exportData = trainingData.Select(td => new
            {
                question = td.Question,
                answer = td.Answer,
                context = td.Context,
                type = td.DataType,
                created = td.CreatedAt
            }).ToList();

            return JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        public async Task<bool> ImportTrainingDataFromJsonAsync(string jsonData)
        {
            try
            {
                var importData = JsonSerializer.Deserialize<List<JsonElement>>(jsonData);
                if (importData == null) return false;

                var trainingDataList = new List<TrainingData>();

                foreach (var item in importData)
                {
                    var question = item.TryGetProperty("question", out var q) ? q.GetString() : "";
                    var answer = item.TryGetProperty("answer", out var a) ? a.GetString() : "";
                    var context = item.TryGetProperty("context", out var c) ? c.GetString() : "";
                    var dataType = item.TryGetProperty("type", out var t) ? t.GetString() : "QA";

                    if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
                    {
                        trainingDataList.Add(new TrainingData
                        {
                            Question = question,
                            Answer = answer,
                            Context = context ?? "",
                            DataType = dataType ?? "QA"
                        });
                    }
                }

                _context.TrainingData.AddRange(trainingDataList);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Imported {trainingDataList.Count} training data entries");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import training data");
                return false;
            }
        }

        private async Task GenerateFromOPZMatches(List<TrainingData> trainingData)
        {
            var matches = await _context.EquipmentMatches
                .Include(m => m.OPZDocument)
                .Include(m => m.EquipmentModel)
                    .ThenInclude(e => e.Manufacturer)
                .Include(m => m.EquipmentModel)
                    .ThenInclude(e => e.Type)
                .Include(m => m.OPZDocument.OPZRequirements)
                .Where(m => m.MatchScore > 0.5m)
                .Take(100) // Limit to avoid too much data
                .ToListAsync();

            foreach (var match in matches)
            {
                var requirements = string.Join("\n", match.OPZDocument.OPZRequirements.Select(r => r.RequirementText));
                var equipment = $"{match.EquipmentModel.Manufacturer.Name} {match.EquipmentModel.Type.Name} {match.EquipmentModel.ModelName}";

                // Question-Answer pair for equipment recommendation
                trainingData.Add(new TrainingData
                {
                    Question = $"Jakie urządzenie spełnia następujące wymagania OPZ: {requirements}",
                    Answer = $"Rekomendowane urządzenie: {equipment}. {match.ComplianceDescription}",
                    Context = $"OPZ: {match.OPZDocument.Filename}, Match Score: {match.MatchScore}",
                    DataType = "RequirementMatch"
                });

                // Reverse question for OPZ generation
                trainingData.Add(new TrainingData
                {
                    Question = $"Jakie wymagania OPZ powinny być określone dla urządzenia {equipment}?",
                    Answer = requirements,
                    Context = $"Equipment specs: {match.EquipmentModel.SpecificationsJson}",
                    DataType = "OPZGeneration"
                });
            }
        }

        private async Task GenerateFromEquipmentSpecs(List<TrainingData> trainingData)
        {
            var equipment = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => !string.IsNullOrEmpty(e.SpecificationsJson) && e.SpecificationsJson != "{}")
                .Take(50)
                .ToListAsync();

            foreach (var item in equipment)
            {
                if (item.Specifications != null && item.Specifications.Any())
                {
                    foreach (var spec in item.Specifications)
                    {
                        // Specification extraction training
                        trainingData.Add(new TrainingData
                        {
                            Question = $"Jaka jest wartość parametru {spec.Key} dla urządzenia {item.Manufacturer.Name} {item.ModelName}?",
                            Answer = spec.Value.ToString() ?? "",
                            Context = $"Equipment: {item.Type.Name}, Full specs: {item.SpecificationsJson}",
                            DataType = "SpecExtraction"
                        });

                        // Technical requirement generation
                        trainingData.Add(new TrainingData
                        {
                            Question = $"Jak sformułować wymaganie techniczne dla parametru {spec.Key} w OPZ?",
                            Answer = $"Wymagane minimum {spec.Key}: {spec.Value} lub równoważne",
                            Context = $"Based on {item.Manufacturer.Name} {item.ModelName} specification",
                            DataType = "RequirementGeneration"
                        });
                    }
                }
            }
        }

        private async Task GenerateFromDocumentSpecs(List<TrainingData> trainingData)
        {
            var documentSpecs = await _context.DocumentSpecs
                .Include(ds => ds.Document)
                    .ThenInclude(d => d.Manufacturer)
                .Include(ds => ds.Document)
                    .ThenInclude(d => d.Type)
                .Take(100)
                .ToListAsync();

            foreach (var spec in documentSpecs)
            {
                if (spec.Document.Manufacturer != null && spec.Document.Type != null)
                {
                    // Document analysis training
                    trainingData.Add(new TrainingData
                    {
                        Question = $"Jaka jest wartość {spec.SpecKey} w dokumentacji {spec.Document.Manufacturer.Name} {spec.Document.Type.Name}?",
                        Answer = spec.SpecValue,
                        Context = $"Document: {spec.Document.Filename}, Type: {spec.SpecType}",
                        DataType = "DocumentAnalysis"
                    });

                    // Specification comparison
                    trainingData.Add(new TrainingData
                    {
                        Question = $"Czy {spec.SpecValue} dla parametru {spec.SpecKey} jest wystarczające dla zastosowań {spec.Document.Type.Name}?",
                        Answer = $"Wartość {spec.SpecValue} dla {spec.SpecKey} jest typowa dla urządzeń typu {spec.Document.Type.Name} od producenta {spec.Document.Manufacturer.Name}",
                        Context = $"Specification analysis for {spec.Document.Type.Name}",
                        DataType = "SpecComparison"
                    });
                }
            }
        }
    }
}
