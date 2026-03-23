namespace OPZManager.API.Services
{
    public class SWZDocumentDto
    {
        public string Title { get; set; } = string.Empty;
        public string InstitutionName { get; set; } = string.Empty;
        public string ProcedureNumber { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string> Sections { get; set; } = new();
    }

    public interface IDocxExportService
    {
        Task<byte[]> GenerateDocxAsync(string content, string title);
        Task<byte[]> GenerateSWZDocxAsync(SWZDocumentDto document);
    }
}
