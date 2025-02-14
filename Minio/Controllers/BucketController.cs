using Microsoft.AspNetCore.Mvc;
using Minio.Models;
using Minio.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Minio.Controllers
{
    public class BucketController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}