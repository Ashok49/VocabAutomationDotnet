using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VocabAutomation.Services.Interfaces;

namespace VocabAutomation.Services
{
    public class VocabSyncService : IVocabSyncService
    {
        private readonly IGoogleDriveService _driveService;
        private readonly IDocxParserService _parserService;
        private readonly IVocabService _storageService;
        private readonly ILogger<VocabSyncService> _logger;

        public VocabSyncService(
            IGoogleDriveService driveService,
            IDocxParserService parserService,
            IVocabService storageService,
            ILogger<VocabSyncService> logger)
        {
            _driveService = driveService;
            _parserService = parserService;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<string> SyncFromGoogleDriveAsync()
        {
            _logger.LogInformation("🚀 Starting vocabulary sync from Google Drive...");

            var files = await _driveService.GetDocFilesAsync();
            if (files.Count == 0)
            {
                _logger.LogInformation("📂 No Google Docs found in the folder.");
                return "No Google Docs found in the folder.";
            }

            foreach (var (fileId, fileName) in files)
            {
                try
                {
                    _logger.LogInformation("📄 Processing file: {FileName}", fileName);

                    var docxBytes = await _driveService.DownloadDocxAsync(fileId);
                    if (docxBytes.Length == 0)
                    {
                        _logger.LogWarning("⚠️ Skipped file: {FileName} — empty or unreadable.", fileName);
                        continue;
                    }

                    var entries = _parserService.ExtractWordMeanings(docxBytes);
                    if (entries.Count == 0)
                    {
                        _logger.LogWarning("⚠️ No vocab found in file: {FileName}", fileName);
                        continue;
                    }

                    await _storageService.StoreVocabularyAsync(entries, fileName);
                    _logger.LogInformation("✅ Stored {Count} entries from {FileName}", entries.Count, fileName);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "❌ Error processing file '{FileName}'", fileName);
                }
            }

            _logger.LogInformation("🎉 Vocabulary sync completed successfully.");
            return "✅ Vocab sync completed successfully.";
        }
    }
}
