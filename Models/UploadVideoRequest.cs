using Microsoft.AspNetCore.Mvc;

namespace StreamAudio.Models
{
    public class UploadVideoRequest
    {
        [FromForm]
        public IFormFile File { get; set; }
    }
}
