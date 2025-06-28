using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VocabAutomation.Services.Interfaces;

namespace VocabAutomation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VocabController : ControllerBase
    {
        private readonly IVocabSyncService _syncService;
        private readonly ISendBatchService _sendBatchService;
        private readonly ILogger<VocabController> _logger;

        public VocabController(
            IVocabSyncService syncService,
            ISendBatchService sendBatchService,
            ILogger<VocabController> logger)
        {
            _syncService = syncService;
            _sendBatchService = sendBatchService;
            _logger = logger;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncFromGoogleDrive()
        {
            try
            {
                _logger.LogInformation("📥 Sync request received.");
                var result = await _syncService.SyncFromGoogleDriveAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Sync failed.");
                return StatusCode(500, "Error during Google Drive sync.");
            }
        }

        [HttpPost("send-batch/{tableName}")]
        public async Task<IActionResult> SendVocabBatch(string tableName)
        {
            try
            {
                _logger.LogInformation("📤 Sending vocab batch for table: {Table}", tableName);
                var result = await _sendBatchService.ProcessBatchAsync(tableName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send vocab batch.");
                return StatusCode(500, "Error while sending vocab batch.");
            }
        }
    }
}
