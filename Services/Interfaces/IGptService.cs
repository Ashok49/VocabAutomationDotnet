using System.Collections.Generic;
using System.Threading.Tasks;

namespace VocabAutomation.Services.Interfaces
{
    public interface IGptService
    {
        Task<string> GenerateStoryAsync(List<(string Word, string Meaning)> vocab, string context = "general");
         Task<byte[]> GenerateSpeechAsync(string text);
    }
}
