using Microsoft.AspNetCore.Mvc;
using Minio.Models;
using Minio.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Minio.Controllers
{
    public class UploadController : Controller
    {
        private readonly FileUploadService _uploadService;
        private static Dictionary<string, double> _progressTracker = new Dictionary<string, double>();

        private readonly ILogger<UploadController> _logger;

        public UploadController(FileUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [HttpGet]
        public IActionResult StartUpload()
        {
            var uploadId = Guid.NewGuid().ToString();
            _progressTracker[uploadId] = 0;
            return Ok(new { uploadId });
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 12L * 1024 * 1024 * 1024)] // 12GB
        public async Task<IActionResult> Upload([FromQuery] string uploadId, [FromForm] IFormFile file)
        {
            try
            {
                if (string.IsNullOrEmpty(uploadId))
                    return BadRequest(new { error = "Upload ID tidak valid" });

                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "File tidak valid" });

                // Pastikan progress tracker memiliki ID ini
                if (!_progressTracker.ContainsKey(uploadId))
                    _progressTracker[uploadId] = 0;

                using var fileStream = file.OpenReadStream();
                long fileSize = file.Length;
                long totalRead = 0;
                int chunkSize = 10 * 1024 * 1024; // 10MB chunk
                byte[] buffer = new byte[chunkSize];
                int bytesRead;

                using var memoryStream = new MemoryStream();
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    double progress = Math.Round((double)totalRead / fileSize * 100, 2);
                    _progressTracker[uploadId] = progress;

                    Console.WriteLine($"Upload {uploadId} progress: {progress}%");

                    await Task.Delay(100);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                await _uploadService.UploadFile(memoryStream, file.FileName);
                _progressTracker[uploadId] = 100;

                return Ok(new { uploadId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }


        public async Task<IActionResult> Index()
        {
            try
            {
                // Ambil daftar file dari MinIO menggunakan FileUploadService
                var fileList = await _uploadService.GetFileListAsync(); // Ganti dengan nama bucket yang sesuai
                return View(fileList);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
                return View(new List<MinioFileInfo>());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult GetProgress([FromQuery] string uploadId)
        {
            if (_progressTracker.TryGetValue(uploadId, out double progress))
            {
                return Ok(new { progress });
            }
            return NotFound(new { error = "Upload not found" });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                var fileStream = await _uploadService.GetFileStreamAsync(fileName);

                if (fileStream == null)
                    return NotFound("File tidak ditemukan.");

                return File(fileStream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                return StatusCode(500, "Terjadi kesalahan saat mengunduh file.");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                await _uploadService.DeleteFileAsync(fileName);
                return Ok(new { message = $"File {fileName} berhasil dihapus." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Terjadi kesalahan: {ex.Message}");
            }
        }
    }
}