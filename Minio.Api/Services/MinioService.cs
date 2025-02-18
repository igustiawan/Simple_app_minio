using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace Minio.Api.Services 
{
    public class MinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;
        private readonly ILogger<MinioService> _logger;

        public MinioService(IConfiguration config, ILogger<MinioService> logger)
        {
            var minioConfig = config.GetSection("Minio");

            // ✅ Tambahkan validasi
            if (minioConfig["Endpoint"] == null)
                throw new ArgumentNullException("Minio:Endpoint config missing");

            _minioClient = new MinioClient()
                .WithEndpoint(minioConfig["Endpoint"])
                .WithCredentials(
                    minioConfig["AccessKey"] ?? throw new ArgumentNullException("Minio:AccessKey missing"),
                    minioConfig["SecretKey"] ?? throw new ArgumentNullException("Minio:SecretKey missing")
                )
                .WithSSL(bool.Parse(minioConfig["WithSSL"] ?? "false"))
                .Build();

            _bucketName = minioConfig["BucketName"] ?? "default-bucket";

            TestConnection();
        }

        public async Task TestConnection()
        {
            try
            {
                var args = new BucketExistsArgs().WithBucket(_bucketName);
                bool exists = await _minioClient.BucketExistsAsync(args);
                Console.WriteLine($"Koneksi berhasil. Bucket exists: {exists}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gagal terkoneksi ke MinIO: {ex.Message}");
            }
        }

        public async Task UploadFileAsync(
           string objectName,
           Stream data,
           long size,
           Action<double> progress)
        {
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(data)
                .WithObjectSize(size)
                .WithProgress(new Progress<ProgressReport>(report =>
                {
                    Console.WriteLine($"Upload progress: {report.Percentage}%"); // ✅ Debugging tambahan
                    progress(report.Percentage);
                }));

            await _minioClient.PutObjectAsync(putObjectArgs);
        }

        public async Task<List<FileInfo>> GetFileListAsync()
        {
            var fileList = new List<FileInfo>();

            try
            {
                var args = new ListObjectsArgs().WithBucket(_bucketName);
                var observable = _minioClient.ListObjectsAsync(args);

                // Create a TaskCompletionSource to wait until observable is done
                var tcs = new TaskCompletionSource<bool>();

                // Subscribe to the observable and accumulate items in the list
                observable.Subscribe(
                    item =>
                    {
                        var fileInfo = new FileInfo
                        {
                            FileName = item.Key,
                            LastModified = (DateTime)item.LastModifiedDateTime,
                            Size = (long)item.Size
                        };
                        fileList.Add(fileInfo); // Add item to the list
                        Console.WriteLine($"Item found: {item.Key}, Last Modified: {item.LastModifiedDateTime}, Size: {item.Size} bytes"); // Log the item
                    },
                    ex =>
                    {
                        Console.WriteLine($"Error retrieving file list: {ex.Message}");
                        tcs.SetResult(false); // If error occurs, complete with false
                    },
                    () =>
                    {
                        Console.WriteLine("Finished retrieving file list.");
                        tcs.SetResult(true); // Complete with true when done
                    }
                );

                // Wait until the subscription is completed
                await tcs.Task;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return fileList;
        }

        // MinioService.cs
        public async Task<Stream> DownloadFileAsync(string fileName)
        {
            try
            {
                var memoryStream = new MemoryStream();
                var tcs = new TaskCompletionSource<bool>();

                var args = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithCallbackStream(stream =>
                    {
                        try
                        {
                            // Salin stream SINKRON ke MemoryStream
                            stream.CopyTo(memoryStream); // ⚠️ Gunakan CopyTo(), bukan CopyToAsync()
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            tcs.SetResult(true); // Tandai operasi selesai
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    });

                // Jalankan GetObjectAsync dan tunggu callback selesai
                await _minioClient.GetObjectAsync(args).ConfigureAwait(false);
                await tcs.Task.ConfigureAwait(false); // ⚠️ Pastikan callback selesai

                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file: {ex.Message}");
                throw;
            }
        }


        public async Task<ReleaseableFileStreamModel> DownloadFileLargeAsync(string fileName)
        {
            try
            {
                var statArgs = new StatObjectArgs()
                    .WithObject(fileName)
                    .WithBucket(_bucketName);
                var stat = await _minioClient.StatObjectAsync(statArgs);
                var res = new ReleaseableFileStreamModel
                {
                    ContentType = stat.ContentType,
                    FileName = fileName,
                };
                // the magic begins here
                var getArgs = new GetObjectArgs()
                    .WithObject(fileName)
                    .WithBucket(_bucketName)
                    .WithCallbackStream(res.SetStreamAsync);
                await res.HandleAsync(_minioClient.GetObjectAsync(getArgs));
                // the magic partially ends here
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileName)
        {
            try
            {
                var args = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName);
                await _minioClient.RemoveObjectAsync(args);
                Console.WriteLine($"File {fileName} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                throw;
            }
        }
    }


    public class FileInfo
    {
        public string FileName { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
    }
}