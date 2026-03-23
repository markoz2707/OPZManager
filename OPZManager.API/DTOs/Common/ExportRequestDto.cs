namespace OPZManager.API.DTOs.Common
{
    public class ExportRequestDto
    {
        public string Content { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Format { get; set; } = "pdf"; // "pdf" or "docx"
    }
}
