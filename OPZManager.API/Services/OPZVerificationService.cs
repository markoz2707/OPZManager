using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.DTOs.Public;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class OPZVerificationService : IOPZVerificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfProcessingService _pdfService;
        private readonly ILogger<OPZVerificationService> _logger;

        public OPZVerificationService(
            ApplicationDbContext context,
            IPdfProcessingService pdfService,
            ILogger<OPZVerificationService> logger)
        {
            _context = context;
            _pdfService = pdfService;
            _logger = logger;
        }

        public async Task<OPZVerificationResult> VerifyDocumentAsync(OPZDocument document)
        {
            // Remove existing verification if any
            var existing = await _context.OPZVerificationResults
                .FirstOrDefaultAsync(v => v.OPZDocumentId == document.Id);
            if (existing != null)
                _context.OPZVerificationResults.Remove(existing);

            // Extract text from PDF
            string pdfText;
            try
            {
                pdfText = await _pdfService.ExtractTextFromPdfAsync(document.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from PDF {Id}", document.Id);
                pdfText = string.Empty;
            }

            var completeness = CheckCompleteness(pdfText);
            var compliance = CheckCompliance(pdfText);
            var technical = ValidateTechnicalSpecs(pdfText);
            var gaps = AnalyzeGaps(pdfText, completeness);

            var overallScore = CalculateOverallScore(
                completeness.Score, compliance.Score, technical.Score, gaps.Score);

            var grade = ScoreToGrade(overallScore);

            var summary = GenerateSummary(overallScore, grade, completeness, compliance, technical, gaps);

            var result = new OPZVerificationResult
            {
                OPZDocumentId = document.Id,
                OverallScore = overallScore,
                Grade = grade,
                CompletenessJson = JsonSerializer.Serialize(completeness),
                ComplianceJson = JsonSerializer.Serialize(compliance),
                TechnicalJson = JsonSerializer.Serialize(technical),
                GapAnalysisJson = JsonSerializer.Serialize(gaps),
                SummaryText = summary
            };

            _context.OPZVerificationResults.Add(result);
            await _context.SaveChangesAsync();

            return result;
        }

        public async Task<OPZVerificationResult?> GetVerificationResultAsync(int opzDocumentId)
        {
            return await _context.OPZVerificationResults
                .FirstOrDefaultAsync(v => v.OPZDocumentId == opzDocumentId);
        }

        private CompletenessResultDto CheckCompleteness(string text)
        {
            var lowerText = text.ToLowerInvariant();

            var sections = new List<SectionCheckDto>
            {
                CheckSection(lowerText, "Opis przedmiotu zamówienia",
                    new[] { "opis przedmiotu", "przedmiot zamówienia", "opz", "opis zamówienia" }),
                CheckSection(lowerText, "Wymagania techniczne",
                    new[] { "wymagania techniczne", "specyfikacja techniczna", "parametry techniczne", "wymagania sprzętowe" }),
                CheckSection(lowerText, "Gwarancja i serwis",
                    new[] { "gwarancja", "serwis", "wsparcie techniczne", "okres gwarancji" }),
                CheckSection(lowerText, "Warunki dostawy",
                    new[] { "dostawa", "warunki dostawy", "termin dostawy", "miejsce dostawy" }),
                CheckSection(lowerText, "Kryteria oceny ofert",
                    new[] { "kryteria oceny", "ocena ofert", "kryterium", "punktacja" }),
                CheckSection(lowerText, "Zgodność i certyfikaty",
                    new[] { "zgodność", "certyfikat", "norma", "atest", "deklaracja zgodności" }),
                CheckSection(lowerText, "Wymagania wobec wykonawcy",
                    new[] { "wykonawca", "warunki udziału", "zdolność techniczna", "doświadczenie" }),
            };

            var foundCount = sections.Count(s => s.Found);
            var score = (int)Math.Round((double)foundCount / sections.Count * 100);

            return new CompletenessResultDto { Score = score, Sections = sections };
        }

        private SectionCheckDto CheckSection(string text, string name, string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword))
                {
                    return new SectionCheckDto { Name = name, Found = true, Details = $"Znaleziono: \"{keyword}\"" };
                }
            }
            return new SectionCheckDto { Name = name, Found = false, Details = "Nie znaleziono sekcji" };
        }

        private ComplianceResultDto CheckCompliance(string text)
        {
            var lowerText = text.ToLowerInvariant();

            var norms = new List<NormCheckDto>
            {
                CheckNorm(lowerText, "Znak CE", new[] { "ce", "deklaracja zgodności ce", "oznakowanie ce" }),
                CheckNorm(lowerText, "Prawo Zamówień Publicznych", new[] { "pzp", "prawo zamówień publicznych", "ustawa pzp", "zamówienia publiczne" }),
                CheckNorm(lowerText, "ISO 9001", new[] { "iso 9001", "iso9001", "system zarządzania jakością" }),
                CheckNorm(lowerText, "ISO 14001", new[] { "iso 14001", "iso14001", "zarządzanie środowiskowe" }),
                CheckNorm(lowerText, "ISO 27001", new[] { "iso 27001", "iso27001", "bezpieczeństwo informacji" }),
                CheckNorm(lowerText, "RoHS", new[] { "rohs", "ograniczenie substancji niebezpiecznych" }),
                CheckNorm(lowerText, "WEEE", new[] { "weee", "zużyty sprzęt elektryczny" }),
                CheckNorm(lowerText, "Energy Star", new[] { "energy star", "energystar", "efektywność energetyczna" }),
            };

            var referencedCount = norms.Count(n => n.Referenced);
            var score = (int)Math.Round((double)referencedCount / norms.Count * 100);

            return new ComplianceResultDto { Score = score, Norms = norms };
        }

        private NormCheckDto CheckNorm(string text, string name, string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword))
                {
                    return new NormCheckDto { Name = name, Referenced = true, Details = $"Odwołanie: \"{keyword}\"" };
                }
            }
            return new NormCheckDto { Name = name, Referenced = false, Details = "Brak odwołania" };
        }

        private TechnicalResultDto ValidateTechnicalSpecs(string text)
        {
            var issues = new List<string>();

            // Count parameters with measurable values (numbers + units)
            var measurablePattern = new Regex(
                @"\b\d+[\.,]?\d*\s*(GB|TB|MB|GHz|MHz|Gb/s|Gbit|Mb/s|szt|W|V|A|kg|mm|cm|m|°C|dB|IOPS|rpm|ms)\b",
                RegexOptions.IgnoreCase);
            var measurableMatches = measurablePattern.Matches(text);
            var measurableParams = measurableMatches.Count;

            // Count total parameter-like entries (anything with ":" or specification pattern)
            var paramPattern = new Regex(@"(?:^|\n)\s*[\w\s]+\s*[:–-]\s*.+", RegexOptions.Multiline);
            var totalParams = Math.Max(paramPattern.Matches(text).Count, measurableParams);

            // Check for qualifying language
            var qualifierPattern = new Regex(
                @"\b(minimum|min\.|nie mniej niż|co najmniej|nie gorzej niż|nie więcej niż|maksymalnie|max\.)\b",
                RegexOptions.IgnoreCase);
            var qualifiersUsed = qualifierPattern.Matches(text).Count;

            // Identify issues
            if (measurableParams < 3)
                issues.Add("Zbyt mało mierzalnych parametrów technicznych (znaleziono: " + measurableParams + ")");

            if (qualifiersUsed < 2)
                issues.Add("Brak wystarczających kwalifikatorów (\"minimum\", \"nie mniej niż\" itp.)");

            // Check for vague language
            var vaguePattern = new Regex(
                @"\b(odpowiedni|wystarczający|dobry|nowoczesny|wydajny|szybki)\b",
                RegexOptions.IgnoreCase);
            var vagueCount = vaguePattern.Matches(text).Count;
            if (vagueCount > 3)
                issues.Add($"Zbyt wiele nieprecyzyjnych sformułowań ({vagueCount} wystąpień)");

            var score = 50; // base score
            if (measurableParams >= 10) score += 25;
            else if (measurableParams >= 5) score += 15;
            else if (measurableParams >= 3) score += 8;

            if (qualifiersUsed >= 5) score += 25;
            else if (qualifiersUsed >= 3) score += 15;
            else if (qualifiersUsed >= 1) score += 5;

            if (vagueCount <= 1) score += 10;
            else if (vagueCount > 5) score -= 15;

            score = Math.Clamp(score, 0, 100);

            return new TechnicalResultDto
            {
                Score = score,
                MeasurableParams = measurableParams,
                TotalParams = totalParams,
                QualifiersUsed = qualifiersUsed,
                Issues = issues
            };
        }

        private GapAnalysisResultDto AnalyzeGaps(string text, CompletenessResultDto completeness)
        {
            var missingSections = completeness.Sections
                .Where(s => !s.Found)
                .Select(s => s.Name)
                .ToList();

            var recommendations = new List<string>();

            var lowerText = text.ToLowerInvariant();

            if (!lowerText.Contains("gwarancja"))
                recommendations.Add("Dodaj sekcję dotyczącą warunków gwarancji (min. 36 miesięcy dla sprzętu IT)");

            if (!lowerText.Contains("sla") && !lowerText.Contains("poziom usług"))
                recommendations.Add("Rozważ dodanie wymagań SLA dla serwisu gwarancyjnego");

            if (!lowerText.Contains("szkoleni"))
                recommendations.Add("Rozważ dodanie wymagań dotyczących szkoleń dla użytkowników");

            if (!lowerText.Contains("migracj") && !lowerText.Contains("wdrożeni"))
                recommendations.Add("Rozważ dodanie wymagań dotyczących wdrożenia i migracji danych");

            if (!lowerText.Contains("backup") && !lowerText.Contains("kopia zapasowa"))
                recommendations.Add("Rozważ dodanie wymagań dotyczących kopii zapasowych");

            if (missingSections.Count == 0 && recommendations.Count == 0)
                recommendations.Add("Dokument zawiera wszystkie kluczowe sekcje");

            var score = (int)Math.Round((1.0 - (double)missingSections.Count / 7) * 100);
            score = Math.Clamp(score, 0, 100);

            return new GapAnalysisResultDto
            {
                Score = score,
                MissingSections = missingSections,
                Recommendations = recommendations
            };
        }

        private int CalculateOverallScore(int completeness, int compliance, int technical, int gaps)
        {
            // Weights: completeness 30%, compliance 25%, technical 25%, gaps 20%
            var weighted = completeness * 0.30 + compliance * 0.25 + technical * 0.25 + gaps * 0.20;
            return (int)Math.Round(weighted);
        }

        private string ScoreToGrade(int score)
        {
            return score switch
            {
                >= 90 => "A",
                >= 75 => "B",
                >= 60 => "C",
                >= 40 => "D",
                _ => "F"
            };
        }

        private string GenerateSummary(int score, string grade,
            CompletenessResultDto completeness, ComplianceResultDto compliance,
            TechnicalResultDto technical, GapAnalysisResultDto gaps)
        {
            var parts = new List<string>
            {
                $"Ocena ogólna: {score}/100 (ocena: {grade})",
                $"Kompletność: {completeness.Score}% ({completeness.Sections.Count(s => s.Found)}/{completeness.Sections.Count} sekcji)",
                $"Zgodność z normami: {compliance.Score}% ({compliance.Norms.Count(n => n.Referenced)}/{compliance.Norms.Count} norm)",
                $"Specyfikacja techniczna: {technical.Score}% ({technical.MeasurableParams} mierzalnych parametrów, {technical.QualifiersUsed} kwalifikatorów)"
            };

            if (gaps.MissingSections.Count > 0)
                parts.Add($"Brakujące sekcje: {string.Join(", ", gaps.MissingSections)}");

            if (technical.Issues.Count > 0)
                parts.Add($"Uwagi techniczne: {string.Join("; ", technical.Issues)}");

            return string.Join("\n", parts);
        }
    }
}
