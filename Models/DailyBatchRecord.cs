using System.Text.Json;

namespace VocabAutomation.Models
{
    public class DailyBatchRecord
    {
        public int Id { get; set; }
        public DateTime RunDate { get; set; }

        public required string TableName { get; set; }
        public required string PdfUrl { get; set; }
        public required string AudioUrl { get; set; }

        public required string WordsJson { get; set; }

        public List<WordMeaning> GetWords()
        {
            return JsonSerializer.Deserialize<List<WordMeaning>>(WordsJson) ?? new();
        }

        public static string ToJson(List<WordMeaning> words)
        {
            return JsonSerializer.Serialize(words);
        }
    }
}
