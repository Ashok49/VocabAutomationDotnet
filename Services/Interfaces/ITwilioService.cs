namespace VocabAutomation.Services.Interfaces
{
    public interface ITwilioService
    {
        Task MakeCallAsync(string audioUrl);
    }
}
