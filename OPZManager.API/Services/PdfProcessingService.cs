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

        public PdfProcessingService(ApplicationDbContext context, IPllumIntegrationService pllumService)
        {
            _context = context;
            _pllumService = pllumService;
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
                // Use AI to extract specifications
                var analysisResult = await _pllumService.AnalyzeOPZRequirementsAsync(
                    $"Extract technical specifications from this {equipmentType} document: {pdfText}");
                
                // Parse the AI response and extract key-value pairs
                // This is a simplified implementation - in production, you'd want more sophisticated parsing
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
            catch (Exception ex)
            {
                // Fallback to basic text parsing if AI fails
                specifications = ExtractSpecificationsBasic(pdfText);
            }
            
            return specifications;
        }

        public async Task<List<OPZRequirement>> ExtractOPZRequirementsAsync(string pdfText)
        {
            var requirements = new List<OPZRequirement>();
            
            try
            {
                // Use AI to extract requirements
                var analysisResult = await _pllumService.AnalyzeOPZRequirementsAsync(pdfText);
                
                // Parse requirements from AI response
                var sections = analysisResult.Split(new[] { "REQUIREMENT:", "REQ:" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var section in sections.Skip(1)) // Skip first empty section
                {
                    var requirement = new OPZRequirement
                    {
                        RequirementText = section.Trim(),
                        RequirementType = DetermineRequirementType(section),
                        ExtractedSpecsJson = "{}"
                    };
                    
                    requirements.Add(requirement);
                }
            }
            catch (Exception ex)
            {
                // Fallback to basic text parsing
                requirements = ExtractRequirementsBasic(pdfText);
            }
            
            return requirements;
        }

        public async Task<bool> IndexDocumentAsync(DocumentModel document)
        {
            try
            {
                var pdfText = await ExtractTextFromPdfAsync(document.FilePath);
                
                // Extract specifications based on equipment type
                var equipmentType = document.Type?.Name ?? "Unknown";
                var specifications = await ExtractSpecificationsAsync(pdfText, equipmentType);
                
                // Save specifications to database
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
            catch (Exception ex)
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
            
            // Add title
            var titleParagraph = new Paragraph(title)
                .SetFontSize(18);
            document.Add(titleParagraph);
            
            // Add content
            document.Add(new Paragraph(content)
                .SetFontSize(12));
            
            document.Close();
            return stream.ToArray();
        }

        private Dictionary<string, object> ExtractSpecificationsBasic(string text)
        {
            var specs = new Dictionary<string, object>();
            
            // Basic pattern matching for common specifications
            var patterns = new Dictionary<string, string[]>
            {
                ["RAM"] = new[] { @"(\d+)\s*GB\s*RAM", @"Memory:\s*(\d+)\s*GB" },
                ["Storage"] = new[] { @"(\d+)\s*TB", @"Storage:\s*(\d+)\s*TB" },
                ["CPU"] = new[] { @"(\d+)\s*Core", @"Processor:\s*([^,\n]+)" },
                ["RAID"] = new[] { @"RAID\s*(\d+)", @"RAID\s*Level\s*(\d+)" }
            };
            
            foreach (var pattern in patterns)
            {
                foreach (var regex in pattern.Value)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(text, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        specs[pattern.Key] = match.Groups[1].Value;
                        break;
                    }
                }
            }
            
            return specs;
        }

        private List<OPZRequirement> ExtractRequirementsBasic(string text)
        {
            var requirements = new List<OPZRequirement>();
            
            // Split text into paragraphs and look for requirement-like content
            var paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Length > 50 && (
                    paragraph.ToLower().Contains("wymagane") ||
                    paragraph.ToLower().Contains("musi") ||
                    paragraph.ToLower().Contains("powinien") ||
                    paragraph.ToLower().Contains("requirement")))
                {
                    requirements.Add(new OPZRequirement
                    {
                        RequirementText = paragraph.Trim(),
                        RequirementType = DetermineRequirementType(paragraph),
                        ExtractedSpecsJson = "{}"
                    });
                }
            }
            
            return requirements;
        }

        private string DetermineRequirementType(string text)
        {
            var lowerText = text.ToLower();
            
            if (lowerText.Contains("performance") || lowerText.Contains("wydajność"))
                return "Performance";
            if (lowerText.Contains("technical") || lowerText.Contains("techniczne"))
                return "Technical";
            if (lowerText.Contains("compliance") || lowerText.Contains("zgodność"))
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
