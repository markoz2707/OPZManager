namespace OPZManager.API.Services.Embeddings
{
    public interface IEmbeddingProvider
    {
        string ProviderName { get; }
        string ModelName { get; }
        int Dimensions { get; }
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<float[][]> GenerateEmbeddingsAsync(IList<string> texts);
        Task<bool> TestConnectionAsync();
    }
}
