using System.Collections.Generic;
using System.Threading.Tasks;

namespace VocabAutomation.Services.Interfaces
{
    public interface IVocabStorageService
    {
        Task StoreVocabularyAsync(List<(string Word, string Meaning)> entries, string tableName);
    }
}
