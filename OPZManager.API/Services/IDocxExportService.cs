namespace OPZManager.API.Services
{
    public interface IDocxExportService
    {
        Task<byte[]> GenerateDocxAsync(string content, string title);
    }
}
