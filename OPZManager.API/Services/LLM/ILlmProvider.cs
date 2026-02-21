namespace OPZManager.API.Services.LLM
{
    public interface ILlmProvider
    {
        string ProviderName { get; }
        string ModelName { get; }
        Task<string> SendChatAsync(string systemPrompt, string userPrompt, int maxTokens = 2000, double temperature = 0.7);
        Task<bool> TestConnectionAsync();
    }
}
