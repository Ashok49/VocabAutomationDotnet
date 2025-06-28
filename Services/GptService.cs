using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VocabAutomation.Services.Interfaces;
using VocabAutomation.Models;

namespace VocabAutomation.Services
{
    public class GptService : IGptService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<GptService> _logger;

        public GptService(HttpClient httpClient, IConfiguration config, ILogger<GptService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<string> GenerateStoryAsync(List<VocabEntry> vocab, string context = "general")
        {
            try
            {
                var openAiKey = _config["OPENAI_API_KEY"];
                if (string.IsNullOrWhiteSpace(openAiKey))
                    throw new Exception("OpenAI API key is missing in environment variables.");

                var wordList = string.Join(", ", vocab.Select(v => v.Word));
                string prompt = context == "software"
                    ? $"Write a short software architecture story using the following vocabulary words: {wordList}. Keep it under 150 words."
                    : $"Write a simple and creative story using the following words: {wordList}. Keep it under 150 words.";

                var requestBody = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 300
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request to OpenAI with prompt: {Prompt}", prompt);

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("GPT API failed: {StatusCode}, Response: {Body}", response.StatusCode, json);
                    return "⚠️ Failed to generate story due to API error.";
                }

                using var doc = JsonDocument.Parse(json);
                var result = doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();

                _logger.LogInformation("Successfully generated story from GPT.");

                return result ?? "⚠️ Story content not found.";
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Network error while contacting OpenAI");
                return "⚠️ Network error while generating story.";
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error parsing JSON response from GPT");
                return "⚠️ Error parsing GPT response.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GenerateStoryAsync");
                return "⚠️ Unexpected error occurred while generating story.";
            }
        }

        public async Task<string> GenerateSpeechAsync(string text, string fileName = "story.mp3")
        {
            try
            {
                var payload = new
                {
                    model = "tts-1",      // You can change to "tts-1-hd" if needed
                    input = text,
                    voice = "nova"        // Other options: alloy, echo, fable, shimmer
                };

                var openAiKey = _config["OPENAI_API_KEY"];
                if (string.IsNullOrWhiteSpace(openAiKey))
                    throw new Exception("OpenAI API key is missing in environment variables.");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/speech")
                {
                    Headers = { Authorization = new AuthenticationHeaderValue("Bearer", openAiKey) },
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var audioBytes = await response.Content.ReadAsByteArrayAsync();

                await File.WriteAllBytesAsync(fileName, audioBytes);
                _logger.LogInformation("🔉 Audio saved to {FileName}", fileName);

                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating and saving speech audio.");
                return string.Empty;
            }
        }

        
    }
}
