using System.Collections.Generic;
using VocabAutomation.Models;

namespace VocabAutomation.Services.Interfaces
{
    public interface IDocumentService
    {
        List<(string Word, string Meaning)> ExtractWordMeanings(byte[] docxContent);

        // Generate a PDF given vocab list and stories
        Task<MemoryStream>CreatePdfAsync(
            List<VocabEntry> vocabList,
            string generalStory,
            string softwareStory);

    }
}
