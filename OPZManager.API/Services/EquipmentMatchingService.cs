using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class EquipmentMatchingService : IEquipmentMatchingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPllumIntegrationService _pllumService;

        public EquipmentMatchingService(ApplicationDbContext context, IPllumIntegrationService pllumService)
        {
            _context = context;
            _pllumService = pllumService;
        }

        public async Task<List<EquipmentMatch>> FindMatchingEquipmentAsync(OPZDocument opzDocument)
        {
            var matches = new List<EquipmentMatch>();

            // Get OPZ requirements
            var requirements = await _context.OPZRequirements
                .Where(r => r.OPZId == opzDocument.Id)
                .ToListAsync();

            if (!requirements.Any())
                return matches;

            // Get all equipment models
            var allEquipment = await _context.EquipmentModels
                .Include(e => e.Manufacturer)
                .Include(e => e.Type)
                .ToListAsync();

            foreach (var equipment in allEquipment)
            {
                var matchScore = await CalculateMatchScoreAsync(equipment, requirements);
                
                if (matchScore > 0.3m) // Only include matches with score > 30%
                {
                    var complianceDescription = await _pllumService.GenerateComplianceDescriptionAsync(
                        equipment, 
                        string.Join("\n", requirements.Select(r => r.RequirementText))
                    );

                    var match = new EquipmentMatch
                    {
                        OPZId = opzDocument.Id,
                        ModelId = equipment.Id,
                        MatchScore = matchScore,
                        ComplianceDescription = complianceDescription
                    };

                    matches.Add(match);
                }
            }

            // Sort by match score descending
            matches = matches.OrderByDescending(m => m.MatchScore).ToList();

            // Save matches to database
            _context.EquipmentMatches.AddRange(matches);
            await _context.SaveChangesAsync();

            return matches;
        }

        public async Task<decimal> CalculateMatchScoreAsync(EquipmentModel equipment, List<OPZRequirement> requirements)
        {
            if (!requirements.Any())
                return 0m;

            var totalScore = 0m;
            var maxPossibleScore = requirements.Count;

            foreach (var requirement in requirements)
            {
                var score = CalculateRequirementMatch(equipment, requirement);
                totalScore += score;
            }

            return totalScore / maxPossibleScore;
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
