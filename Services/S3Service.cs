using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VocabAutomation.Services.Interfaces;

namespace VocabAutomation.Services
{
    public class S3Service : IS3Service
    {
        private readonly ILogger<S3Service> _logger;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Service(IConfiguration config, ILogger<S3Service> logger)
        {
            _logger = logger;
            _bucketName = config["S3_BUCKET"];

            _s3Client = new AmazonS3Client(
                config["AWS_ACCESS_KEY_ID"],
                config["AWS_SECRET_ACCESS_KEY"],
                Amazon.RegionEndpoint.USEast1 // Change if needed
            );
        }

        public async Task<string> UploadAudioAsync(string filePath, string s3FileName)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(_s3Client);

                await fileTransferUtility.UploadAsync(filePath, _bucketName, s3FileName);
                var url = $"https://{_bucketName}.s3.amazonaws.com/{s3FileName}";
                _logger.LogInformation("✅ Uploaded audio to S3: {Url}", url);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to upload to S3.");
                return null;
            }
        }
    }
}
