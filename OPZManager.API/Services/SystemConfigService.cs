using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public SystemConfigService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> GetAsync(string key, string defaultValue = "")
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null)
                return setting.Value;

            // Fallback to IConfiguration
            return _configuration[key] ?? defaultValue;
        }

        public async Task SetAsync(string key, string value, string category = "")
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null)
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(category))
                    setting.Category = category;
            }
            else
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Category = category,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<LlmSettingsDto> GetLlmSettingsAsync()
        {
            var provider = await GetAsync("LlmSettings:Provider", "local");

            return new LlmSettingsDto
            {
                Provider = provider,
                BaseUrl = await GetLlmBaseUrlAsync(provider),
                ApiKey = await GetLlmApiKeyAsync(provider),
                ModelName = await GetLlmModelNameAsync(provider)
            };
        }

        public async Task<EmbeddingSettingsDto> GetEmbeddingSettingsAsync()
        {
            var provider = await GetAsync("EmbeddingSettings:Provider", "openai-compatible");

            return new EmbeddingSettingsDto
            {
                Provider = provider,
                BaseUrl = await GetEmbeddingBaseUrlAsync(provider),
                ApiKey = await GetEmbeddingApiKeyAsync(provider),
                ModelName = await GetEmbeddingModelNameAsync(provider),
                Dimensions = int.TryParse(await GetEmbeddingDimensionsAsync(provider), out var d) ? d : 1536
            };
        }

        public async Task UpdateLlmSettingsAsync(LlmSettingsDto dto)
        {
            await SetAsync("LlmSettings:Provider", dto.Provider, "LLM");

            switch (dto.Provider.ToLower())
            {
                case "gemini":
                    if (!string.IsNullOrEmpty(dto.ApiKey))
                        await SetAsync("LlmSettings:Gemini:ApiKey", dto.ApiKey, "LLM");
                    if (!string.IsNullOrEmpty(dto.ModelName))
                        await SetAsync("LlmSettings:Gemini:ModelName", dto.ModelName, "LLM");
                    break;
                case "anthropic":
                    if (!string.IsNullOrEmpty(dto.ApiKey))
                        await SetAsync("LlmSettings:Anthropic:ApiKey", dto.ApiKey, "LLM");
                    if (!string.IsNullOrEmpty(dto.ModelName))
                        await SetAsync("LlmSettings:Anthropic:ModelName", dto.ModelName, "LLM");
                    break;
                default: // "local"
                    if (!string.IsNullOrEmpty(dto.BaseUrl))
                        await SetAsync("LlmSettings:Local:BaseUrl", dto.BaseUrl, "LLM");
                    if (!string.IsNullOrEmpty(dto.ApiKey))
                        await SetAsync("LlmSettings:Local:ApiKey", dto.ApiKey, "LLM");
                    if (!string.IsNullOrEmpty(dto.ModelName))
                        await SetAsync("LlmSettings:Local:ModelName", dto.ModelName, "LLM");
                    break;
            }
        }

        public async Task UpdateEmbeddingSettingsAsync(EmbeddingSettingsDto dto)
        {
            await SetAsync("EmbeddingSettings:Provider", dto.Provider, "Embedding");

            switch (dto.Provider.ToLower())
            {
                case "gemini":
                    if (!string.IsNullOrEmpty(dto.ApiKey))
                        await SetAsync("EmbeddingSettings:Gemini:ApiKey", dto.ApiKey, "Embedding");
                    if (!string.IsNullOrEmpty(dto.ModelName))
                        await SetAsync("EmbeddingSettings:Gemini:ModelName", dto.ModelName, "Embedding");
                    if (dto.Dimensions > 0)
                        await SetAsync("EmbeddingSettings:Gemini:Dimensions", dto.Dimensions.ToString(), "Embedding");
                    break;
                default: // "openai-compatible" or "mistral"
                    var section = dto.Provider.ToLower() == "mistral" ? "Mistral" : "OpenAICompatible";
                    if (!string.IsNullOrEmpty(dto.BaseUrl))
                        await SetAsync($"EmbeddingSettings:{section}:BaseUrl", dto.BaseUrl, "Embedding");
                    if (!string.IsNullOrEmpty(dto.ApiKey))
                        await SetAsync($"EmbeddingSettings:{section}:ApiKey", dto.ApiKey, "Embedding");
                    if (!string.IsNullOrEmpty(dto.ModelName))
                        await SetAsync($"EmbeddingSettings:{section}:ModelName", dto.ModelName, "Embedding");
                    if (dto.Dimensions > 0)
                        await SetAsync($"EmbeddingSettings:{section}:Dimensions", dto.Dimensions.ToString(), "Embedding");
                    break;
            }
        }

        // Helper methods for provider-specific config keys
        private async Task<string> GetLlmBaseUrlAsync(string provider) => provider.ToLower() switch
        {
            "gemini" => "https://generativelanguage.googleapis.com",
            "anthropic" => "https://api.anthropic.com",
            _ => await GetAsync("LlmSettings:Local:BaseUrl", "http://localhost:1234/v1/")
        };

        private async Task<string> GetLlmApiKeyAsync(string provider) => provider.ToLower() switch
        {
            "gemini" => await GetAsync("LlmSettings:Gemini:ApiKey", ""),
            "anthropic" => await GetAsync("LlmSettings:Anthropic:ApiKey", ""),
            _ => await GetAsync("LlmSettings:Local:ApiKey", "")
        };

        private async Task<string> GetLlmModelNameAsync(string provider) => provider.ToLower() switch
        {
            "gemini" => await GetAsync("LlmSettings:Gemini:ModelName", "gemini-2.0-flash"),
            "anthropic" => await GetAsync("LlmSettings:Anthropic:ModelName", "claude-sonnet-4-20250514"),
            _ => await GetAsync("LlmSettings:Local:ModelName", "pllum")
        };

        private async Task<string> GetEmbeddingBaseUrlAsync(string provider) => provider.ToLower() switch
        {
            "gemini" => "https://generativelanguage.googleapis.com",
            _ => await GetAsync($"EmbeddingSettings:{(provider.ToLower() == "mistral" ? "Mistral" : "OpenAICompatible")}:BaseUrl", "http://localhost:1234/v1/")
        };

        private async Task<string> GetEmbeddingApiKeyAsync(string provider) => provider.ToLower() switch
        {
            "gemini" => await GetAsync("EmbeddingSettings:Gemini:ApiKey", ""),
            _ => await GetAsync($"EmbeddingSettings:{(provider.ToLower() == "mistral" ? "Mistral" : "OpenAICompatible")}:ApiKey", "")
        };

        private async Task<string> GetEmbeddingModelNameAsync(string provider) => provider.ToLower() switch
        {
            "gemini" => await GetAsync("EmbeddingSettings:Gemini:ModelName", "text-embedding-004"),
            _ => await GetAsync($"EmbeddingSettings:{(provider.ToLower() == "mistral" ? "Mistral" : "OpenAICompatible")}:ModelName", "text-embedding-3-small")
        };

        private async Task<string> GetEmbeddingDimensionsAsync(string provider) => provider.ToLower() switch
        {
            "gemini" => await GetAsync("EmbeddingSettings:Gemini:Dimensions", "768"),
            _ => await GetAsync($"EmbeddingSettings:{(provider.ToLower() == "mistral" ? "Mistral" : "OpenAICompatible")}:Dimensions", "1536")
        };
    }
}
