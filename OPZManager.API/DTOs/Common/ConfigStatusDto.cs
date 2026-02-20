namespace OPZManager.API.DTOs.Common
{
    public class ConfigStatusDto
    {
        public bool LlmConnected { get; set; }
        public string LlmBaseUrl { get; set; } = string.Empty;
        public int ManufacturersCount { get; set; }
        public int EquipmentTypesCount { get; set; }
        public int EquipmentModelsCount { get; set; }
        public int OPZDocumentsCount { get; set; }
        public int TrainingDataCount { get; set; }
    }
}
