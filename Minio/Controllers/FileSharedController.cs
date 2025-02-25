using Microsoft.AspNetCore.Mvc;

namespace Minio.Controllers
{
    public class FileSharedController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}