using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OPZManager.API.Data;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class FolderImportService : IFolderImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly ILogger<FolderImportService> _logger;

        private static readonly Dictionary<string, string> SubfolderToType = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Servers", "Serwery" },
            { "Storage", "Macierze dyskowe" },
            { "Endpoints", "Komputery" },
        };

        public FolderImportService(
            ApplicationDbContext context,
            IKnowledgeBaseService knowledgeBaseService,
            ILogger<FolderImportService> logger)
        {
            _context = context;
            _knowledgeBaseService = knowledgeBaseService;
            _logger = logger;
        }

        public async Task<FolderImportResult> ImportFromFolderAsync(string folderPath)
        {
            var result = new FolderImportResult();

            if (!Directory.Exists(folderPath))
            {
                _logger.LogError("Import folder does not exist: {Path}", folderPath);
                return result;
            }

            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories);
            result.TotalFiles = pdfFiles.Length;

            _logger.LogInformation("Found {Count} PDF files in {Path}", pdfFiles.Length, folderPath);

            foreach (var pdfPath in pdfFiles)
            {
                try
                {
                    var relativePath = Path.GetRelativePath(folderPath, pdfPath);
                    var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (parts.Length < 2)
                    {
                        result.Items.Add(new ImportedItem
                        {
                            Filename = Path.GetFileName(pdfPath),
                            Status = "skipped",
                            ErrorMessage = "Plik nie jest w podfolderze producenta"
                        });
                        result.SkippedFiles++;
                        continue;
                    }

                    var manufacturerName = parts[0];
                    var filename = Path.GetFileName(pdfPath);

                    // Determine equipment type from subfolder
                    var (typeName, shouldSkip) = DetermineEquipmentType(parts);
                    if (shouldSkip)
                    {
                        result.Items.Add(new ImportedItem
                        {
                            ManufacturerName = manufacturerName,
                            Filename = filename,
                            Status = "skipped",
                            ErrorMessage = "Folder WhitePapers — pominięty"
                        });
                        result.SkippedFiles++;
                        continue;
                    }

                    // Parse model name from filename
                    var modelName = ParseModelName(manufacturerName, parts, filename);

                    // Find or create manufacturer
                    var manufacturer = await FindOrCreateManufacturerAsync(manufacturerName);

                    // Find or create equipment type
                    var equipmentType = await FindOrCreateEquipmentTypeAsync(typeName);

                    // Find or create equipment model
                    var (equipmentModel, modelCreated) = await FindOrCreateEquipmentModelAsync(
                        manufacturer.Id, equipmentType.Id, modelName);

                    if (modelCreated)
                        result.CreatedModels++;

                    // Upload PDF to knowledge base
                    await using var fileStream = File.OpenRead(pdfPath);
                    await _knowledgeBaseService.UploadDocumentAsync(equipmentModel.Id, fileStream, filename);
                    result.UploadedDocuments++;

                    result.Items.Add(new ImportedItem
                    {
                        ManufacturerName = manufacturerName,
                        TypeName = typeName,
                        ModelName = modelName,
                        Filename = filename,
                        Status = modelCreated ? "created" : "uploaded"
                    });

                    _logger.LogInformation("Imported {File} → {Manufacturer} / {Type} / {Model}",
                        filename, manufacturerName, typeName, modelName);
                }
                catch (Exception ex)
                {
                    var filename = Path.GetFileName(pdfPath);
                    _logger.LogError(ex, "Error importing {File}", filename);
                    result.Errors++;
                    result.Items.Add(new ImportedItem
                    {
                        Filename = filename,
                        Status = "error",
                        ErrorMessage = ex.Message
                    });
                }
            }

            _logger.LogInformation(
                "Import complete: {Total} files, {Created} models created, {Uploaded} documents uploaded, {Skipped} skipped, {Errors} errors",
                result.TotalFiles, result.CreatedModels, result.UploadedDocuments, result.SkippedFiles, result.Errors);

            return result;
        }

        private static (string typeName, bool shouldSkip) DetermineEquipmentType(string[] pathParts)
        {
            // pathParts[0] = manufacturer, pathParts[1..n-1] = subfolders, pathParts[n-1] = filename
            if (pathParts.Length < 3)
            {
                // File directly under manufacturer folder (e.g., HPE/file.pdf, xFusion/file.pdf)
                return ("Serwery", false);
            }

            var subfolder = pathParts[1];

            // Skip WhitePapers
            if (subfolder.Equals("WhitePapers", StringComparison.OrdinalIgnoreCase))
                return ("", true);

            if (SubfolderToType.TryGetValue(subfolder, out var typeName))
                return (typeName, false);

            // Unknown subfolder — default to Serwery
            return ("Serwery", false);
        }

        private static string ParseModelName(string manufacturer, string[] pathParts, string filename)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);

            switch (manufacturer.ToUpperInvariant())
            {
                case "DELL":
                    return ParseDellModelName(pathParts, nameWithoutExt);

                case "HPE":
                    return ParseHpeModelName(nameWithoutExt);

                case "LENOVO":
                    return ParseLenovoModelName(nameWithoutExt);

                case "XFUSION":
                    return ParseXFusionModelName(nameWithoutExt);

                default:
                    return CleanModelName(nameWithoutExt);
            }
        }

        private static string ParseDellModelName(string[] pathParts, string nameWithoutExt)
        {
            // Check if this is a Storage subfolder — model name comes from subfolder
            if (pathParts.Length >= 3 &&
                pathParts[1].Equals("Storage", StringComparison.OrdinalIgnoreCase))
            {
                // pathParts[2] is the product subfolder (PowerStore, PowerFlex, AppSync, etc.)
                return pathParts[2];
            }

            // Servers: try to extract PowerEdge model
            var match = Regex.Match(nameWithoutExt, @"poweredge[- ]([a-z]?\d+[a-z]*\d*)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return $"PowerEdge {match.Groups[1].Value.ToUpper()}";
            }

            // PowerEdge series reference (e.g., "poweredge-c-series-...")
            var seriesMatch = Regex.Match(nameWithoutExt, @"poweredge[- ]([a-z]+-series)", RegexOptions.IgnoreCase);
            if (seriesMatch.Success)
            {
                var series = seriesMatch.Groups[1].Value;
                return $"PowerEdge {char.ToUpper(series[0])}{series[1..]}";
            }

            return CleanModelName(nameWithoutExt);
        }

        private static string ParseHpeModelName(string nameWithoutExt)
        {
            // "HPE ProLiant DL380 Gen11 data sheet-PSN..."
            var match = Regex.Match(nameWithoutExt, @"(ProLiant\s+\S+\s+Gen\s*\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value;

            // "HPE ProLiant Compute DL380a Gen12..."
            var computeMatch = Regex.Match(nameWithoutExt, @"(ProLiant\s+Compute\s+\S+(?:\s+Gen\s*\d+)?)", RegexOptions.IgnoreCase);
            if (computeMatch.Success)
                return computeMatch.Groups[1].Value;

            return CleanModelName(nameWithoutExt);
        }

        private static string ParseLenovoModelName(string nameWithoutExt)
        {
            // "ThinkPad_T14_Gen_6_Intel_Spec" → remove _Spec suffix, replace _ with space
            var cleaned = Regex.Replace(nameWithoutExt, @"[_\s]Spec$", "", RegexOptions.IgnoreCase);
            cleaned = cleaned.Replace('_', ' ');
            return cleaned.Trim();
        }

        private static string ParseXFusionModelName(string nameWithoutExt)
        {
            // GUID-like filenames → generic name
            if (Regex.IsMatch(nameWithoutExt, @"^[0-9a-f]{10,}"))
                return "xFusion Document";

            // Otherwise use filename as-is (e.g., "FusionServer V7")
            return nameWithoutExt;
        }

        private static string CleanModelName(string name)
        {
            // Remove common suffixes
            var cleaned = Regex.Replace(name, @"[-_ ](spec[-_ ]?sheet|data[-_ ]?sheet|technical[-_ ]?guide|quick[-_ ]?reference[-_ ]?guide|whitepaper|brochure|overview).*$",
                "", RegexOptions.IgnoreCase);
            // Remove dell-emc- prefix
            cleaned = Regex.Replace(cleaned, @"^dell[-_ ]emc[-_ ]", "", RegexOptions.IgnoreCase);
            // Replace hyphens and underscores with spaces
            cleaned = cleaned.Replace('-', ' ').Replace('_', ' ');
            // Collapse multiple spaces
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            return cleaned.Trim();
        }

        private async Task<Manufacturer> FindOrCreateManufacturerAsync(string name)
        {
            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.Name.ToLower() == name.ToLower());

            if (manufacturer != null)
                return manufacturer;

            manufacturer = new Manufacturer
            {
                Name = name,
                Description = $"Producent {name}"
            };
            _context.Manufacturers.Add(manufacturer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created manufacturer: {Name}", name);
            return manufacturer;
        }

        private async Task<EquipmentType> FindOrCreateEquipmentTypeAsync(string name)
        {
            var equipmentType = await _context.EquipmentTypes
                .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

            if (equipmentType != null)
                return equipmentType;

            equipmentType = new EquipmentType
            {
                Name = name,
                Description = $"Typ sprzętu: {name}"
            };
            _context.EquipmentTypes.Add(equipmentType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created equipment type: {Name}", name);
            return equipmentType;
        }

        private async Task<(EquipmentModel model, bool created)> FindOrCreateEquipmentModelAsync(
            int manufacturerId, int typeId, string modelName)
        {
            var existing = await _context.EquipmentModels
                .FirstOrDefaultAsync(m =>
                    m.ManufacturerId == manufacturerId &&
                    m.ModelName.ToLower() == modelName.ToLower());

            if (existing != null)
                return (existing, false);

            var model = new EquipmentModel
            {
                ManufacturerId = manufacturerId,
                TypeId = typeId,
                ModelName = modelName
            };
            _context.EquipmentModels.Add(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created equipment model: {Model} (manufacturer={MfrId}, type={TypeId})",
                modelName, manufacturerId, typeId);
            return (model, true);
        }
    }
}
