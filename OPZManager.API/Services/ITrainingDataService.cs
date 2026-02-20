using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public interface ITrainingDataService
    {
        Task<List<TrainingData>> GenerateTrainingDataAsync();
        Task<TrainingData> CreateTrainingDataAsync(string question, string answer, string context, string dataType);
        Task<List<TrainingData>> GetTrainingDataAsync(string? dataType = null);
        Task<string> ExportTrainingDataAsJsonAsync(string? dataType = null);
        Task<bool> ImportTrainingDataFromJsonAsync(string jsonData);
    }
}
