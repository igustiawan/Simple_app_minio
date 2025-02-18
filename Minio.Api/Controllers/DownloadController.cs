using Microsoft.AspNetCore.Mvc;
using Minio.Api.Services;
using System.Collections.Concurrent;

[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly MinioService _minioService;

    public DownloadController(MinioService minioService)
    {
        _minioService = minioService;
    }

    [HttpGet]
    public async Task DownloadFile([FromQuery] string fileName)
    {
        try
        {
            var res = await _minioService.DownloadFileLargeAsync(fileName);
            Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
            Response.ContentType = res.ContentType;
            await res.Stream.CopyToAsync(Response.Body);

            //var fileStream = await _minioService.DownloadFileAsync(fileName);
            //Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            //return File(fileStream, "application/octet-stream");
        }
        catch (Exception ex)
        {
           
        }
    }



}