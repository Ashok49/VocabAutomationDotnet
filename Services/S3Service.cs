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
        private readonly string _audiobucketName;

        private readonly string _pdfBucketName;

        public S3Service(IConfiguration config, ILogger<S3Service> logger)
        {
            _logger = logger;
            _audiobucketName = config["S3_AUDIO_BUCKET"];
            _pdfBucketName = config["S3_PDF_BUCKET"];
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

                await fileTransferUtility.UploadAsync(filePath, _audiobucketName, s3FileName);
                var url = $"https://{_audiobucketName}.s3.amazonaws.com/{s3FileName}";
                _logger.LogInformation("✅ Uploaded audio to S3: {Url}", url);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to upload to S3.");
                return null;
            }
        }

        public async Task<string> UploadPdfAsync(Stream stream, string fileName)
        {
            try
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = fileName,
                    BucketName = _pdfBucketName,
                    ContentType = "application/pdf",
                };

                var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(uploadRequest);

                var url = $"https://{_pdfBucketName}.s3.amazonaws.com/{fileName}";
                _logger.LogInformation("✅ PDF uploaded to S3: {Url}", url);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to upload PDF to S3");
                throw;
            }
        }
    }
}
