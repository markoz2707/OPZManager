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

    public class UpdateEquipmentModelDto
    {
        public int ManufacturerId { get; set; }
        public int TypeId { get; set; }
        public string ModelName { get; set; } = string.Empty;
    }

    public class FolderImportResultDto
    {
        public int TotalFiles { get; set; }
        public int CreatedModels { get; set; }
        public int UploadedDocuments { get; set; }
        public int SkippedFiles { get; set; }
        public int Errors { get; set; }
        public List<ImportedItemDto> Items { get; set; } = new();
    }

    public class ImportedItemDto
    {
        public string ManufacturerName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "created", "existing", "uploaded", "error", "skipped"
        public string? ErrorMessage { get; set; }
    }

    public class ImportFolderRequestDto
    {
        public string? FolderPath { get; set; }
    }
}
