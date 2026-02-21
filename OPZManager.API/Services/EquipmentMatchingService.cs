using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class EquipmentMatchingService : IEquipmentMatchingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPllumIntegrationService _pllumService;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly ILogger<EquipmentMatchingService> _logger;

        public EquipmentMatchingService(
            ApplicationDbContext context,
            IPllumIntegrationService pllumService,
            IKnowledgeBaseService knowledgeBaseService,
            ILogger<EquipmentMatchingService> logger)
        {
            _context = context;
            _pllumService = pllumService;
            _knowledgeBaseService = knowledgeBaseService;
            _logger = logger;
        }

        public async Task<List<EquipmentMatch>> FindMatchingEquipmentAsync(OPZDocument opzDocument)
        {
            // Get OPZ requirements
            var requirements = await _context.OPZRequirements
                .Where(r => r.OPZId == opzDocument.Id)
                .ToListAsync();

            if (!requirements.Any())
                return new List<EquipmentMatch>();

            // Cleanup old matches (cascade deletes RequirementCompliances)
            var oldMatches = await _context.EquipmentMatches
                .Where(m => m.OPZId == opzDocument.Id)
                .ToListAsync();
            if (oldMatches.Any())
            {
                _context.EquipmentMatches.RemoveRange(oldMatches);
                await _context.SaveChangesAsync();
            }

            // Build requirement inputs with device parsed from [brackets]
            var requirementInputs = requirements.Select(r => new LlmRequirementInput
            {
                RequirementId = r.Id,
                Device = ParseDeviceFromRequirement(r.RequirementText),
                RequirementText = r.RequirementText
            }).ToList();

            var requirementsText = string.Join("\n", requirements.Select(r => r.RequirementText));

            // Get ALL equipment models (no type pre-filter — OPZ describes multiple device types)
            var allEquipment = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .ToListAsync();

            var matches = new List<EquipmentMatch>();

            foreach (var equipment in allEquipment)
            {
                try
                {
                    // RAG search: find relevant KB fragments for this equipment
                    var kbFragments = new List<KnowledgeSearchResult>();
                    try
                    {
                        kbFragments = await _knowledgeBaseService.SearchAsync(equipment.Id, requirementsText, topK: 3);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "No KB results for equipment {ModelId}", equipment.Id);
                    }

                    var kbText = kbFragments.Count > 0
                        ? string.Join("\n---\n", kbFragments.Select(f => f.Content))
                        : "Brak dodatkowej dokumentacji w bazie wiedzy.";

                    // LLM detailed scoring — one call per equipment, all requirements at once
                    var detailedResult = await _pllumService.ScoreEquipmentMatchDetailedAsync(
                        requirementInputs,
                        $"{equipment.Manufacturer.Name} {equipment.ModelName} ({equipment.Type.Name})\nSpecyfikacja: {equipment.SpecificationsJson}",
                        kbText);

                    // Calculate overall score from per-requirement results
                    var applicableReqs = detailedResult.Requirements
                        .Where(r => r.Status != "not_applicable")
                        .ToList();

                    decimal normalizedScore;
                    if (applicableReqs.Count > 0)
                    {
                        var sum = applicableReqs.Sum(r => r.Status switch
                        {
                            "met" => 1.0m,
                            "partial" => 0.5m,
                            _ => 0.0m
                        });
                        normalizedScore = sum / applicableReqs.Count;
                    }
                    else
                    {
                        // No applicable requirements — use LLM overall score as fallback
                        normalizedScore = Math.Clamp(detailedResult.OverallScore / 100.0m, 0m, 1m);
                    }

                    if (normalizedScore > 0.1m) // Include matches with score > 10%
                    {
                        var match = new EquipmentMatch
                        {
                            OPZId = opzDocument.Id,
                            ModelId = equipment.Id,
                            MatchScore = normalizedScore,
                            ComplianceDescription = detailedResult.OverallExplanation
                        };

                        matches.Add(match);

                        // Build RequirementCompliance entities (will be saved after match gets Id)
                        match.RequirementCompliances = detailedResult.Requirements
                            .Select(r => new RequirementCompliance
                            {
                                RequirementId = r.RequirementId,
                                Status = r.Status,
                                Explanation = r.Status == "met" ? null : r.Explanation
                            }).ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to score equipment {ModelId} against OPZ {OPZId}", equipment.Id, opzDocument.Id);
                }
            }

            // Sort by match score descending and take top 5
            matches = matches.OrderByDescending(m => m.MatchScore).Take(5).ToList();

            // Save matches + compliances to database
            _context.EquipmentMatches.AddRange(matches);
            await _context.SaveChangesAsync();

            return matches;
        }

        /// <summary>
        /// Parses device name from requirement text prefix like "[Serwer rack]" or "[Macierz dyskowa]".
        /// Returns "Ogólne" if no bracket prefix found.
        /// </summary>
        private static string ParseDeviceFromRequirement(string requirementText)
        {
            var match = Regex.Match(requirementText, @"^\[([^\]]+)\]");
            return match.Success ? match.Groups[1].Value : "Ogólne";
        }

        public async Task<decimal> CalculateMatchScoreAsync(EquipmentModel equipment, List<OPZRequirement> requirements)
        {
            if (!requirements.Any())
                return 0m;

            var requirementsText = string.Join("\n", requirements.Select(r => r.RequirementText));

            try
            {
                var kbFragments = new List<KnowledgeSearchResult>();
                try
                {
                    kbFragments = await _knowledgeBaseService.SearchAsync(equipment.Id, requirementsText, topK: 3);
                }
                catch { /* no KB data available */ }

                var kbText = kbFragments.Count > 0
                    ? string.Join("\n---\n", kbFragments.Select(f => f.Content))
                    : "Brak dodatkowej dokumentacji.";

                var llmScore = await _pllumService.ScoreEquipmentMatchAsync(
                    requirementsText,
                    $"{equipment.Manufacturer.Name} {equipment.ModelName}\nSpecyfikacja: {equipment.SpecificationsJson}",
                    kbText);

                return Math.Clamp(llmScore.Score / 100.0m, 0m, 1m);
            }
            catch
            {
                // Fallback to simple matching
                var totalScore = 0m;
                foreach (var requirement in requirements)
                {
                    totalScore += CalculateRequirementMatch(equipment, requirement);
                }
                return totalScore / requirements.Count;
            }
        }

        public async Task<List<EquipmentModel>> GetEquipmentByManufacturerAsync(int manufacturerId)
        {
            return await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => e.ManufacturerId == manufacturerId)
                .ToListAsync();
        }

        public async Task<List<EquipmentModel>> GetEquipmentByTypeAsync(int typeId)
        {
            return await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .Where(e => e.TypeId == typeId)
                .ToListAsync();
        }

        public async Task<EquipmentModel?> CreateEquipmentModelAsync(int manufacturerId, int typeId, string modelName, Dictionary<string, object> specifications)
        {
            var equipment = new EquipmentModel
            {
                ManufacturerId = manufacturerId,
                TypeId = typeId,
                ModelName = modelName,
                Specifications = specifications
            };

            _context.EquipmentModels.Add(equipment);
            await _context.SaveChangesAsync();

            return await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .FirstOrDefaultAsync(e => e.Id == equipment.Id);
        }

        // Equipment catalog management methods
        public async Task<List<Manufacturer>> GetAllManufacturersAsync()
        {
            return await _context.Manufacturers
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<List<EquipmentType>> GetAllEquipmentTypesAsync()
        {
            return await _context.EquipmentTypes
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<EquipmentModel>> GetAllEquipmentModelsAsync()
        {
            return await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .OrderBy(e => e.Manufacturer.Name)
                .ThenBy(e => e.Type.Name)
                .ThenBy(e => e.ModelName)
                .ToListAsync();
        }

        public async Task<EquipmentModel?> GetEquipmentModelByIdAsync(int id)
        {
            return await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Manufacturer> CreateManufacturerAsync(Manufacturer manufacturer)
        {
            _context.Manufacturers.Add(manufacturer);
            await _context.SaveChangesAsync();
            return manufacturer;
        }

        public async Task<EquipmentType> CreateEquipmentTypeAsync(EquipmentType equipmentType)
        {
            _context.EquipmentTypes.Add(equipmentType);
            await _context.SaveChangesAsync();
            return equipmentType;
        }

        public async Task<EquipmentModel> CreateEquipmentModelAsync(EquipmentModel equipmentModel)
        {
            _context.EquipmentModels.Add(equipmentModel);
            await _context.SaveChangesAsync();
            return equipmentModel;
        }

        public async Task<bool> DeleteManufacturerAsync(int id)
        {
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer == null)
                return false;

            // Check if manufacturer has associated equipment
            var hasEquipment = await _context.EquipmentModels.AnyAsync(e => e.ManufacturerId == id);
            if (hasEquipment)
                return false; // Cannot delete manufacturer with associated equipment

            _context.Manufacturers.Remove(manufacturer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteEquipmentTypeAsync(int id)
        {
            var equipmentType = await _context.EquipmentTypes.FindAsync(id);
            if (equipmentType == null)
                return false;

            // Check if type has associated equipment
            var hasEquipment = await _context.EquipmentModels.AnyAsync(e => e.TypeId == id);
            if (hasEquipment)
                return false; // Cannot delete type with associated equipment

            _context.EquipmentTypes.Remove(equipmentType);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteEquipmentModelAsync(int id)
        {
            var equipmentModel = await _context.EquipmentModels.FindAsync(id);
            if (equipmentModel == null)
                return false;

            _context.EquipmentModels.Remove(equipmentModel);
            await _context.SaveChangesAsync();
            return true;
        }

        private decimal CalculateRequirementMatch(EquipmentModel equipment, OPZRequirement requirement)
        {
            var score = 0m;
            var requirementText = requirement.RequirementText.ToLower();
            var equipmentSpecs = equipment.Specifications ?? new Dictionary<string, object>();

            // Check for direct specification matches
            foreach (var spec in equipmentSpecs)
            {
                var specKey = spec.Key.ToLower();
                var specValue = spec.Value.ToString()?.ToLower() ?? "";

                if (requirementText.Contains(specKey) || requirementText.Contains(specValue))
                {
                    score += 0.3m;
                }
            }

            // Check for manufacturer match
            if (requirementText.Contains(equipment.Manufacturer.Name.ToLower()))
            {
                score += 0.2m;
            }

            // Check for equipment type match
            if (requirementText.Contains(equipment.Type.Name.ToLower()))
            {
                score += 0.2m;
            }

            // Check for model name match
            if (requirementText.Contains(equipment.ModelName.ToLower()))
            {
                score += 0.3m;
            }

            // Additional scoring based on requirement type
            switch (requirement.RequirementType.ToLower())
            {
                case "technical":
                    score += CalculateTechnicalMatch(equipmentSpecs, requirementText);
                    break;
                case "performance":
                    score += CalculatePerformanceMatch(equipmentSpecs, requirementText);
                    break;
                case "compliance":
                    score += 0.1m; // Basic compliance assumption
                    break;
            }

            return Math.Min(score, 1.0m); // Cap at 1.0
        }

        private decimal CalculateTechnicalMatch(Dictionary<string, object> specs, string requirementText)
        {
            var score = 0m;

            // Check for common technical specifications
            var technicalTerms = new[] { "ram", "memory", "storage", "cpu", "processor", "disk", "ssd", "hdd", "raid" };

            foreach (var term in technicalTerms)
            {
                if (requirementText.Contains(term))
                {
                    // Check if equipment has related specification
                    var relatedSpec = specs.Keys.FirstOrDefault(k => k.ToLower().Contains(term));
                    if (relatedSpec != null)
                    {
                        score += 0.1m;
                    }
                }
            }

            return score;
        }

        private decimal CalculatePerformanceMatch(Dictionary<string, object> specs, string requirementText)
        {
            var score = 0m;

            // Check for performance-related terms
            var performanceTerms = new[] { "speed", "throughput", "iops", "bandwidth", "latency", "performance" };

            foreach (var term in performanceTerms)
            {
                if (requirementText.Contains(term))
                {
                    score += 0.1m;
                }
            }

            return score;
        }
    }
}
