using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using VocabAutomation.Services.Interfaces;
using Twilio.Types;

namespace VocabAutomation.Services
{
    public class TwilioService : ITwilioService
    {
        private readonly ILogger<TwilioService> _logger;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _from;
        private readonly string _to;

        public TwilioService(IConfiguration config, ILogger<TwilioService> logger)
        {
            _logger = logger;
            _accountSid = config["TWILIO_ACCOUNT_SID"];
            _authToken = config["TWILIO_AUTH_TOKEN"];
            _from = config["TWILIO_FROM_NUMBER"];
            _to = config["TWILIO_TO_NUMBER"];

            TwilioClient.Init(_accountSid, _authToken);
        }

        public async Task MakeCallAsync(string audioUrl)
        {
            try
            {
                var call = await CallResource.CreateAsync(
                    twiml: new Twiml($"<Response><Play>{audioUrl}</Play></Response>"),
                    to: new Twilio.Types.PhoneNumber(_to),
                    from: new Twilio.Types.PhoneNumber(_from)
                );

                _logger.LogInformation("📞 Call initiated: {SID}", call.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Twilio call failed.");
            }
        }
    }
}
