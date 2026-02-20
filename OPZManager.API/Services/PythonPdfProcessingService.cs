using System.Diagnostics;
using System.Text.Json;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class PythonPdfProcessingService
    {
        private readonly ILogger<PythonPdfProcessingService> _logger;
        private readonly IConfiguration _configuration;

        public PythonPdfProcessingService(ILogger<PythonPdfProcessingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "scripts", "pdf_converter.py");
            if (!File.Exists(scriptPath))
            {
                scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "scripts", "pdf_converter.py");
            }

            var pythonPath = _configuration["PdfProcessing:PythonPath"] ?? "python";

            var psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{scriptPath}\" \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException("Failed to start Python process");

            var output = await process.StandardOutput.ReadToEndAsync();
            var errors = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Python PDF extraction failed: {Errors}", errors);
                throw new InvalidOperationException($"Python PDF extraction failed: {errors}");
            }

            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(output);
                if (result.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    return result.GetProperty("fullText").GetString() ?? string.Empty;
                }

                var errorMsg = result.TryGetProperty("error", out var err) ? err.GetString() : "Unknown error";
                throw new InvalidOperationException($"Python PDF extraction error: {errorMsg}");
            }
            catch (JsonException)
            {
                _logger.LogError("Invalid JSON from Python script: {Output}", output);
                throw new InvalidOperationException("Invalid response from Python PDF extractor");
            }
        }
    }
}
