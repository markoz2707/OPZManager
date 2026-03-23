using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OPZManager.API.Services
{
    public class DocxExportService : IDocxExportService
    {
        private readonly ILogger<DocxExportService> _logger;

        // Polish document standards
        private const string BodyFontName = "Times New Roman";
        private const string BodyFontSize = "24";   // half-points: 24 = 12pt
        private const string HeadingFontSize = "28"; // 14pt
        private const string TitleFontSize = "32";   // 16pt
        private const int LineSpacing = 276;          // 1.15 line spacing (240 * 1.15)

        public DocxExportService(ILogger<DocxExportService> logger)
        {
            _logger = logger;
        }

        public Task<byte[]> GenerateDocxAsync(string content, string title)
        {
            using var stream = new MemoryStream();
            using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Section properties with 2.5cm margins (Polish standard)
                var sectionProps = new SectionProperties();
                var pageMargin = new PageMargin
                {
                    Top = 1417,    // 2.5cm in twips
                    Right = (UInt32Value)1417u,
                    Bottom = 1417,
                    Left = (UInt32Value)1417u,
                    Header = (UInt32Value)708u,
                    Footer = (UInt32Value)708u
                };
                sectionProps.Append(pageMargin);

                // Add header and footer
                AddFooter(mainPart, sectionProps);
                AddHeader(mainPart, sectionProps, title);

                // Document title
                AddTitle(body, title);

                // Generation date
                AddDateParagraph(body);

                // Separator
                AddEmptyParagraph(body);

                // Parse and render content
                ParseAndAddContent(body, content);

                // Section properties must be last child of body
                body.Append(sectionProps);
            }

            return Task.FromResult(stream.ToArray());
        }

        public Task<byte[]> GenerateSWZDocxAsync(SWZDocumentDto document)
        {
            using var stream = new MemoryStream();
            using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                var sectionProps = new SectionProperties();
                var pageMargin = new PageMargin
                {
                    Top = 1417,
                    Right = (UInt32Value)1417u,
                    Bottom = 1417,
                    Left = (UInt32Value)1417u,
                    Header = (UInt32Value)708u,
                    Footer = (UInt32Value)708u
                };
                sectionProps.Append(pageMargin);

                AddFooter(mainPart, sectionProps);
                AddHeader(mainPart, sectionProps, document.Title);

                // SWZ title page
                AddTitle(body, "SPECYFIKACJA WARUNKÓW ZAMÓWIENIA");
                AddEmptyParagraph(body);

                if (!string.IsNullOrWhiteSpace(document.InstitutionName))
                    AddCenteredParagraph(body, document.InstitutionName, BodyFontSize, false);

                if (!string.IsNullOrWhiteSpace(document.ProcedureNumber))
                    AddCenteredParagraph(body, $"Nr postępowania: {document.ProcedureNumber}", BodyFontSize, false);

                AddEmptyParagraph(body);
                AddCenteredParagraph(body, document.Title, HeadingFontSize, true);
                AddEmptyParagraph(body);

                ParseAndAddContent(body, document.Content);

                body.Append(sectionProps);
            }

            return Task.FromResult(stream.ToArray());
        }

        // ──────────────────────────────────────────────
        //  Content parsing
        // ──────────────────────────────────────────────

        private void ParseAndAddContent(Body body, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var mainSectionPattern = new Regex(@"^(\d+)\.\s+(.+)$");
            var subSectionPattern = new Regex(@"^(\d+\.\d+)\.\s+(.+)$");
            var romanSectionPattern = new Regex(@"^(I{1,3}|IV|V|VI{0,3}|IX|X{0,3})\.\s+(.+)$", RegexOptions.IgnoreCase);
            var bulletPattern = new Regex(@"^[\s]*[-*•–]\s+(.+)$");
            var numberedBulletPattern = new Regex(@"^[\s]*[a-z]\)\s+(.+)$");
            var separatorPattern = new Regex(@"^[=\-]{3,}$");
            var tableRowPattern = new Regex(@"\|.*\|");

            var inTable = false;
            var tableRows = new List<string[]>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();

                // Empty lines
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    if (inTable && tableRows.Count > 0)
                    {
                        AddTable(body, tableRows);
                        tableRows.Clear();
                        inTable = false;
                    }
                    AddEmptyParagraph(body);
                    continue;
                }

                // Separators (===, ---)
                if (separatorPattern.IsMatch(trimmed))
                    continue;

                // Table rows (| col1 | col2 |)
                if (tableRowPattern.IsMatch(trimmed))
                {
                    // Skip separator rows like |---|---|
                    if (Regex.IsMatch(trimmed, @"^\|[\s\-:]+\|"))
                        continue;

                    inTable = true;
                    var cells = trimmed.Split('|', StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .ToArray();
                    if (cells.Length > 0)
                        tableRows.Add(cells);
                    continue;
                }

                // Flush pending table when hitting a non-table line
                if (inTable && tableRows.Count > 0)
                {
                    AddTable(body, tableRows);
                    tableRows.Clear();
                    inTable = false;
                }

                // Sub-section (2.1. Title) — check before main section
                if (subSectionPattern.IsMatch(trimmed))
                {
                    AddSubSectionHeading(body, trimmed);
                    continue;
                }

                // Main section (1. TITLE)
                var mainMatch = mainSectionPattern.Match(trimmed);
                if (mainMatch.Success && IsLikelyHeader(mainMatch.Groups[2].Value))
                {
                    AddSectionHeading(body, trimmed);
                    continue;
                }

                // Roman numeral section (I. TITLE)
                var romanMatch = romanSectionPattern.Match(trimmed);
                if (romanMatch.Success && IsLikelyHeader(romanMatch.Groups[2].Value))
                {
                    AddSectionHeading(body, trimmed);
                    continue;
                }

                // Bullet points (- item, * item, • item)
                var bulletMatch = bulletPattern.Match(trimmed);
                if (bulletMatch.Success)
                {
                    AddBulletPoint(body, bulletMatch.Groups[1].Value);
                    continue;
                }

                // Lettered bullets (a) item, b) item)
                if (numberedBulletPattern.IsMatch(trimmed))
                {
                    AddBulletPoint(body, trimmed, indentLevel: 1);
                    continue;
                }

                // ALL-CAPS lines (sub-headers)
                if (trimmed.Length > 5 && trimmed == trimmed.ToUpper() && trimmed.Any(char.IsLetter))
                {
                    AddSubSectionHeading(body, trimmed);
                    continue;
                }

                // Regular body text
                AddBodyParagraph(body, trimmed);
            }

            // Flush any remaining table
            if (inTable && tableRows.Count > 0)
                AddTable(body, tableRows);
        }

        private static bool IsLikelyHeader(string text)
        {
            if (text.Length > 200) return false;
            if (text == text.ToUpper() && text.Length > 3) return true;

            var headerKeywords = new[]
            {
                "PRZEDMIOT", "ZAMÓWIENI", "SPECYFIKACJA", "WYMAGANI", "WARUNKI",
                "KRYTERIA", "CERTYFIKAT", "ZGODNOŚĆ", "GWARANCJ", "REALIZACJ",
                "OCEN", "WYKONAWC", "TECHNICZN", "BEZPIECZEŃ", "ŚRODOWISK",
                "DODATKOW", "DOPUSZCZALN", "WYDAJNOŚĆ", "SERWIS", "DOSTAW"
            };
            var upper = text.ToUpper();
            return headerKeywords.Any(kw => upper.Contains(kw));
        }

        // ──────────────────────────────────────────────
        //  Paragraph helpers
        // ──────────────────────────────────────────────

        private static void AddTitle(Body body, string title)
        {
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();
            pProps.Append(new Justification { Val = JustificationValues.Center });
            pProps.Append(new SpacingBetweenLines { After = "240", Line = LineSpacing.ToString(), LineRule = LineSpacingRuleValues.Auto });
            paragraph.Append(pProps);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            runProps.Append(new FontSize { Val = TitleFontSize });
            runProps.Append(new Bold());
            run.Append(runProps);
            run.Append(new Text(title));
            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static void AddDateParagraph(Body body)
        {
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();
            pProps.Append(new Justification { Val = JustificationValues.Right });
            pProps.Append(new SpacingBetweenLines { After = "120", Line = LineSpacing.ToString(), LineRule = LineSpacingRuleValues.Auto });
            paragraph.Append(pProps);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            runProps.Append(new FontSize { Val = "20" }); // 10pt
            runProps.Append(new Italic());
            run.Append(runProps);
            run.Append(new Text($"Data wygenerowania: {DateTime.Now:dd.MM.yyyy}"));
            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static void AddSectionHeading(Body body, string text)
        {
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();
            pProps.Append(new SpacingBetweenLines { Before = "360", After = "120", Line = LineSpacing.ToString(), LineRule = LineSpacingRuleValues.Auto });
            pProps.Append(new KeepNext());
            paragraph.Append(pProps);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            runProps.Append(new FontSize { Val = HeadingFontSize });
            runProps.Append(new Bold());
            run.Append(runProps);
            run.Append(new Text(text));
            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static void AddSubSectionHeading(Body body, string text)
        {
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();
            pProps.Append(new SpacingBetweenLines { Before = "240", After = "120", Line = LineSpacing.ToString(), LineRule = LineSpacingRuleValues.Auto });
            pProps.Append(new KeepNext());
            paragraph.Append(pProps);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            runProps.Append(new FontSize { Val = BodyFontSize });
            runProps.Append(new Bold());
            run.Append(runProps);
            run.Append(new Text(text));
            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static void AddBodyParagraph(Body body, string text)
        {
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();
            pProps.Append(new SpacingBetweenLines { After = "60", Line = LineSpacing.ToString(), LineRule = LineSpacingRuleValues.Auto });
            pProps.Append(new Justification { Val = JustificationValues.Both });
            paragraph.Append(pProps);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            runProps.Append(new FontSize { Val = BodyFontSize });
            run.Append(runProps);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static void AddBulletPoint(Body body, string text, int indentLevel = 0)
        {
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();

            var indentValue = 720 + (indentLevel * 360);
            pProps.Append(new Indentation { Left = indentValue.ToString(), Hanging = "360" });
            pProps.Append(new SpacingBetweenLines { After = "40", Line = LineSpacing.ToString(), LineRule = LineSpacingRuleValues.Auto });
            pProps.Append(new Justification { Val = JustificationValues.Both });
            paragraph.Append(pProps);

            // Bullet character
            var bulletRun = new Run();
            var bulletRunProps = new RunProperties();
            bulletRunProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            bulletRunProps.Append(new FontSize { Val = BodyFontSize });
            bulletRun.Append(bulletRunProps);
            bulletRun.Append(new Text("\u2022 ") { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(bulletRun);

            // Content
            var contentRun = new Run();
            var contentRunProps = new RunProperties();
            contentRunProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            contentRunProps.Append(new FontSize { Val = BodyFontSize });
            contentRun.Append(contentRunProps);
            contentRun.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(contentRun);

            body.Append(paragraph);
        }

        private static void AddCenteredParagraph(Body body, string text, string fontSize, bool bold)
        {
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();
            pProps.Append(new Justification { Val = JustificationValues.Center });
            pProps.Append(new SpacingBetweenLines { After = "120", Line = LineSpacing.ToString(), LineRule = LineSpacingRuleValues.Auto });
            paragraph.Append(pProps);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            runProps.Append(new FontSize { Val = fontSize });
            if (bold) runProps.Append(new Bold());
            run.Append(runProps);
            run.Append(new Text(text));
            paragraph.Append(run);

            body.Append(paragraph);
        }

        private static void AddEmptyParagraph(Body body)
        {
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();
            pProps.Append(new SpacingBetweenLines { After = "0", Line = "120", LineRule = LineSpacingRuleValues.Auto });
            paragraph.Append(pProps);
            body.Append(paragraph);
        }

        // ──────────────────────────────────────────────
        //  Table rendering
        // ──────────────────────────────────────────────

        private static void AddTable(Body body, List<string[]> rows)
        {
            if (rows.Count == 0) return;

            var table = new Table();

            var tblProps = new TableProperties();
            var tblBorders = new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
            );
            tblProps.Append(tblBorders);
            tblProps.Append(new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });
            table.Append(tblProps);

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var tableRow = new TableRow();
                var cells = rows[rowIndex];
                var isHeader = rowIndex == 0;

                foreach (var cellText in cells)
                {
                    var cell = new TableCell();
                    var cellProps = new TableCellProperties();
                    cellProps.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });

                    if (isHeader)
                    {
                        cellProps.Append(new Shading
                        {
                            Val = ShadingPatternValues.Clear,
                            Fill = "D9E2F3"
                        });
                    }

                    cell.Append(cellProps);

                    var paragraph = new Paragraph();
                    var pProps = new ParagraphProperties();
                    pProps.Append(new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto });
                    if (isHeader) pProps.Append(new Justification { Val = JustificationValues.Center });
                    paragraph.Append(pProps);

                    var run = new Run();
                    var runProps = new RunProperties();
                    runProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
                    runProps.Append(new FontSize { Val = "20" }); // 10pt for table cells
                    if (isHeader) runProps.Append(new Bold());
                    run.Append(runProps);
                    run.Append(new Text(cellText) { Space = SpaceProcessingModeValues.Preserve });
                    paragraph.Append(run);

                    cell.Append(paragraph);
                    tableRow.Append(cell);
                }

                table.Append(tableRow);
            }

            body.Append(table);
            AddEmptyParagraph(body);
        }

        // ──────────────────────────────────────────────
        //  Header & Footer
        // ──────────────────────────────────────────────

        private static void AddHeader(MainDocumentPart mainPart, SectionProperties sectionProps, string title)
        {
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            var headerPartId = mainPart.GetIdOfPart(headerPart);

            var header = new Header();
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();
            pProps.Append(new Justification { Val = JustificationValues.Left });

            var pBorders = new ParagraphBorders();
            pBorders.Append(new BottomBorder { Val = BorderValues.Single, Size = 4, Space = 1, Color = "808080" });
            pProps.Append(pBorders);
            paragraph.Append(pProps);

            // Document title
            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            runProps.Append(new FontSize { Val = "18" }); // 9pt
            runProps.Append(new Color { Val = "808080" });
            run.Append(runProps);
            run.Append(new Text(title) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(run);

            // Date alongside title
            var dateRun = new Run();
            var dateRunProps = new RunProperties();
            dateRunProps.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            dateRunProps.Append(new FontSize { Val = "18" });
            dateRunProps.Append(new Color { Val = "808080" });
            dateRun.Append(dateRunProps);
            dateRun.Append(new Text($"  |  {DateTime.Now:dd.MM.yyyy}") { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(dateRun);

            header.Append(paragraph);
            headerPart.Header = header;

            sectionProps.Append(new HeaderReference { Type = HeaderFooterValues.Default, Id = headerPartId });
        }

        private static void AddFooter(MainDocumentPart mainPart, SectionProperties sectionProps)
        {
            var footerPart = mainPart.AddNewPart<FooterPart>();
            var footerPartId = mainPart.GetIdOfPart(footerPart);

            var footer = new Footer();
            var paragraph = new Paragraph();
            var pProps = new ParagraphProperties();
            pProps.Append(new Justification { Val = JustificationValues.Center });

            var pBorders = new ParagraphBorders();
            pBorders.Append(new TopBorder { Val = BorderValues.Single, Size = 4, Space = 1, Color = "808080" });
            pProps.Append(pBorders);
            paragraph.Append(pProps);

            // "Strona " text
            var run1 = new Run();
            var run1Props = new RunProperties();
            run1Props.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            run1Props.Append(new FontSize { Val = "18" });
            run1Props.Append(new Color { Val = "808080" });
            run1.Append(run1Props);
            run1.Append(new Text("Strona ") { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(run1);

            // PAGE field code
            paragraph.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            paragraph.Append(new Run(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }));
            paragraph.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

            // " z " text
            var run2 = new Run();
            var run2Props = new RunProperties();
            run2Props.Append(new RunFonts { Ascii = BodyFontName, HighAnsi = BodyFontName });
            run2Props.Append(new FontSize { Val = "18" });
            run2Props.Append(new Color { Val = "808080" });
            run2.Append(run2Props);
            run2.Append(new Text(" z ") { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(run2);

            // NUMPAGES field code
            paragraph.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            paragraph.Append(new Run(new FieldCode(" NUMPAGES ") { Space = SpaceProcessingModeValues.Preserve }));
            paragraph.Append(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

            footer.Append(paragraph);
            footerPart.Footer = footer;

            sectionProps.Append(new FooterReference { Type = HeaderFooterValues.Default, Id = footerPartId });
        }
    }
}
