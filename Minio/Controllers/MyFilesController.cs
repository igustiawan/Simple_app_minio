using Microsoft.AspNetCore.Mvc;
using Minio.Services;

namespace Minio.Controllers
{
    public class MyFilesController : Controller
    {
        private readonly FileUploadService _uploadService;
        private static Dictionary<string, double> _progressTracker = new Dictionary<string, double>();

        public MyFilesController(FileUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFileList()
        {
            try
            {
                var fileList = await _uploadService.GetFileListAsync();
                return Json(fileList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }
    }
}