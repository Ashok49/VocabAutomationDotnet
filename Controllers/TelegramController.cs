using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VocabAutomation.Controllers
{
    [ApiController]
    [Route("api/telegram")]
    public class TelegramController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TelegramController> _logger;

        private const string TELEGRAM_API = "https://api.telegram.org/bot{0}/sendMessage";

        public TelegramController(IHttpClientFactory httpClientFactory, ILogger<TelegramController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] TelegramUpdate update)
        {
            var chatId = update?.Message?.Chat?.Id;
            var messageText = update?.Message?.Text?.Trim();

            if (chatId == null || string.IsNullOrEmpty(messageText))
                return Ok();

            string responseText;

            if (messageText.Equals("/sync", StringComparison.OrdinalIgnoreCase))
            {
                await TriggerInternalApi("api/Vocab/sync");
                responseText = "✅ Sync triggered.";
            }
            else if (messageText.StartsWith("/batch", StringComparison.OrdinalIgnoreCase))
            {
                var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var tableName = parts.Length >= 2 ? parts[1] : "general_vocabulary";

                await TriggerInternalApi($"api/Vocab/send-batch/{tableName}", HttpMethod.Post);
                responseText = $"✅ Batch sent for `{tableName}`.";
            }
            else
            {
                responseText = "🤖 Welcome! Use:\n\n/sync\n/batch [table_name]";
            }

            await SendTelegramMessage(chatId.Value, responseText);
            return Ok();
        }

        private async Task TriggerInternalApi(string endpoint, HttpMethod method = null)
        {
            method ??= HttpMethod.Get;

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(method, endpoint);
            await client.SendAsync(request);
        }

        private async Task SendTelegramMessage(long chatId, string text)
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = "Markdown"
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var url = string.Format(TELEGRAM_API, Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN"));
            await client.PostAsync(url, content);
        }

        // DTOs
        public class TelegramUpdate
        {
            public TelegramMessage Message { get; set; }
        }

        public class TelegramMessage
        {
            public TelegramChat Chat { get; set; }
            public string Text { get; set; }
        }

        public class TelegramChat
        {
            public long Id { get; set; }
        }
    }
}
