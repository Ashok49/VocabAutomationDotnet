using System.Collections.Generic;
using System.Threading.Tasks;
using VocabAutomation.Models;

namespace VocabAutomation.Services.Interfaces
{
    public interface IVocabService
    {
        Task StoreVocabularyAsync(List<(string Word, string Meaning)> entries, string tableName);
        Task<(List<VocabEntry> Words, bool FromToday)> GetVocabBatchAsync(string tableName);
        Task MarkWordsAsSentAsync(List<int> ids, string tableName);
    }
}
