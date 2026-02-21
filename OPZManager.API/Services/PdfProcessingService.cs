using System.Text.Json;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using OPZManager.API.Data;
using OPZManager.API.Models;
using Microsoft.EntityFrameworkCore;
using DocumentModel = OPZManager.API.Models.Document;
using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using LayoutDocument = iText.Layout.Document;

namespace OPZManager.API.Services
{
    public class PdfProcessingService : IPdfProcessingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPllumIntegrationService _pllumService;
        private readonly ILogger<PdfProcessingService> _logger;

        public PdfProcessingService(ApplicationDbContext context, IPllumIntegrationService pllumService, ILogger<PdfProcessingService> logger)
        {
            _context = context;
            _pllumService = pllumService;
            _logger = logger;
        }

        public async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            try
            {
                using var reader = new PdfReader(filePath);
                using var document = new PdfDocument(reader);

                var text = string.Empty;
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    var page = document.GetPage(i);
                    text += PdfTextExtractor.GetTextFromPage(page);
                }

                return text;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<string, object>> ExtractSpecificationsAsync(string pdfText, string equipmentType)
        {
            var specifications = new Dictionary<string, object>();

            try
            {
                var analysisResult = await _pllumService.AnalyzeOPZRequirementsAsync(
                    $"Extract technical specifications from this {equipmentType} document: {pdfText}");

                var lines = analysisResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains(':'))
                    {
                        var parts = line.Split(':', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();
                            specifications[key] = value;
                        }
                    }
                }
            }
            catch
            {
                specifications = ExtractSpecificationsBasic(pdfText);
            }

            return specifications;
        }

        public async Task<List<OPZRequirement>> ExtractOPZRequirementsAsync(string pdfText)
        {
            if (string.IsNullOrWhiteSpace(pdfText))
                return new List<OPZRequirement>();

            // Try LLM structured extraction first
            try
            {
                _logger.LogInformation("Attempting LLM structured requirement extraction for {Length} chars of PDF text", pdfText.Length);
                var llmRequirements = await _pllumService.ExtractStructuredRequirementsAsync(pdfText);
                if (llmRequirements.Count >= 2)
                {
                    _logger.LogInformation("LLM extracted {Count} structured requirements, using LLM results", llmRequirements.Count);
                    return llmRequirements.Select(r =>
                    {
                        // Include device name in requirement text for context
                        var text = !string.IsNullOrWhiteSpace(r.Device) && r.Device != "Ogólne"
                            ? $"[{r.Device}] {r.Requirement}"
                            : r.Requirement;

                        // Include device in specs JSON
                        var specsDict = r.Specs ?? new Dictionary<string, string>();
                        if (!string.IsNullOrWhiteSpace(r.Device))
                            specsDict["_device"] = r.Device;

                        return new OPZRequirement
                        {
                            RequirementText = text.Length > 2000 ? text[..2000] : text,
                            RequirementType = r.Category,
                            ExtractedSpecsJson = JsonSerializer.Serialize(specsDict)
                        };
                    }).ToList();
                }
                _logger.LogWarning("LLM returned only {Count} requirements, falling back to rule-based", llmRequirements.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM extraction failed, falling back to rule-based");
            }

            // Fallback: rule-based extraction
            _logger.LogInformation("Using rule-based requirement extraction");
            var requirements = ExtractRequirementsFromText(pdfText);
            _logger.LogInformation("Rule-based extracted {Count} requirements", requirements.Count);

            return requirements;
        }

        private async Task<List<OPZRequirement>> ExtractRequirementsWithAIAsync(string pdfText)
        {
            var truncated = pdfText.Length > 30000 ? pdfText[..30000] : pdfText;
            var analysisResult = await _pllumService.AnalyzeOPZRequirementsAsync(truncated);

            var requirements = new List<OPZRequirement>();

            // Strategy 1: REQUIREMENT: / WYMAGANIE: markers
            var sections = analysisResult.Split(
                new[] { "REQUIREMENT:", "REQ:", "WYMAGANIE:" },
                StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length > 1)
            {
                foreach (var section in sections.Skip(1))
                {
                    var text = section.Trim();
                    if (text.Length > 10)
                    {
                        requirements.Add(new OPZRequirement
                        {
                            RequirementText = text.Length > 2000 ? text[..2000] : text,
                            RequirementType = DetermineRequirementType(text),
                            ExtractedSpecsJson = "{}"
                        });
                    }
                }
                return requirements;
            }

            // Strategy 2: Numbered items (1. xxx, 2. xxx)
            var numberedPattern = new Regex(
                @"^\s*\d+[\.\)]\s+(.+?)(?=\n\s*\d+[\.\)]|\z)",
                RegexOptions.Multiline | RegexOptions.Singleline);
            var matches = numberedPattern.Matches(analysisResult);
            foreach (Match match in matches)
            {
                var text = match.Groups[1].Value.Trim();
                if (text.Length > 20)
                {
                    requirements.Add(new OPZRequirement
                    {
                        RequirementText = text.Length > 2000 ? text[..2000] : text,
                        RequirementType = DetermineRequirementType(text),
                        ExtractedSpecsJson = "{}"
                    });
                }
            }
            if (requirements.Count > 0) return requirements;

            // Strategy 3: Bullet points (- xxx, * xxx, • xxx)
            var lines = analysisResult.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if ((trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("• ")) && trimmed.Length > 20)
                {
                    requirements.Add(new OPZRequirement
                    {
                        RequirementText = trimmed[2..].Trim(),
                        RequirementType = DetermineRequirementType(trimmed),
                        ExtractedSpecsJson = "{}"
                    });
                }
            }

            return requirements;
        }

        /// <summary>
        /// Rule-based requirement extraction from Polish OPZ text.
        /// Groups consecutive related lines into consolidated requirements.
        /// </summary>
        private List<OPZRequirement> ExtractRequirementsFromText(string pdfText)
        {
            var requirements = new List<OPZRequirement>();
            var lines = pdfText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var requirementKeywords = new[] {
                "wymagane", "wymagany", "wymagana", "wymagań",
                "musi", "muszą", "powinien", "powinno", "powinna",
                "minimum", "minimaln", "co najmniej", "nie mniej niż",
                "maksymaln", "nie więcej niż", "nie gorsz",
                "obsługa", "obsługi", "obsługuje",
                "zapewnia", "zapewni", "zapewnienie",
                "zgodn", "certyfikat", "norma", "spełnia",
                "gwarancj", "wsparcie", "serwis",
                "dostaw", "termin", "realizacj",
                "procesor", "pamięć", "dysk", "raid", "zasilacz",
                "interfejs", "port", "złącze", "slot"
            };

            // Detect numbered rows like "1. ", "2. ", "LP " — these are table row boundaries
            var numberedRowPattern = new Regex(
                @"^[\s]*(\d+)\.\s+",
                RegexOptions.IgnoreCase);

            var currentSection = "General";
            var currentGroupLines = new List<string>();
            var currentGroupSection = "General";

            void FlushGroup()
            {
                if (currentGroupLines.Count == 0) return;
                var fullText = string.Join(" ", currentGroupLines).Trim();
                // Only create a requirement if the consolidated text is meaningful
                if (fullText.Length >= 30)
                {
                    requirements.Add(new OPZRequirement
                    {
                        RequirementText = fullText.Length > 2000 ? fullText[..2000] : fullText,
                        RequirementType = currentGroupSection,
                        ExtractedSpecsJson = "{}"
                    });
                }
                currentGroupLines.Clear();
            }

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < 10)
                {
                    // Empty/short line = group boundary
                    FlushGroup();
                    continue;
                }

                var lower = trimmed.ToLower();

                // Detect section context
                if (lower.Contains("wymagania techniczne") || lower.Contains("specyfikacja techniczna") || lower.Contains("parametry techniczne") || lower.Contains("wymagania minimalne"))
                {
                    FlushGroup();
                    currentSection = "Technical";
                    continue; // Skip section header itself
                }
                else if (lower.Contains("wydajność") || lower.Contains("parametry wydajnościowe"))
                {
                    FlushGroup();
                    currentSection = "Performance";
                    continue;
                }
                else if (lower.Contains("zgodność") || lower.Contains("certyfikat") || lower.Contains("normy"))
                {
                    FlushGroup();
                    currentSection = "Compliance";
                }
                else if (lower.Contains("gwarancj") || lower.Contains("serwis"))
                {
                    FlushGroup();
                    currentSection = "General";
                }

                // Check if this is a new numbered row (table boundary)
                if (numberedRowPattern.IsMatch(trimmed))
                {
                    FlushGroup();
                    currentGroupSection = currentSection;
                }

                var isRequirement = requirementKeywords.Any(kw => lower.Contains(kw));
                // Bullet point lines (starting with -, *, •) are continuations
                var isBullet = trimmed.StartsWith("-") || trimmed.StartsWith("*") || trimmed.StartsWith("•") || trimmed.StartsWith("\u2013") || trimmed.StartsWith("\u2022");

                if (isRequirement || isBullet)
                {
                    if (currentGroupLines.Count == 0)
                        currentGroupSection = currentSection;
                    currentGroupLines.Add(trimmed);
                }
                else if (currentGroupLines.Count > 0)
                {
                    // Continue accumulating if this looks like a continuation (no period at end of last line, or starts lowercase)
                    var lastLine = currentGroupLines.Last();
                    if (!lastLine.EndsWith(".") || char.IsLower(trimmed[0]))
                    {
                        currentGroupLines.Add(trimmed);
                    }
                    else
                    {
                        FlushGroup();
                    }
                }
            }

            FlushGroup();

            return requirements;
        }

        public async Task<bool> IndexDocumentAsync(DocumentModel document)
        {
            try
            {
                var pdfText = await ExtractTextFromPdfAsync(document.FilePath);

                var equipmentType = document.Type?.Name ?? "Unknown";
                var specifications = await ExtractSpecificationsAsync(pdfText, equipmentType);

                foreach (var spec in specifications)
                {
                    var documentSpec = new DocumentSpec
                    {
                        DocumentId = document.Id,
                        SpecKey = spec.Key,
                        SpecValue = spec.Value.ToString() ?? string.Empty,
                        SpecType = DetermineSpecType(spec.Value)
                    };

                    _context.DocumentSpecs.Add(documentSpec);
                }

                document.IndexedDate = DateTime.UtcNow;
                document.Status = "Indexed";

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                document.Status = "Failed";
                await _context.SaveChangesAsync();
                return false;
            }
        }

        public async Task<byte[]> GenerateOPZPdfAsync(string content, string title)
        {
            using var stream = new MemoryStream();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            using var document = new LayoutDocument(pdf);

            var titleParagraph = new Paragraph(title)
                .SetFontSize(18);
            document.Add(titleParagraph);

            document.Add(new Paragraph(content)
                .SetFontSize(12));

            document.Close();
            return stream.ToArray();
        }

        private Dictionary<string, object> ExtractSpecificationsBasic(string text)
        {
            var specs = new Dictionary<string, object>();

            var patterns = new Dictionary<string, string[]>
            {
                ["RAM"] = new[] { @"(\d+)\s*GB\s*RAM", @"Memory:\s*(\d+)\s*GB", @"pamięć[:\s]*(\d+)\s*GB" },
                ["Storage"] = new[] { @"(\d+)\s*TB", @"Storage:\s*(\d+)\s*TB", @"dysk[:\s]*(\d+)\s*TB" },
                ["CPU"] = new[] { @"(\d+)\s*Core", @"Processor:\s*([^,\n]+)", @"procesor[:\s]*([^,\n]+)" },
                ["RAID"] = new[] { @"RAID\s*(\d+)", @"RAID\s*Level\s*(\d+)" }
            };

            foreach (var pattern in patterns)
            {
                foreach (var regex in pattern.Value)
                {
                    var match = Regex.Match(text, regex, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        specs[pattern.Key] = match.Groups[1].Value;
                        break;
                    }
                }
            }

            return specs;
        }

        private string DetermineRequirementType(string text)
        {
            var lowerText = text.ToLower();

            if (lowerText.Contains("performance") || lowerText.Contains("wydajność") || lowerText.Contains("iops") || lowerText.Contains("throughput"))
                return "Performance";
            if (lowerText.Contains("technical") || lowerText.Contains("techniczne") || lowerText.Contains("procesor") || lowerText.Contains("pamięć") || lowerText.Contains("dysk"))
                return "Technical";
            if (lowerText.Contains("compliance") || lowerText.Contains("zgodność") || lowerText.Contains("certyfikat") || lowerText.Contains("norma"))
                return "Compliance";

            return "General";
        }

        private string DetermineSpecType(object value)
        {
            if (value is bool)
                return "Boolean";
            if (value is int || value is decimal || value is double)
                return "Number";

            return "Text";
        }
    }
}
