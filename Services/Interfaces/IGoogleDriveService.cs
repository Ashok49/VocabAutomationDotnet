using System.Collections.Generic;
using System.Threading.Tasks;

namespace VocabAutomation.Services.Interfaces
{
    public interface IGoogleDriveService
    {
        Task<List<(string Id, string Name)>> GetDocFilesAsync();
        Task<byte[]> DownloadDocxAsync(string fileId);
    }
}
