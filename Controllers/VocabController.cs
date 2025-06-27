using Microsoft.AspNetCore.Mvc;
using VocabAutomation.Services.Interfaces;
using System.Threading.Tasks;
using System;

namespace VocabAutomation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VocabController : ControllerBase
    {
        private readonly IGoogleDriveService _driveService;
        private readonly IDocxParserService _parserService;
        private readonly IVocabStorageService _storageService;

        public VocabController(
            IGoogleDriveService driveService,
            IDocxParserService parserService,
            IVocabStorageService storageService)
        {
            _driveService = driveService;
            _parserService = parserService;
            _storageService = storageService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncFromGoogleDrive()
        {
            try
            {
                var files = await _driveService.GetDocFilesAsync();

                if (files.Count == 0)
                {
                    return Ok("No Google Docs found in the folder.");
                }

                foreach (var (fileId, fileName) in files)
                {
                    try
                    {
                        var docxBytes = await _driveService.DownloadDocxAsync(fileId);
                        if (docxBytes.Length == 0)
                        {
                            Console.WriteLine($"⚠️ Skipped file: {fileName} — empty or unreadable.");
                            continue;
                        }

                        var entries = _parserService.ExtractWordMeanings(docxBytes);

                        if (entries.Count == 0)
                        {
                            Console.WriteLine($"⚠️ No vocab found in file: {fileName}");
                            continue;
                        }

                        await _storageService.StoreVocabularyAsync(entries, fileName);
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine($"❌ Error processing file '{fileName}': {innerEx.Message}");
                    }
                }

                return Ok("✅ Vocab sync completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal error during vocab sync: {ex.Message}");
                return StatusCode(500, "An error occurred during vocab sync.");
            }
        }
    }
}
