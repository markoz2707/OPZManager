namespace OPZManager.API.Services
{
    public interface IFolderImportService
    {
        Task<FolderImportResult> ImportFromFolderAsync(string folderPath);
    }

    public class FolderImportResult
    {
        public int TotalFiles { get; set; }
        public int CreatedModels { get; set; }
        public int UploadedDocuments { get; set; }
        public int SkippedFiles { get; set; }
        public int Errors { get; set; }
        public List<ImportedItem> Items { get; set; } = new();
    }

    public class ImportedItem
    {
        public string ManufacturerName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
