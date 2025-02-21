using Microsoft.AspNetCore.Mvc;
using Minio.Api.Services;

[ApiController]
[Route("api/[controller]")]
public class BucketController : ControllerBase
{
    private readonly MinioService _minioService;

    public BucketController(MinioService minioService)
    {
        _minioService = minioService;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetListData()
    {
        try
        {
            var fileList = await _minioService.GetBucketListAsync();
            return Ok(fileList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddBucket([FromQuery] string bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            return BadRequest("Nama bucket tidak boleh kosong.");
        }

        bool success = await _minioService.AddBucketAsync(bucketName);
        if (success)
        {
            return Ok(new { message = $"Bucket '{bucketName}' berhasil dibuat." });
        }
        return Conflict(new { message = $"Bucket '{bucketName}' sudah ada." });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteBucket([FromQuery] string bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            return BadRequest("Nama bucket tidak boleh kosong.");
        }

        bool success = await _minioService.DeleteBucketAsync(bucketName);
        if (success)
        {
            return Ok(new { message = $"Bucket '{bucketName}' berhasil dihapus." });
        }
        return NotFound(new { message = $"Bucket '{bucketName}' tidak ditemukan." });
    }
}