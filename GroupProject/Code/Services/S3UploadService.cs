using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace GroupProject.Code.Services
{
    public class S3UploadService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly string _bucketName;

        public S3UploadService(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _bucketName = configuration["AWS:S3BucketName"] ?? "podcasthub-audio";
        }

        public async Task<string> UploadFileAsync(string filePath, string s3Key)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(_s3Client);
                
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    BucketName = _bucketName,
                    FilePath = filePath,
                    Key = s3Key,
                    ContentType = "audio/mpeg",
                    CannedACL = S3CannedACL.PublicRead // Make file publicly readable
                };

                await fileTransferUtility.UploadAsync(uploadRequest);
                
                // Return the public URL
                string s3Url = $"https://{_bucketName}.s3.amazonaws.com/{s3Key}";
                Console.WriteLine($"? Uploaded (public): {s3Key} -> {s3Url}");
                
                return s3Url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error uploading {filePath}: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, string>> UploadPodcastAudioFilesAsync(string localFolderPath)
        {
            var uploadedUrls = new Dictionary<string, string>();
            
            if (!Directory.Exists(localFolderPath))
            {
                Console.WriteLine($"? Folder not found: {localFolderPath}");
                return uploadedUrls;
            }

            // Get existing files in S3
            var existingFiles = await ListExistingFilesAsync();

            // Get all MP3 files
            var mp3Files = Directory.GetFiles(localFolderPath, "*.mp3", SearchOption.AllDirectories);
            
            Console.WriteLine($"?? Found {mp3Files.Length} MP3 files to upload...");
            Console.WriteLine($"?? {existingFiles.Count} files already exist in S3");

            foreach (var filePath in mp3Files)
            {
                var fileName = Path.GetFileName(filePath);
                var relativePath = Path.GetRelativePath(localFolderPath, filePath);
                
                // Create S3 key (path in S3) - clean up any double extensions
                var cleanFileName = fileName.Replace(".mp3.mp3", ".mp3");
                var s3Key = $"podcasts/{cleanFileName}";
                
                // Check if file already exists
                if (existingFiles.ContainsKey(s3Key))
                {
                    Console.WriteLine($"?? Skipping (already exists): {cleanFileName}");
                    uploadedUrls[cleanFileName] = $"https://{_bucketName}.s3.amazonaws.com/{s3Key}";
                    continue;
                }

                try
                {
                    var s3Url = await UploadFileAsync(filePath, s3Key);
                    uploadedUrls[cleanFileName] = s3Url;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Failed to upload {fileName}: {ex.Message}");
                }
            }

            Console.WriteLine($"? Successfully uploaded {uploadedUrls.Count} files to S3!");
            return uploadedUrls;
        }

        private async Task<Dictionary<string, string>> ListExistingFilesAsync()
        {
            var existingFiles = new Dictionary<string, string>();
            
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = "podcasts/"
                };

                ListObjectsV2Response response;
                do
                {
                    response = await _s3Client.ListObjectsV2Async(request);
                    
                    foreach (var obj in response.S3Objects)
                    {
                        existingFiles[obj.Key] = $"https://{_bucketName}.s3.amazonaws.com/{obj.Key}";
                    }

                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated == true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Could not list existing files: {ex.Message}");
            }

            return existingFiles;
        }

        public async Task<bool> CheckBucketExistsAsync()
        {
            try
            {
                await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    MaxKeys = 1
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task CreateBucketIfNotExistsAsync()
        {
            try
            {
                if (!await CheckBucketExistsAsync())
                {
                    Console.WriteLine($"?? Creating S3 bucket: {_bucketName}");
                    await _s3Client.PutBucketAsync(new PutBucketRequest
                    {
                        BucketName = _bucketName,
                        UseClientRegion = true
                    });
                    
                    // Wait a moment for bucket to be created
                    await Task.Delay(2000);
                    
                    // Set bucket policy to allow public read access
                    await SetBucketPolicyForPublicReadAsync();
                    
                    Console.WriteLine($"? Bucket created with public read access: {_bucketName}");
                }
                else
                {
                    Console.WriteLine($"? Bucket already exists: {_bucketName}");
                    // Ensure bucket policy is set
                    await SetBucketPolicyForPublicReadAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error with bucket: {ex.Message}");
            }
        }

        private async Task SetBucketPolicyForPublicReadAsync()
        {
            try
            {
                // Create a bucket policy that allows public read access
                var bucketPolicy = $@"{{
                    ""Version"": ""2012-10-17"",
                    ""Statement"": [
                        {{
                            ""Sid"": ""PublicReadGetObject"",
                            ""Effect"": ""Allow"",
                            ""Principal"": ""*"",
                            ""Action"": ""s3:GetObject"",
                            ""Resource"": ""arn:aws:s3:::{_bucketName}/*""
                        }}
                    ]
                }}";

                await _s3Client.PutBucketPolicyAsync(new PutBucketPolicyRequest
                {
                    BucketName = _bucketName,
                    Policy = bucketPolicy
                });

                Console.WriteLine($"? Bucket policy set for public read access");
                
                // Also set CORS configuration for audio playback
                await SetBucketCorsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Warning: Could not set bucket policy: {ex.Message}");
                Console.WriteLine($"   You may need to manually set the bucket to public in AWS Console");
            }
        }

        private async Task SetBucketCorsAsync()
        {
            try
            {
                var corsConfiguration = new CORSConfiguration
                {
                    Rules = new List<CORSRule>
                    {
                        new CORSRule
                        {
                            AllowedMethods = new List<string> { "GET", "HEAD" },
                            AllowedOrigins = new List<string> { "*" },
                            AllowedHeaders = new List<string> { "*" },
                            ExposeHeaders = new List<string> { "ETag", "Content-Length", "Content-Type" },
                            MaxAgeSeconds = 3000
                        }
                    }
                };

                await _s3Client.PutCORSConfigurationAsync(new PutCORSConfigurationRequest
                {
                    BucketName = _bucketName,
                    Configuration = corsConfiguration
                });

                Console.WriteLine($"? CORS configuration set for audio playback");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Warning: Could not set CORS: {ex.Message}");
            }
        }

        public async Task<string> GetPresignedUrlAsync(string s3Key, int expirationMinutes = 60)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key,
                    Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };

                string url = _s3Client.GetPreSignedURL(request);
                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error generating presigned URL: {ex.Message}");
                return $"https://{_bucketName}.s3.amazonaws.com/{s3Key}";
            }
        }
    }
}