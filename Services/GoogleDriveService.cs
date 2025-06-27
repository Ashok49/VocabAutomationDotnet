using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Download;
using Google.Apis.Drive.v3.Data;
using VocabAutomation.Services.Interfaces;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace VocabAutomation.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly DriveService _driveService;
        private readonly string _folderId;

        public GoogleDriveService(IConfiguration configuration)
        {
            try
            {
                var credentialPath = configuration["GOOGLE_KEY_PATH"];
                _folderId = configuration["GOOGLE_FOLDER_ID"];

                if (string.IsNullOrEmpty(credentialPath) || string.IsNullOrEmpty(_folderId))
                {
                    throw new ArgumentException("Google service account path or folder ID is not configured.");
                }

                GoogleCredential credential = GoogleCredential.FromFile(credentialPath)
                    .CreateScoped(DriveService.Scope.DriveReadonly);

                _driveService = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "VocabAutomation"
                });
                Console.WriteLine("Google drive service connected...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to initialize Google Drive service: {ex.Message}");
                throw;
            }
        }

        public async Task<List<(string Id, string Name)>> GetDocFilesAsync()
        {
            try
            {
                var request = _driveService.Files.List();
                request.Q = $"'{_folderId}' in parents and trashed = false and mimeType = 'application/vnd.google-apps.document'";
                request.Fields = "files(id, name)";

                var result = await request.ExecuteAsync();
                return result.Files.Select(f => (f.Id, f.Name)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching Google Docs: {ex.Message}");
                return new List<(string, string)>(); // Return empty list instead of crashing
            }
        }

        public async Task<byte[]> DownloadDocxAsync(string fileId)
        {
            try
            {
                var request = _driveService.Files.Export(fileId, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

                using var stream = new MemoryStream();
                var progress = await request.DownloadAsync(stream);

                if (progress.Status == DownloadStatus.Completed)
                {
                    return stream.ToArray();
                }

                throw new Exception($"Incomplete download: Status={progress.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to download DOCX for fileId {fileId}: {ex.Message}");
                return Array.Empty<byte>(); // Fallback to empty byte array
            }
        }
    }
}
