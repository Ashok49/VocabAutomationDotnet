using VocabAutomation.Services.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System;

namespace VocabAutomation.Services
{
    public class DocxParserService : IDocxParserService
    {
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
    }
}
