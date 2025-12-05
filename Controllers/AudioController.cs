using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StreamAudio.Models;

namespace StreamAudio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public AudioController(IWebHostEnvironment env)
        {
            _env = env;
        }


        // Lấy danh sách playlist .m3u8
        [HttpGet("playlists")]
        public IActionResult GetPlaylists()
        {
            var path = Path.Combine(_env.WebRootPath, "audio", "output");
            if (!Directory.Exists(path))
                return NotFound();

            var files = Directory.GetFiles(path, "*.m3u8")
                                 .Select(f => Path.GetFileName(f))
                                 .ToList();
            return Ok(files);
        }

        // Serve file .m3u8 hoặc .ts
        [HttpGet("/watch/{id}/{file}")]
        public IActionResult GetTsFile(string id, string file)
        {
            // Đường dẫn file chunk
            var path = Path.Combine(_env.WebRootPath, id, file);

            if (!System.IO.File.Exists(path))
                return NotFound();

            var contentType = file.EndsWith(".m3u8")
                ? "application/vnd.apple.mpegurl"
                : "video/MP2T";

            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        [HttpGet("{fileName}")]
        public IActionResult GetFile(string fileName)
        {
            var path = Path.Combine(_env.WebRootPath, "audio", "output", fileName);
            if (!System.IO.File.Exists(path))
                return NotFound();

            var contentType = fileName.EndsWith(".m3u8") ? "application/vnd.apple.mpegurl" : "video/MP2T";
            var stream = System.IO.File.OpenRead(path);
            return File(stream, contentType, enableRangeProcessing: true);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAndConvert([FromForm] UploadVideoRequest request)
        {
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                // Ví dụ lưu trong folder "wwwroot" của app
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                Directory.CreateDirectory(webRoot);
            }
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            // 0. Tạo thư mục wwwroot nếu chưa tồn tại
            if (!Directory.Exists(_env.WebRootPath))
            {
                Directory.CreateDirectory(_env.WebRootPath);
            }

            // 1. Tạo đường dẫn input/output
            var id = Guid.NewGuid().ToString();

            var inputPath = Path.Combine(_env.WebRootPath, $"{id}.mp4");
            var outputFolder = Path.Combine(_env.WebRootPath, id);

            Directory.CreateDirectory(outputFolder);

            // 2. Lưu file mp4 vào wwwroot
            using (var stream = new FileStream(inputPath, FileMode.Create))
                await file.CopyToAsync(stream);

            // 3. FFmpeg command chuyển mp4 → m3u8 + ts
            var ffmpegArgs =
                $"-i \"{inputPath}\" " +
                "-profile:v baseline -level 3.0 -start_number 0 " +
                "-hls_time 5 -hls_list_size 0 -f hls " +
                $"\"{Path.Combine(outputFolder, "index.m3u8")}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string stderr = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return StatusCode(500, "FFmpeg error: " + stderr);
            }

            // 4. Trả về đường dẫn playlist
            return Ok(new
            {
                id,
                playlist = $"/api/audio/output/{id}/index.m3u8"
            });
        }
    }
}
