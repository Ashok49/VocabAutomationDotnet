namespace VocabAutomation.Services.Interfaces
{
    public interface ISendBatchService
    {
        Task<string> ProcessBatchAsync(string tableName);
    }
}
