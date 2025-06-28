using Microsoft.AspNetCore.Http.Features;
using Npgsql;
using VocabAutomation.Models;
using VocabAutomation.Services.Interfaces;

namespace VocabAutomation.Services
{
    public class SendBatchService : ISendBatchService
    {
        private readonly IVocabService _vocabService;
        private readonly IGptService _gptService;
        private readonly IEmailService _emailService;
        private readonly IS3Service _s3Service;
        private readonly ITwilioService _twilioService;
        private readonly IDocumentService _docService;
        private readonly ILogger<SendBatchService> _logger;
        private readonly string _connectionString;

        public SendBatchService(
            IVocabService vocabService,
            IGptService gptService,
            IEmailService emailService,
            IS3Service s3Service,
            ITwilioService twilioService,
            IConfiguration configuration,
            IDocumentService documentService,
            ILogger<SendBatchService> logger)
        {
            _vocabService = vocabService;
            _gptService = gptService;
            _emailService = emailService;
            _s3Service = s3Service;
            _twilioService = twilioService;
            _logger = logger;
            _docService = documentService;
            _connectionString = configuration["POSTGRES_CONN"];
        }

public async Task<string> ProcessBatchAsync(string tableName)
{
    try
    {
        // Step 1: Check for existing batch
        var (exists, record) = await GetTodayBatchAsync(tableName);
        if (exists && record is not null)
        {
            _logger.LogInformation("♻️ Reusing today's batch for table: {Table}", tableName);

            var vocabList1 = record.GetWords();  // Deserialize from JSON
            var subject = $"🧠 Your Daily Vocabulary - {DateTime.Now:MMM dd}";

            await _emailService.SendVocabEmailAsync(vocabList1, record.PdfUrl, subject); // Assumes overloaded method
            await _twilioService.MakeCallAsync(record.AudioUrl);

            return $"♻️ Batch reused and sent for {tableName}.";
        }

        // Step 2: Get new words
        var (vocabList, fromToday) = await _vocabService.GetVocabBatchAsync(tableName);
        if (vocabList.Count == 0)
        {
            _logger.LogInformation("📭 No words to send for table: {Table}", tableName);
            return $"📭 No words to send for {tableName}.";
        }

        // Step 3: Generate stories
        var generalStory = await _gptService.GenerateStoryAsync(vocabList, "general");
        var softwareStory = await _gptService.GenerateStoryAsync(vocabList, "software");

        // Step 4: Create and upload PDF
        var pdfStream = await _docService.CreatePdfAsync(vocabList, generalStory, softwareStory);
        var pdfUrl = await _s3Service.UploadPdfAsync(pdfStream,$"vocab_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");

        // Step 5: Generate and upload audio
        var combinedText = BuildCombinedText(vocabList, generalStory, softwareStory);
        var audiofileName = $"audio_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp3";
        var audioPath = await _gptService.GenerateSpeechAsync(combinedText,audiofileName);
        var audioUrl = await _s3Service.UploadAudioAsync(audioPath,audiofileName);

        // Step 6: Send email and make Twilio call
        var subjectLine = $"🧠 Your Daily Vocabulary - {DateTime.Now:MMM dd}";
        await _emailService.SendVocabEmailAsync(vocabList, generalStory, softwareStory, subjectLine);
        await _twilioService.MakeCallAsync(audioUrl);

        // Step 7: Save new batch record
        await SaveBatchRecordAsync(new SaveBatchRequest
        {
            TableName = tableName,
            Words = [.. vocabList.Select(v => new WordMeaning
            {
                Word = v.Word,
                Meaning = v.Meaning
            })],
            PdfUrl = pdfUrl,
            AudioUrl = audioUrl
        });

        _logger.LogInformation("✅ New vocab batch processed and saved for table: {Table}", tableName);
        return $"✅ New vocab batch sent and saved for {tableName}.";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error while processing batch for {Table}", tableName);
        return $"❌ Failed to process vocab batch for {tableName}.";
    }
}


public async Task<(bool Exists, DailyBatchRecord? Record)> GetTodayBatchAsync(string tableName)
{
    try
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, run_date, table_name, pdf_url, audio_url, words_json
            FROM daily_vocab_batches
            WHERE run_date::date = CURRENT_DATE AND table_name = @table;";
        cmd.Parameters.AddWithValue("table", tableName);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var record = new DailyBatchRecord
            {
                Id = reader.GetInt32(0),
                RunDate = reader.GetDateTime(1),
                TableName = reader.GetString(2),
                PdfUrl = reader.GetString(3),
                AudioUrl = reader.GetString(4),
                WordsJson = reader.GetString(5)
            };
            return (true, record);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Failed to fetch today's batch for {Table}", tableName);
    }

    return (false, null);
}


        public async Task SaveBatchRecordAsync(SaveBatchRequest request)
        {
            try
            {

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO daily_vocab_batches (run_date, table_name, pdf_url, audio_url, words_json)
                VALUES (@run_date, @table_name, @pdf_url, @audio_url, @words_json);";

            cmd.Parameters.AddWithValue("run_date", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("table_name", request.TableName);
            cmd.Parameters.AddWithValue("pdf_url", request.PdfUrl);
            cmd.Parameters.AddWithValue("audio_url", request.AudioUrl);
            cmd.Parameters.AddWithValue("words_json", DailyBatchRecord.ToJson(request.Words ?? new()));
            await cmd.ExecuteNonQueryAsync();


                _logger.LogInformation("📌 Batch record saved for table: {TableName}", request.TableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to save batch record for table: {TableName}", request.TableName);
            }
        }



        private static string BuildCombinedText(List<VocabEntry> vocabList, string generalStory, string softwareStory)
        {
            var intro = "Here are today's vocabulary words and meanings:\n";
            var wordsText = string.Join("\n", vocabList.ConvertAll(v => $"{v.Word}: {v.Meaning}"));
            return $"{intro}{wordsText}\n\nHere's a general story:\n{generalStory}\n\nHere's a software story:\n{softwareStory}";
        }
    }
}
