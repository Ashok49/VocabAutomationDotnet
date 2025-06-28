using System.Collections.Generic;
using System.Threading.Tasks;

namespace VocabAutomation.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendVocabEmailAsync(
            List<(string Word, string Meaning)> vocabList,
            string generalStory,
            string softwareStory,
            string subject);
    }
}
