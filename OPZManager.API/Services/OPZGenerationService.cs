using System.Text;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class OPZGenerationService : IOPZGenerationService
    {
        private readonly IPllumIntegrationService _pllumService;
        private readonly IPdfProcessingService _pdfService;

        public OPZGenerationService(IPllumIntegrationService pllumService, IPdfProcessingService pdfService)
        {
            _pllumService = pllumService;
            _pdfService = pdfService;
        }

        public async Task<string> GenerateOPZContentAsync(List<EquipmentModel> selectedEquipment, string equipmentType)
        {
            try
            {
                // Use AI service to generate content
                return await _pllumService.GenerateOPZContentAsync(selectedEquipment, equipmentType);
            }
            catch (Exception)
            {
                // Fallback to manual generation
                return await GenerateFallbackOPZContentAsync(selectedEquipment, equipmentType);
            }
        }

        public async Task<byte[]> GenerateOPZPdfAsync(string content, string title)
        {
            return await _pdfService.GenerateOPZPdfAsync(content, title);
        }

        public async Task<string> GenerateComplianceRequirementsAsync(List<EquipmentModel> equipment)
        {
            var sb = new StringBuilder();
            sb.AppendLine("WYMAGANIA ZGODNOŚCI I CERTYFIKACJI:");
            sb.AppendLine();

            // Standard compliance requirements
            sb.AppendLine("1. CERTYFIKATY WYMAGANE:");
            sb.AppendLine("   - Certyfikat CE (zgodność z dyrektywami UE)");
            sb.AppendLine("   - Certyfikat ISO 9001 (system zarządzania jakością)");
            sb.AppendLine("   - Certyfikat ISO 14001 (system zarządzania środowiskowego)");
            sb.AppendLine("   - Certyfikat ISO 27001 (bezpieczeństwo informacji) - jeśli dotyczy");
            sb.AppendLine();

            sb.AppendLine("2. WYMAGANIA BEZPIECZEŃSTWA:");
            sb.AppendLine("   - Zgodność z normami bezpieczeństwa elektrycznego");
            sb.AppendLine("   - Zgodność z normami EMC (kompatybilność elektromagnetyczna)");
            sb.AppendLine("   - Zgodność z RoHS (ograniczenie substancji niebezpiecznych)");
            sb.AppendLine();

            sb.AppendLine("3. WYMAGANIA ŚRODOWISKOWE:");
            sb.AppendLine("   - Zgodność z WEEE (utylizacja sprzętu elektrycznego)");
            sb.AppendLine("   - Certyfikat Energy Star lub równoważny");
            sb.AppendLine("   - Deklaracja zgodności środowiskowej");
            sb.AppendLine();

            // Equipment-specific requirements
            foreach (var item in equipment.GroupBy(e => e.Type.Name))
            {
                sb.AppendLine($"4. WYMAGANIA SPECYFICZNE DLA {item.Key.ToUpper()}:");
                
                switch (item.Key.ToLower())
                {
                    case "macierze dyskowe":
                        sb.AppendLine("   - Certyfikat zgodności z normami RAID");
                        sb.AppendLine("   - Certyfikat bezpieczeństwa danych");
                        sb.AppendLine("   - Zgodność z normami SAS/SATA");
                        break;
                    case "serwery":
                        sb.AppendLine("   - Certyfikat zgodności z normami serwerowymi");
                        sb.AppendLine("   - Certyfikat wirtualizacji (jeśli dotyczy)");
                        sb.AppendLine("   - Zgodność z normami rack 19\"");
                        break;
                    case "przełączniki sieciowe":
                        sb.AppendLine("   - Certyfikat zgodności z normami sieciowymi");
                        sb.AppendLine("   - Zgodność z IEEE 802.x");
                        sb.AppendLine("   - Certyfikat bezpieczeństwa sieciowego");
                        break;
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public async Task<string> GenerateTechnicalSpecificationsAsync(List<EquipmentModel> equipment)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SPECYFIKACJA TECHNICZNA:");
            sb.AppendLine();

            var groupedEquipment = equipment.GroupBy(e => e.Type.Name);

            foreach (var group in groupedEquipment)
            {
                sb.AppendLine($"{group.Key.ToUpper()}:");
                sb.AppendLine();

                // Collect all specifications from equipment in this group
                var allSpecs = new Dictionary<string, HashSet<string>>();
                
                foreach (var item in group)
                {
                    if (item.Specifications != null)
                    {
                        foreach (var spec in item.Specifications)
                        {
                            if (!allSpecs.ContainsKey(spec.Key))
                                allSpecs[spec.Key] = new HashSet<string>();
                            
                            allSpecs[spec.Key].Add(spec.Value.ToString() ?? "");
                        }
                    }
                }

                // Generate requirements based on collected specifications
                foreach (var spec in allSpecs.OrderBy(s => s.Key))
                {
                    if (spec.Value.Count == 1)
                    {
                        sb.AppendLine($"- {spec.Key}: minimum {spec.Value.First()}");
                    }
                    else
                    {
                        sb.AppendLine($"- {spec.Key}: jeden z: {string.Join(", ", spec.Value)}");
                    }
                }

                sb.AppendLine();
                sb.AppendLine("WYMAGANIA DODATKOWE:");
                sb.AppendLine("- Gwarancja: minimum 36 miesięcy");
                sb.AppendLine("- Wsparcie techniczne: 24/7 przez cały okres gwarancji");
                sb.AppendLine("- Dokumentacja: w języku polskim");
                sb.AppendLine("- Szkolenie: dla administratorów systemu");
                sb.AppendLine();

                // Add manufacturer options
                var manufacturers = group.Select(e => e.Manufacturer.Name).Distinct().ToList();
                if (manufacturers.Count > 1)
                {
                    sb.AppendLine("DOPUSZCZALNI PRODUCENCI:");
                    foreach (var manufacturer in manufacturers)
                    {
                        sb.AppendLine($"- {manufacturer}");
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public ContentPreviewResult SplitContentForPreview(string fullContent)
        {
            if (string.IsNullOrWhiteSpace(fullContent))
                return new ContentPreviewResult { Preview = string.Empty, FullContent = string.Empty };

            // Try to find section 3 marker
            var section3Patterns = new[] { "\n3.", "\n3 ", "\n3)" };
            var splitIndex = -1;

            foreach (var pattern in section3Patterns)
            {
                var idx = fullContent.IndexOf(pattern, StringComparison.Ordinal);
                if (idx > 0)
                {
                    splitIndex = idx;
                    break;
                }
            }

            if (splitIndex <= 0)
            {
                // Fallback: 30% of content, aligned to nearest newline
                splitIndex = (int)(fullContent.Length * 0.3);
                var nextNewline = fullContent.IndexOf('\n', splitIndex);
                if (nextNewline > 0)
                    splitIndex = nextNewline;
            }

            var preview = fullContent[..splitIndex].TrimEnd()
                + "\n\n--- Zaloguj się, aby zobaczyć pełny dokument ---";

            return new ContentPreviewResult
            {
                Preview = preview,
                FullContent = fullContent
            };
        }

        private async Task<string> GenerateFallbackOPZContentAsync(List<EquipmentModel> selectedEquipment, string equipmentType)
        {
            var sb = new StringBuilder();

            sb.AppendLine("OPIS ZAMÓWIENIA PUBLICZNEGO");
            sb.AppendLine($"Przedmiot: {equipmentType}");
            sb.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine("=" + new string('=', 50));
            sb.AppendLine();

            sb.AppendLine("1. PRZEDMIOT ZAMÓWIENIA");
            sb.AppendLine();
            sb.AppendLine($"Przedmiotem zamówienia jest dostawa {equipmentType.ToLower()} zgodnie z poniższą specyfikacją techniczną.");
            sb.AppendLine();

            // Technical specifications
            var techSpecs = await GenerateTechnicalSpecificationsAsync(selectedEquipment);
            sb.AppendLine("2. " + techSpecs);

            // Compliance requirements
            var complianceReqs = await GenerateComplianceRequirementsAsync(selectedEquipment);
            sb.AppendLine("3. " + complianceReqs);

            sb.AppendLine("4. WARUNKI REALIZACJI ZAMÓWIENIA");
            sb.AppendLine();
            sb.AppendLine("- Termin dostawy: do 30 dni od podpisania umowy");
            sb.AppendLine("- Miejsce dostawy: siedziба zamawiającego");
            sb.AppendLine("- Instalacja i konfiguracja: w cenie");
            sb.AppendLine("- Przekazanie dokumentacji: wraz z dostawą");
            sb.AppendLine("- Szkolenie użytkowników: w cenie");
            sb.AppendLine();

            sb.AppendLine("5. KRYTERIA OCENY OFERT");
            sb.AppendLine();
            sb.AppendLine("- Cena: 60%");
            sb.AppendLine("- Parametry techniczne: 25%");
            sb.AppendLine("- Warunki gwarancji: 10%");
            sb.AppendLine("- Doświadczenie wykonawcy: 5%");
            sb.AppendLine();

            sb.AppendLine("6. WYMAGANIA WOBEC WYKONAWCY");
            sb.AppendLine();
            sb.AppendLine("- Doświadczenie w realizacji podobnych projektów");
            sb.AppendLine("- Posiadanie autoryzacji producentów sprzętu");
            sb.AppendLine("- Zespół certyfikowanych specjalistów");
            sb.AppendLine("- Referencje z ostatnich 3 lat");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
