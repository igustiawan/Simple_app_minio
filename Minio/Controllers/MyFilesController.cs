using Microsoft.AspNetCore.Mvc;

namespace Minio.Controllers
{
    public class MyFilesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}