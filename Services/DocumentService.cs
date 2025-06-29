using VocabAutomation.Services.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SixLabors.Fonts;
using System.Text;
using VocabAutomation.Models;

namespace VocabAutomation.Services
{
    public class DocumentService : IDocumentService
    {

        private readonly ILogger<DocumentService> _logger;

        public DocumentService(ILogger<DocumentService> logger)
        {
            _logger = logger;
        }

        public List<(string Word, string Meaning)> ExtractWordMeanings(byte[] docxContent)
        {
            var wordMeanings = new List<(string Word, string Meaning)>();

            try
            {
                using var stream = new MemoryStream(docxContent);
                using var document = WordprocessingDocument.Open(stream, false);

                var body = document.MainDocumentPart.Document.Body;
                var text = string.Join('\n', body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
                                                .Select(p => p.InnerText.Trim()));

                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var cleanLine = line.Trim();

                    if (string.IsNullOrWhiteSpace(cleanLine)) continue;
                    if (!cleanLine.Contains(':')) continue;
                    if (cleanLine.Count(c => c == '-') > 5) continue;

                    var parts = cleanLine.Split(':', 2);
                    var word = parts[0].Trim();
                    var meaning = parts[1].Trim();

                    if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(meaning))
                    {
                        wordMeanings.Add((word, meaning));
                    }
                }

                // Remove duplicates by keeping the last meaning for each word
                wordMeanings = wordMeanings
                    .GroupBy(w => w.Word.ToLower())
                    .Select(g => g.Last())
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error parsing .docx: {ex.Message}");
            }

            return wordMeanings;
        }

        public async Task<MemoryStream> CreatePdfAsync(
            List<VocabEntry> vocabList,
            string generalStory,
            string softwareStory)
        {
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            var fontCollection = new FontCollection();

            //var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "LiberationSans-Regular.ttf");
            
            string fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Regular.ttf");


            if (!File.Exists(fontPath))
            {
                _logger.LogError("Font not found at: {0}", fontPath);
            }

            var fontFamily = fontCollection.Add(fontPath);

            var font = new XFont(fontFamily.Name, 14, XFontStyle.Regular);
            double yPoint = 40;

            // Title
            gfx.DrawString("🧠 Daily Vocabulary Batch", font, XBrushes.Black, new XRect(0, yPoint, page.Width, 20), XStringFormats.TopCenter);
            yPoint += 40;

            // Vocabulary list
            gfx.DrawString("📘 Vocabulary Words:", font, XBrushes.DarkBlue, new XRect(40, yPoint, page.Width - 80, 20), XStringFormats.TopLeft);
            yPoint += 30;

            foreach (var vocab in vocabList)
            {
                gfx.DrawString($"{vocab.Word} — {vocab.Meaning}", font, XBrushes.Black, new XRect(50, yPoint, page.Width - 100, 20), XStringFormats.TopLeft);
                yPoint += 25;
                if (yPoint > page.Height - 100)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPoint = 40;
                }
            }

            // General Story
            yPoint += 20;
            gfx.DrawString("📖 General Story:", font, XBrushes.DarkBlue, new XRect(40, yPoint, page.Width - 80, 20), XStringFormats.TopLeft);
            yPoint += 30;
            yPoint = DrawWrappedText(gfx, generalStory, font, page, yPoint);

            // Software Story
            yPoint += 20;
            gfx.DrawString("💻 Software Story:", font, XBrushes.DarkBlue, new XRect(40, yPoint, page.Width - 80, 20), XStringFormats.TopLeft);
            yPoint += 30;
            DrawWrappedText(gfx, softwareStory, font, page, yPoint);

            // Save to memory stream
            var stream = new MemoryStream();
            document.Save(stream, false);
            stream.Position = 0;
            return stream;
        }

        private double DrawWrappedText(XGraphics gfx, string text, XFont font, PdfPage page, double startY)
        {
            double lineHeight = 20;
            double maxWidth = page.Width - 100;
            var lines = BreakTextIntoLines(gfx, text, font, maxWidth);

            double y = startY;
            foreach (var line in lines)
            {
                gfx.DrawString(line, font, XBrushes.Black, new XRect(50, y, maxWidth, lineHeight), XStringFormats.TopLeft);
                y += lineHeight;

                if (y > page.Height - 100)
                {
                    page = page.Owner.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 40;
                }
            }

            return y;
        }

        private List<string> BreakTextIntoLines(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            var result = new List<string>();
            var words = text.Split(' ');
            var sb = new StringBuilder();

            foreach (var word in words)
            {
                var testLine = sb.Length == 0 ? word : $"{sb} {word}";
                var size = gfx.MeasureString(testLine, font);

                if (size.Width > maxWidth)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    sb.Append(word);
                }
                else
                {
                    sb.Append(sb.Length == 0 ? word : $" {word}");
                }
            }

            if (sb.Length > 0)
                result.Add(sb.ToString());

            return result;
        }
    }
}

