﻿using Microsoft.AspNetCore.Mvc;

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