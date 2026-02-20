namespace OPZManager.API.DTOs.Equipment
{
    public class EquipmentModelDto
    {
        public int Id { get; set; }
        public int ManufacturerId { get; set; }
        public string ManufacturerName { get; set; } = string.Empty;
        public int TypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string SpecificationsJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; }
    }

    public class CreateEquipmentModelDto
    {
        public int ManufacturerId { get; set; }
        public int TypeId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string SpecificationsJson { get; set; } = "{}";
    }
}
