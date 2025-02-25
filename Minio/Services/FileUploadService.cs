using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Minio.Controllers;

namespace Minio.Services
{
    public class FileUploadService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly IHubContext<UploadHub> _hubContext;

        public FileUploadService(
            HttpClient httpClient,
            IConfiguration config,
            IHubContext<UploadHub> hubContext)
        {
            _httpClient = httpClient;
            _apiBaseUrl = config["ApiBaseUrl"];
            _hubContext = hubContext;
        }

        public async Task<string> UploadFile(Stream fileStream, string fileName)
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/Upload/upload", content);

            await _hubContext.Clients.All.SendAsync("ReceiveUploadProgress", fileName, 100);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // Metode baru untuk mengambil daftar file dari API
        public async Task<List<MinioFileInfo>> GetFileListAsync()
        {
            try
            {
                // Panggil API GetListController
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/upload/list");

                response.EnsureSuccessStatusCode(); // Pastikan response sukses

                // Baca dan deserialisasi response JSON ke List<MinioFileInfo>
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var fileList = JsonSerializer.Deserialize<List<MinioFileInfo>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return fileList ?? new List<MinioFileInfo>(); // Return list kosong jika null
            }
            catch (Exception ex)
            {
                // Tangani error
                Console.WriteLine($"Error fetching file list: {ex.Message}");
                throw; // Re-throw exception untuk penanganan lebih lanjut
            }
        }

        public async Task<Stream> GetFileStreamAsync(string fileName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Upload/Download?fileName={fileName}");

                response.EnsureSuccessStatusCode(); // Pastikan response sukses

                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching file stream: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileName)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/Upload/Delete?fileName={fileName}");

                response.EnsureSuccessStatusCode();
                Console.WriteLine($"File {fileName} berhasil dihapus.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                throw;
            }
        }

    }

    public class MinioFileInfo
    {
        public string FileName { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
    }
}