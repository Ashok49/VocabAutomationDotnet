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
            var pages = new List<(PdfPage Page, XGraphics Gfx)>();

            var fontCollection = new FontCollection();
            string fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSans-Regular.ttf");

            if (!File.Exists(fontPath))
            {
                _logger.LogError("Font not found at: {0}", fontPath);
            }

            var fontFamily = fontCollection.Add(fontPath);

            var font = new XFont("NotoSans", 12, XFontStyle.Regular);
            var headerFont = new XFont("NotoSans", 14, XFontStyle.Bold);

            double margin = 50;
            double yPoint = 40;

            void AddNewPage()
            {
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);
                pages.Add((page, gfx));
                yPoint = 40;
            }

            AddNewPage(); // start with the first page
            var current = pages.Last();

            // Title
            current.Gfx.DrawString("🧠 Daily Vocabulary Batch", headerFont, XBrushes.Black, new XRect(0, yPoint, current.Page.Width, 20), XStringFormats.TopCenter);
            yPoint += 30;

            // Date
            var today = DateTime.Now.ToString("MMMM dd, yyyy");
            current.Gfx.DrawString($"📅 Date: {today}", font, XBrushes.Black, new XRect(margin, yPoint, current.Page.Width - 2 * margin, 20), XStringFormats.TopLeft);
            yPoint += 30;

            // Vocabulary Section
            current.Gfx.DrawString("📘 Vocabulary Words:", headerFont, XBrushes.DarkBlue, new XRect(margin, yPoint, current.Page.Width - 2 * margin, 20), XStringFormats.TopLeft);
            yPoint += 25;

            foreach (var vocab in vocabList)
            {
                var combinedLine = $"{vocab.Word} — {vocab.Meaning}";
                var wrappedLines = BreakTextIntoLines(current.Gfx, combinedLine, font, current.Page.Width - 2 * margin);

                foreach (var line in wrappedLines)
                {
                    current.Gfx.DrawString(line, font, XBrushes.Black, new XRect(margin, yPoint, current.Page.Width - 2 * margin, 20), XStringFormats.TopLeft);
                    yPoint += 18;

                    if (yPoint > current.Page.Height - 70)
                    {
                        AddNewPage();
                        current = pages.Last();
                    }
                }

                yPoint += 5;
            }

            // Divider line
            yPoint += 10;
            current.Gfx.DrawLine(XPens.DarkGray, margin, yPoint, current.Page.Width - margin, yPoint);
            yPoint += 20;

            // General Story
            current.Gfx.DrawString("📖 General Story:", headerFont, XBrushes.DarkBlue, new XRect(margin, yPoint, current.Page.Width - 2 * margin, 20), XStringFormats.TopLeft);
            yPoint += 25;
            yPoint = DrawWrappedText(current, generalStory, font, yPoint, AddNewPage, () => current = pages.Last());

            // Software Story
            yPoint += 15;
            current.Gfx.DrawString("💻 Software Story:", headerFont, XBrushes.DarkBlue, new XRect(margin, yPoint, current.Page.Width - 2 * margin, 20), XStringFormats.TopLeft);
            yPoint += 25;
            DrawWrappedText(current, softwareStory, font, yPoint, AddNewPage, () => current = pages.Last());

            // Footer: Page numbers
            for (int i = 0; i < pages.Count; i++)
            {
                var (page, gfx) = pages[i];
                gfx.DrawString($"Page {i + 1} of {pages.Count}", font, XBrushes.Gray, new XRect(0, page.Height - 30, page.Width, 20), XStringFormats.Center);
            }

            // Save to stream
            var stream = new MemoryStream();
            document.Save(stream, false);
            stream.Position = 0;
            return stream;
        }


        private double DrawWrappedText((PdfPage Page, XGraphics Gfx) current, string text, XFont font, double startY,
                                    Action addNewPage, Action updateCurrent)
        {
            double lineHeight = 18;
            double margin = 50;
            double maxWidth = current.Page.Width - 2 * margin;
            var lines = BreakTextIntoLines(current.Gfx, text, font, maxWidth);

            double y = startY;
            foreach (var line in lines)
            {
                current.Gfx.DrawString(line, font, XBrushes.Black, new XRect(margin, y, maxWidth, lineHeight), XStringFormats.TopLeft);
                y += lineHeight;

                if (y > current.Page.Height - 70)
                {
                    addNewPage();
                    updateCurrent();
                    y = 40;
                }
            }

            return y;
        }

        private List<string> BreakTextIntoLines(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            foreach (var word in words)
            {
                string testLine = sb.Length == 0 ? word : $"{sb} {word}";
                var size = gfx.MeasureString(testLine, font);

                if (size.Width > maxWidth)
                {
                    // If even a single word is too long, split mid-word
                    if (sb.Length == 0 && word.Length > 10)
                    {
                        result.Add(word); // Add long word as a line
                        continue;
                    }

                    result.Add(sb.ToString());
                    sb.Clear();
                    sb.Append(word);
                }
                else
                {
                    if (sb.Length > 0)
                        sb.Append(' ');
                    sb.Append(word);
                }
            }

            if (sb.Length > 0)
                result.Add(sb.ToString());

            return result;
        }

    }
}

