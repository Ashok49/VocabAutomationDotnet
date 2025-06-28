using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SendBatchService> _logger;

        public SendBatchService(
            IVocabService vocabService,
            IGptService gptService,
            IEmailService emailService,
            IS3Service s3Service,
            ITwilioService twilioService,
            ILogger<SendBatchService> logger)
        {
            _vocabService = vocabService;
            _gptService = gptService;
            _emailService = emailService;
            _s3Service = s3Service;
            _twilioService = twilioService;
            _logger = logger;
        }

        public async Task<string> ProcessBatchAsync(string tableName)
        {
            try
            {
                var (vocabList, fromToday) = await _vocabService.GetVocabBatchAsync(tableName);

                if (vocabList.Count == 0)
                {
                    _logger.LogInformation("📭 No words to send for {Table}", tableName);
                    return $"📭 No words to send for {tableName}.";

                }
                if (fromToday)
                {
                    _logger.LogInformation("✅ Words from '{Table}' already sent today.", tableName);
                    return $"✅ Words from '{tableName}' already sent today.";
                }

                if (vocabList.Count == 0)
                {
                    _logger.LogInformation("✅ All words from '{Table}' are already sent.", tableName);
                    return $"✅ All words from '{tableName}' already sent.";
                }

                var generalStory = await _gptService.GenerateStoryAsync(vocabList, "general");
                var softwareStory = await _gptService.GenerateStoryAsync(vocabList, "software");

                var subject = $"🧠 Your Daily Vocabulary - {DateTime.Now:MMM dd}";
                await _emailService.SendVocabEmailAsync(vocabList, generalStory, softwareStory, subject);

                var combinedText = BuildCombinedText(vocabList, generalStory, softwareStory);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"combined_vocab_audio_{timestamp}.mp3";

                var mp3Path = await _gptService.GenerateSpeechAsync(combinedText, fileName); 
                var audioUrl = await _s3Service.UploadAudioAsync(mp3Path,fileName);

                await _twilioService.MakeCallAsync(audioUrl);
                return "✅ Batch processed and sent successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while processing batch for table: {Table}", tableName);
                return "❌ Failed to process batch.";
            }
        }

        private string BuildCombinedText(List<VocabEntry> vocabList, string generalStory, string softwareStory)
        {
            var intro = "Here are today's vocabulary words and meanings:\n";
            var wordsText = string.Join("\n", vocabList.ConvertAll(v => $"{v.Word}: {v.Meaning}"));
            return $"{intro}{wordsText}\n\nHere's a general story:\n{generalStory}\n\nHere's a software story:\n{softwareStory}";
        }
    }
}
