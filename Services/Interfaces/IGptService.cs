using System.Collections.Generic;
using System.Threading.Tasks;
using VocabAutomation.Models;

namespace VocabAutomation.Services.Interfaces
{
    public interface IGptService
    {
        Task<string> GenerateStoryAsync(List<VocabEntry> vocab, string context = "general");
         Task<string> GenerateSpeechAsync(string text, string fileName);
    }
}
