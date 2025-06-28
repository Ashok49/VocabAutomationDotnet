namespace VocabAutomation.Services.Interfaces
{
    public interface IS3Service
    {
        Task<string> UploadAudioAsync(string filePath, string s3FileName);
    }
}
