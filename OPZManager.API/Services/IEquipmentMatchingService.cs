using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public interface IEquipmentMatchingService
    {
        // Equipment matching methods
        Task<List<EquipmentMatch>> FindMatchingEquipmentAsync(OPZDocument opzDocument);
        Task<decimal> CalculateMatchScoreAsync(EquipmentModel equipment, List<OPZRequirement> requirements);
        Task<List<EquipmentModel>> GetEquipmentByManufacturerAsync(int manufacturerId);
        Task<List<EquipmentModel>> GetEquipmentByTypeAsync(int typeId);
        Task<EquipmentModel?> CreateEquipmentModelAsync(int manufacturerId, int typeId, string modelName, Dictionary<string, object> specifications);

        // Equipment catalog management methods
        Task<List<Manufacturer>> GetAllManufacturersAsync();
        Task<List<EquipmentType>> GetAllEquipmentTypesAsync();
        Task<List<EquipmentModel>> GetAllEquipmentModelsAsync();
        Task<EquipmentModel?> GetEquipmentModelByIdAsync(int id);
        Task<Manufacturer> CreateManufacturerAsync(Manufacturer manufacturer);
        Task<EquipmentType> CreateEquipmentTypeAsync(EquipmentType equipmentType);
        Task<EquipmentModel> CreateEquipmentModelAsync(EquipmentModel equipmentModel);
        Task<bool> DeleteManufacturerAsync(int id);
        Task<bool> DeleteEquipmentTypeAsync(int id);
        Task<bool> DeleteEquipmentModelAsync(int id);
    }
}
