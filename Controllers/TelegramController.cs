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
            var messageText = update?.Message?.Text;

            if (chatId == null || string.IsNullOrEmpty(messageText))
                return Ok();

            string responseText;

            switch (messageText.ToLower())
            {
                case "/sync":
                    await TriggerInternalApi("api/Vocab/sync");
                    responseText = "✅ Sync triggered.";
                    break;
                case "/batch":
                    await TriggerInternalApi("api/Vocab/send-batch/software_vocabulary", HttpMethod.Post);
                    responseText = "✅ Batch sent.";
                    break;
                default:
                    responseText = "🤖 Welcome! Use `/sync` or `/batch`.";
                    break;
            }

            await SendTelegramMessage(chatId.Value, responseText);
            return Ok();
        }

        private async Task TriggerInternalApi(string path, HttpMethod method = null)
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(method ?? HttpMethod.Get, $"https://vocabautomationdotnet.onrender.com/{path}");
            await client.SendAsync(request);
        }

        private async Task SendTelegramMessage(long chatId, string text)
        {
            var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            var client = _httpClientFactory.CreateClient();

            var body = new
            {
                chat_id = chatId,
                text = text
            };

            var json = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            await client.PostAsync(string.Format(TELEGRAM_API, token), json);
        }
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
