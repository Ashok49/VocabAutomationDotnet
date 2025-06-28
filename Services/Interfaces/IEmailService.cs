using System.Collections.Generic;
using System.Threading.Tasks;
using VocabAutomation.Models;

namespace VocabAutomation.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendVocabEmailAsync(
            List<VocabEntry> vocabList,
            string generalStory,
            string softwareStory,
            string subject);

        Task SendVocabEmailAsync(List<WordMeaning> vocabList, string pdfUrl, string subject);

    }

    
}
