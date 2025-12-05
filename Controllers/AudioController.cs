using Microsoft.AspNetCore.Mvc;

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
    }
}
