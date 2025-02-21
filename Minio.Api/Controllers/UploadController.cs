using Microsoft.AspNetCore.Mvc;
using Minio.Api.Services;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly MinioService _minioService;

    public UploadController(MinioService minioService)
    {
        _minioService = minioService;
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteFile([FromQuery] string fileName)
    {
        try
        {
            await _minioService.DeleteFileAsync(fileName);
            return Ok($"File {fileName} deleted successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpGet("download")]
    public async Task DownloadFile([FromQuery] string fileName)
    {
        try
        {
            var res = await _minioService.DownloadFileLargeAsync(fileName);
            Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
            Response.ContentType = res.ContentType;
            await res.Stream.CopyToAsync(Response.Body);
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;
            await Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"Error: {ex.Message}"));
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File invalid");

        try
        {
            var objectName = $"{Guid.NewGuid()}-{file.FileName}";

            using var stream = file.OpenReadStream();
            await _minioService.UploadFileAsync(
                objectName,
                stream,
                file.Length,
                percentage => Console.WriteLine($"Progress: {percentage}%")
            );

            return Ok(new { objectName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetListData()
    {
        try
        {
            var fileList = await _minioService.GetFileListAsync();
            return Ok(fileList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}