using System.Collections.Generic;
using System.Threading.Tasks;

namespace VocabAutomation.Services.Interfaces
{
    public interface IVocabService
    {
        Task StoreVocabularyAsync(List<(string Word, string Meaning)> entries, string tableName);
        Task<List<(int Id, string Word, string Meaning)>> GetVocabBatchAsync(string tableName);
        Task MarkWordsAsSentAsync(List<int> ids, string tableName);
    }
}
