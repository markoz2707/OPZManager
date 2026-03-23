using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class SWZGenerationService : ISWZGenerationService
    {
        private readonly IPllumIntegrationService _llmService;
        private readonly IKnowledgeBaseService _kbService;
        private readonly ILogger<SWZGenerationService> _logger;

        public SWZGenerationService(
            IPllumIntegrationService llmService,
            IKnowledgeBaseService kbService,
            ILogger<SWZGenerationService> logger)
        {
            _llmService = llmService;
            _kbService = kbService;
            _logger = logger;
        }

        public async Task<SWZGenerationResult> GenerateSWZAsync(List<EquipmentModel> selectedModels, string equipmentType)
        {
            // Step 1: Extract specs from all models (from JSON + KB if available)
            var modelSpecs = new List<(EquipmentModel Model, Dictionary<string, string> Specs)>();

            foreach (var model in selectedModels)
            {
                var specs = ExtractSpecsFromModel(model);

                // Try to enrich from knowledge base
                try
                {
                    var kbChunks = await _kbService.SearchAsync(model.Id, $"specyfikacja techniczna parametry {model.ModelName}", 3);
                    if (kbChunks.Any())
                    {
                        var kbText = string.Join("\n", kbChunks.Select(c => c.Content));
                        var kbSpecs = await ExtractSpecsFromTextAsync(kbText, model.ModelName);
                        // Merge KB specs (don't overwrite existing)
                        foreach (var kv in kbSpecs)
                        {
                            specs.TryAdd(kv.Key, kv.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not enrich specs from KB for model {ModelId}", model.Id);
                }

                modelSpecs.Add((model, specs));
            }

            // Step 2: Find common parameters and compute "minimum envelope"
            var requirements = ComputeCommonRequirements(modelSpecs, equipmentType);

            // Step 3: Use LLM to generate the full SWZ document
            var content = await GenerateSWZDocumentAsync(requirements, equipmentType, selectedModels.Count);

            return new SWZGenerationResult
            {
                Content = content,
                ExtractedRequirements = requirements,
                MatchingModelsCount = selectedModels.Count,
            };
        }

        public async Task<string> GenerateSWZDocumentAsync(List<SWZRequirement> requirements, string equipmentType, int modelCount)
        {
            var reqText = new StringBuilder();
            foreach (var group in requirements.GroupBy(r => r.Category))
            {
                reqText.AppendLine($"[{group.Key}]");
                foreach (var req in group)
                {
                    reqText.AppendLine($"- {req.ParameterName}: {req.MinValue} {req.Unit} ({req.Description})");
                }
                reqText.AppendLine();
            }

            // Try LLM-based generation, fallback to manual template
            try
            {
                // Reuse the existing OPZ generation which handles LLM communication
                return await _llmService.GenerateOPZContentAsync(
                    new List<EquipmentModel>(),
                    $"SWZ - {equipmentType}"
                );
            }
            catch
            {
                return GenerateFallbackSWZ(requirements, equipmentType, modelCount);
            }
        }

        public SWZPreviewResult GetPreviewForAnonymous(SWZGenerationResult fullResult)
        {
            const int visibleCount = 10;
            var visible = fullResult.ExtractedRequirements.Take(visibleCount).ToList();
            var total = fullResult.ExtractedRequirements.Count;

            // Build preview content: show first 10 requirements, then cut
            var lines = fullResult.Content.Split('\n');
            var previewLines = new List<string>();
            var requirementsSeen = 0;

            foreach (var line in lines)
            {
                previewLines.Add(line);

                // Count numbered requirements (e.g. "2.1", "2.2", etc.)
                if (Regex.IsMatch(line.TrimStart(), @"^\d+\.\d+"))
                    requirementsSeen++;

                if (requirementsSeen >= visibleCount)
                {
                    previewLines.Add("");
                    previewLines.Add($"--- Aby zobaczyć pozostałe {total - visibleCount} wymagań, zarejestruj się w systemie ---");
                    break;
                }
            }

            return new SWZPreviewResult
            {
                PreviewContent = string.Join("\n", previewLines),
                TotalRequirements = total,
                VisibleRequirements = Math.Min(visibleCount, total),
                VisibleItems = visible,
            };
        }

        private List<SWZRequirement> ComputeCommonRequirements(
            List<(EquipmentModel Model, Dictionary<string, string> Specs)> modelSpecs,
            string equipmentType)
        {
            var requirements = new List<SWZRequirement>();

            if (!modelSpecs.Any()) return requirements;

            // Find parameters that exist in ALL models
            var allKeys = modelSpecs.SelectMany(m => m.Specs.Keys).Distinct().ToList();

            foreach (var key in allKeys)
            {
                var values = modelSpecs
                    .Where(m => m.Specs.ContainsKey(key))
                    .Select(m => m.Specs[key])
                    .ToList();

                var isCommon = values.Count == modelSpecs.Count;

                // Try to extract numeric value for "minimum" requirement
                var numericValues = values
                    .Select(v => ExtractNumericValue(v))
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                string minValue;
                string unit;
                string description;

                if (numericValues.Count == values.Count && numericValues.Any())
                {
                    // All values are numeric - use minimum as the requirement
                    var min = numericValues.Min();
                    unit = ExtractUnit(values.First());
                    minValue = FormatNumber(min);
                    description = $"nie mniej niż {minValue} {unit}".Trim();
                }
                else
                {
                    // Non-numeric: find common value or list options
                    var distinctValues = values.Distinct().ToList();
                    if (distinctValues.Count == 1)
                    {
                        minValue = SanitizeValue(distinctValues.First());
                        unit = "";
                        description = minValue;
                    }
                    else
                    {
                        // Multiple different values - use most restrictive or generic
                        minValue = SanitizeValue(string.Join(" / ", distinctValues.Take(3)));
                        unit = "";
                        description = $"obsługa: {minValue}";
                    }
                }

                var category = CategorizeParameter(key, equipmentType);

                requirements.Add(new SWZRequirement
                {
                    Category = category,
                    ParameterName = SanitizeParameterName(key),
                    MinValue = minValue,
                    Unit = unit,
                    Description = description,
                    IsCommon = isCommon,
                });
            }

            // Add standard requirements
            requirements.AddRange(GetStandardRequirements(equipmentType));

            return requirements.OrderBy(r => r.Category).ThenBy(r => r.ParameterName).ToList();
        }

        private static Dictionary<string, string> ExtractSpecsFromModel(EquipmentModel model)
        {
            var specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (model.Specifications != null)
            {
                foreach (var kv in model.Specifications)
                {
                    specs[kv.Key] = kv.Value?.ToString() ?? "";
                }
            }

            if (!string.IsNullOrWhiteSpace(model.SpecificationsJson) && model.SpecificationsJson != "{}")
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(model.SpecificationsJson);
                    if (parsed != null)
                    {
                        foreach (var kv in parsed)
                        {
                            specs.TryAdd(kv.Key, kv.Value.ToString());
                        }
                    }
                }
                catch { }
            }

            return specs;
        }

        private async Task<Dictionary<string, string>> ExtractSpecsFromTextAsync(string text, string modelName)
        {
            try
            {
                return await _llmService.ExtractEquipmentSpecsAsync(text);
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static double? ExtractNumericValue(string value)
        {
            var match = Regex.Match(value, @"([\d]+[.,]?\d*)");
            if (match.Success)
            {
                var numStr = match.Groups[1].Value.Replace(',', '.');
                if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                    return num;
            }
            return null;
        }

        private static string ExtractUnit(string value)
        {
            var match = Regex.Match(value, @"[\d.,]+\s*([A-Za-z/%]+.*)");
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        private static string FormatNumber(double value)
        {
            return value == Math.Floor(value) ? ((int)value).ToString() : value.ToString("F1", CultureInfo.InvariantCulture);
        }

        private static string SanitizeValue(string value)
        {
            // Remove manufacturer-specific terms
            var sanitized = Regex.Replace(value, @"\b(Dell|HPE|HP|IBM|Lenovo|Cisco|Intel|AMD|Xeon|EPYC|Core|Ryzen|PowerEdge|ProLiant|ThinkSystem)\b",
                "", RegexOptions.IgnoreCase).Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? value : sanitized;
        }

        private static string SanitizeParameterName(string key)
        {
            // Translate common English spec names to Polish if needed
            var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Processor", "Procesor" },
                { "CPU", "Procesor" },
                { "Memory", "Pamięć RAM" },
                { "RAM", "Pamięć RAM" },
                { "Storage", "Pojemność dyskowa" },
                { "Disk", "Dyski" },
                { "Network", "Interfejsy sieciowe" },
                { "Power Supply", "Zasilacz" },
                { "Form Factor", "Format obudowy" },
                { "Ports", "Porty" },
                { "Weight", "Waga" },
                { "Dimensions", "Wymiary" },
            };

            return translations.TryGetValue(key, out var translated) ? translated : key;
        }

        private static string CategorizeParameter(string key, string equipmentType)
        {
            var lower = key.ToLower();

            if (lower.Contains("procesor") || lower.Contains("cpu") || lower.Contains("processor"))
                return "Wydajność obliczeniowa";
            if (lower.Contains("ram") || lower.Contains("pamięć") || lower.Contains("memory"))
                return "Pamięć";
            if (lower.Contains("dysk") || lower.Contains("storage") || lower.Contains("ssd") || lower.Contains("hdd") || lower.Contains("nvme"))
                return "Pamięć masowa";
            if (lower.Contains("sieć") || lower.Contains("network") || lower.Contains("ethernet") || lower.Contains("port"))
                return "Łączność";
            if (lower.Contains("zasilacz") || lower.Contains("power") || lower.Contains("watt"))
                return "Zasilanie";
            if (lower.Contains("gwarancja") || lower.Contains("warranty"))
                return "Gwarancja i wsparcie";

            return "Parametry ogólne";
        }

        private static List<SWZRequirement> GetStandardRequirements(string equipmentType)
        {
            return new List<SWZRequirement>
            {
                new() { Category = "Certyfikaty", ParameterName = "Certyfikat CE", MinValue = "wymagany", Description = "Zgodność z dyrektywami UE", IsCommon = true },
                new() { Category = "Certyfikaty", ParameterName = "RoHS", MinValue = "wymagany", Description = "Zgodność z dyrektywą RoHS", IsCommon = true },
                new() { Category = "Certyfikaty", ParameterName = "Energy Star", MinValue = "wymagany lub równoważny", Description = "Certyfikat efektywności energetycznej", IsCommon = true },
                new() { Category = "Gwarancja i wsparcie", ParameterName = "Gwarancja producenta", MinValue = "36", Unit = "miesięcy", Description = "nie mniej niż 36 miesięcy", IsCommon = true },
                new() { Category = "Gwarancja i wsparcie", ParameterName = "Wsparcie techniczne", MinValue = "24/7", Description = "Wsparcie techniczne 24/7 w okresie gwarancji", IsCommon = true },
                new() { Category = "Gwarancja i wsparcie", ParameterName = "Czas reakcji", MinValue = "4", Unit = "godziny", Description = "nie więcej niż 4 godziny w dni robocze", IsCommon = true },
                new() { Category = "Dokumentacja", ParameterName = "Dokumentacja techniczna", MinValue = "w języku polskim", Description = "Pełna dokumentacja w języku polskim", IsCommon = true },
                new() { Category = "Dokumentacja", ParameterName = "Szkolenie", MinValue = "wymagane", Description = "Szkolenie administratorów w cenie dostawy", IsCommon = true },
            };
        }

        private string GenerateFallbackSWZ(List<SWZRequirement> requirements, string equipmentType, int modelCount)
        {
            var sb = new StringBuilder();

            sb.AppendLine("SPECYFIKACJA WARUNKÓW ZAMÓWIENIA (SWZ)");
            sb.AppendLine($"Przedmiot: Dostawa {equipmentType}");
            sb.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine(new string('=', 60));
            sb.AppendLine();

            sb.AppendLine("1. PRZEDMIOT ZAMÓWIENIA");
            sb.AppendLine();
            sb.AppendLine($"Przedmiotem zamówienia jest dostawa, instalacja i konfiguracja {equipmentType.ToLower()} ");
            sb.AppendLine($"w ilości {modelCount} sztuk, zgodnie z poniższą specyfikacją techniczną.");
            sb.AppendLine("Oferowany sprzęt musi być fabrycznie nowy, nieużywany i wyprodukowany nie wcześniej niż 12 miesięcy");
            sb.AppendLine("przed datą dostawy.");
            sb.AppendLine();

            sb.AppendLine("2. SPECYFIKACJA TECHNICZNA");
            sb.AppendLine();

            var sectionNum = 1;
            foreach (var group in requirements.GroupBy(r => r.Category))
            {
                sb.AppendLine($"2.{sectionNum}. {group.Key.ToUpper()}");
                sb.AppendLine();

                var reqNum = 1;
                foreach (var req in group)
                {
                    sb.AppendLine($"2.{sectionNum}.{reqNum}. {req.ParameterName}: {req.Description}");
                    reqNum++;
                }
                sb.AppendLine();
                sectionNum++;
            }

            sb.AppendLine("3. WYMAGANIA ZGODNOŚCI I CERTYFIKACJI");
            sb.AppendLine();
            sb.AppendLine("3.1. Oferowany sprzęt musi posiadać certyfikat CE");
            sb.AppendLine("3.2. Zgodność z dyrektywą RoHS 2011/65/UE");
            sb.AppendLine("3.3. Zgodność z dyrektywą WEEE 2012/19/UE");
            sb.AppendLine("3.4. Certyfikat Energy Star lub równoważny");
            sb.AppendLine("3.5. Certyfikat ISO 9001 producenta");
            sb.AppendLine("3.6. Certyfikat ISO 14001 producenta");
            sb.AppendLine();

            sb.AppendLine("4. WARUNKI GWARANCJI I WSPARCIA TECHNICZNEGO");
            sb.AppendLine();
            sb.AppendLine("4.1. Gwarancja producenta: nie mniej niż 36 miesięcy");
            sb.AppendLine("4.2. Wsparcie techniczne: 24/7 przez cały okres gwarancji");
            sb.AppendLine("4.3. Czas reakcji serwisu: nie więcej niż 4 godziny w dni robocze");
            sb.AppendLine("4.4. Naprawa lub wymiana sprzętu: nie więcej niż 24 godziny od zgłoszenia");
            sb.AppendLine("4.5. Serwis na miejscu u zamawiającego (on-site)");
            sb.AppendLine();

            sb.AppendLine("5. WARUNKI DOSTAWY I REALIZACJI");
            sb.AppendLine();
            sb.AppendLine("5.1. Termin dostawy: do 30 dni kalendarzowych od podpisania umowy");
            sb.AppendLine("5.2. Miejsce dostawy: siedziba zamawiającego");
            sb.AppendLine("5.3. Instalacja i konfiguracja: w cenie dostawy");
            sb.AppendLine("5.4. Dokumentacja techniczna w języku polskim: wraz z dostawą");
            sb.AppendLine("5.5. Szkolenie administratorów: minimum 8 godzin, w cenie dostawy");
            sb.AppendLine();

            sb.AppendLine("6. WYMAGANIA WOBEC WYKONAWCY");
            sb.AppendLine();
            sb.AppendLine("6.1. Minimum 3 zrealizowane dostawy o podobnym zakresie w ostatnich 3 latach");
            sb.AppendLine("6.2. Posiadanie autoryzacji serwisowej producenta oferowanego sprzętu");
            sb.AppendLine("6.3. Zespół certyfikowanych inżynierów serwisowych");
            sb.AppendLine("6.4. Referencje potwierdzające doświadczenie");
            sb.AppendLine();

            sb.AppendLine("7. KRYTERIA OCENY OFERT");
            sb.AppendLine();
            sb.AppendLine("7.1. Cena ofertowa brutto: waga 60%");
            sb.AppendLine("7.2. Parametry techniczne powyżej minimalnych: waga 25%");
            sb.AppendLine("7.3. Warunki gwarancji (okres powyżej minimum): waga 10%");
            sb.AppendLine("7.4. Termin dostawy (krótszy niż wymagany): waga 5%");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
