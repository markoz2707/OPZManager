namespace OPZManager.API.DTOs.OPZ
{
    public class GenerateOPZContentRequestDto
    {
        public List<int> EquipmentModelIds { get; set; } = new();
        public string EquipmentType { get; set; } = string.Empty;
    }

    public class GenerateOPZPdfRequestDto
    {
        public string Content { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
