using VocabAutomation.Models;

namespace VocabAutomation.Services.Interfaces
{
    public interface ISendBatchService
    {
        Task<string> ProcessBatchAsync(string tableName);
        Task<(bool Exists, DailyBatchRecord Record)> GetTodayBatchAsync(string tableName);
        Task SaveBatchRecordAsync(SaveBatchRequest request);

    }
}
