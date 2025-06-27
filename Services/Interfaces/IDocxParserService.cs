using System.Collections.Generic;

namespace VocabAutomation.Services.Interfaces
{
    public interface IDocxParserService
    {
        List<(string Word, string Meaning)> ExtractWordMeanings(byte[] docxContent);
    }
}
