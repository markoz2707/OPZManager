using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace OPZManager.API.Services
{
    public class DocxExportService : IDocxExportService
    {
        public Task<byte[]> GenerateDocxAsync(string content, string title)
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Page settings: A4, 2.5cm margins
                var sectionProps = new SectionProperties(
                    new PageSize { Width = 11906, Height = 16838 }, // A4
                    new PageMargin
                    {
                        Top = 1417, Bottom = 1417, Left = 1417, Right = 1417, // ~2.5cm
                        Header = 708, Footer = 708
                    }
                );

                // Add styles
                var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
                stylesPart.Styles = CreateStyles();

                // Add header
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                CreateHeader(headerPart, title);
                sectionProps.AppendChild(new HeaderReference
                {
                    Type = HeaderFooterValues.Default,
                    Id = mainPart.GetIdOfPart(headerPart)
                });

                // Add footer with page numbers
                var footerPart = mainPart.AddNewPart<FooterPart>();
                CreateFooter(footerPart);
                sectionProps.AppendChild(new FooterReference
                {
                    Type = HeaderFooterValues.Default,
                    Id = mainPart.GetIdOfPart(footerPart)
                });

                // Title
                body.AppendChild(CreateParagraph(title, "Heading1", true, "28", JustificationValues.Center));
                body.AppendChild(CreateParagraph("", null, false, "24", null));

                // Parse and add content
                var lines = content.Split('\n');
                foreach (var line in lines)
                {
                    var trimmed = line.TrimEnd('\r');

                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        body.AppendChild(CreateParagraph("", null, false, "24", null));
                        continue;
                    }

                    // Detect section headers (numbered: "1.", "2.", "I.", "II.", or ALL CAPS)
                    if (IsMainHeader(trimmed))
                    {
                        body.AppendChild(CreateParagraph(trimmed, "Heading2", true, "26", null));
                    }
                    else if (IsSubHeader(trimmed))
                    {
                        body.AppendChild(CreateParagraph(trimmed, "Heading3", true, "24", null));
                    }
                    else if (trimmed.StartsWith("- ") || trimmed.StartsWith("• "))
                    {
                        // Bullet point
                        var text = trimmed.TrimStart('-', '•', ' ');
                        var para = CreateParagraph(text, null, false, "24", null);
                        para.ParagraphProperties ??= new ParagraphProperties();
                        para.ParagraphProperties.Indentation = new Indentation { Left = "720" };
                        // Add bullet character
                        para.InsertBefore(new Run(new Text("• ") { Space = SpaceProcessingModeValues.Preserve }), para.GetFirstChild<Run>());
                        body.AppendChild(para);
                    }
                    else
                    {
                        body.AppendChild(CreateParagraph(trimmed, null, false, "24", JustificationValues.Both));
                    }
                }

                body.AppendChild(sectionProps);
                mainPart.Document.Save();
            }

            return Task.FromResult(stream.ToArray());
        }

        private static bool IsMainHeader(string line)
        {
            // Matches "1.", "2.", "I.", "II.", "III." at start, or ALL CAPS lines
            if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^(\d+\.|[IVXLC]+\.)\s"))
                return true;
            if (line.Length > 5 && line.Length < 100 && line == line.ToUpper() && !line.Contains("- "))
                return true;
            return false;
        }

        private static bool IsSubHeader(string line)
        {
            // Matches "1.1", "1.2.", "a)", "b)" at start
            return System.Text.RegularExpressions.Regex.IsMatch(line, @"^(\d+\.\d+\.?|\d+\.\d+\.\d+\.?|[a-z]\))\s");
        }

        private static Paragraph CreateParagraph(string text, string? styleId, bool bold, string fontSize, JustificationValues? justification)
        {
            var para = new Paragraph();
            var props = new ParagraphProperties();

            if (styleId != null)
                props.ParagraphStyleId = new ParagraphStyleId { Val = styleId };
            if (justification != null)
                props.Justification = new Justification { Val = justification };

            props.SpacingBetweenLines = new SpacingBetweenLines { After = "120", Line = "276", LineRule = LineSpacingRuleValues.Auto }; // 1.15 spacing

            para.AppendChild(props);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.AppendChild(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });
            runProps.AppendChild(new FontSize { Val = fontSize });
            if (bold) runProps.AppendChild(new Bold());
            run.AppendChild(runProps);
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            para.AppendChild(run);
            return para;
        }

        private static Styles CreateStyles()
        {
            var styles = new Styles();

            // Default style
            var defaultStyle = new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "Normal",
                Default = true
            };
            defaultStyle.AppendChild(new StyleName { Val = "Normal" });
            defaultStyle.AppendChild(new StyleRunProperties(
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new FontSize { Val = "24" } // 12pt
            ));
            styles.AppendChild(defaultStyle);

            // Heading 1
            var h1 = new Style { Type = StyleValues.Paragraph, StyleId = "Heading1" };
            h1.AppendChild(new StyleName { Val = "heading 1" });
            h1.AppendChild(new StyleRunProperties(
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new FontSize { Val = "28" }, // 14pt
                new Bold()
            ));
            styles.AppendChild(h1);

            // Heading 2
            var h2 = new Style { Type = StyleValues.Paragraph, StyleId = "Heading2" };
            h2.AppendChild(new StyleName { Val = "heading 2" });
            h2.AppendChild(new StyleRunProperties(
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new FontSize { Val = "26" }, // 13pt
                new Bold()
            ));
            styles.AppendChild(h2);

            // Heading 3
            var h3 = new Style { Type = StyleValues.Paragraph, StyleId = "Heading3" };
            h3.AppendChild(new StyleName { Val = "heading 3" });
            h3.AppendChild(new StyleRunProperties(
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new FontSize { Val = "24" }, // 12pt
                new Bold()
            ));
            styles.AppendChild(h3);

            return styles;
        }

        private static void CreateHeader(HeaderPart headerPart, string title)
        {
            var header = new Header();
            var para = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Right },
                    new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "999999" })
                ),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = "18" },
                        new Color { Val = "666666" }
                    ),
                    new Text(title) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            header.AppendChild(para);
            headerPart.Header = header;
        }

        private static void CreateFooter(FooterPart footerPart)
        {
            var footer = new Footer();
            var para = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new ParagraphBorders(new TopBorder { Val = BorderValues.Single, Size = 4, Color = "999999" })
                ),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = "18" },
                        new Color { Val = "666666" }
                    ),
                    new Text("Strona ") { Space = SpaceProcessingModeValues.Preserve }
                ),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = "18" }
                    ),
                    new FieldChar { FieldCharType = FieldCharValues.Begin }
                ),
                new Run(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = "18" },
                        new Color { Val = "666666" }
                    ),
                    new Text(" z ") { Space = SpaceProcessingModeValues.Preserve }
                ),
                new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
                new Run(new FieldCode(" NUMPAGES ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new FieldChar { FieldCharType = FieldCharValues.End })
            );
            footer.AppendChild(para);
            footerPart.Footer = footer;
        }
    }
}
