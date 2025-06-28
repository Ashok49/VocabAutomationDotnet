namespace VocabAutomation.Models
{
    public class WordMeaning
    {
        public string Word { get; set; } = default!;
        public string Meaning { get; set; } = default!;
    }

    public class SaveBatchRequest
    {
        public string TableName { get; set; } = default!;
        public List<WordMeaning> Words { get; set; } = new();
        public string PdfUrl { get; set; } = default!;
        public string AudioUrl { get; set; } = default!;
    }
}
