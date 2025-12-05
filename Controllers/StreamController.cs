using Microsoft.AspNetCore.Mvc;
using StreamAudio.Services;

namespace StreamAudio.Controllers
{

    [ApiController]
    [Route("api/streams")]
    public class StreamController : ControllerBase
    {
        private readonly StreamManager _manager;
        public StreamController(StreamManager manager) => _manager = manager;

        [HttpPost("{streamId}/start")]
        public IActionResult Start(string streamId)
        {
            _manager.StartStream(streamId);
            return Ok();
        }

        [HttpPost("{streamId}/chunk")]
        public async Task<IActionResult> UploadChunk(string streamId)
        {
            // chunk body = binary blob (webm chunk)
            if (Request.ContentLength == null || Request.ContentLength == 0)
                return BadRequest("empty");

            await _manager.AppendChunkAsync(streamId, Request.Body);
            return Ok();
        }

        [HttpPost("{streamId}/stop")]
        public IActionResult Stop(string streamId)
        {
            _manager.StopStream(streamId);
            return Ok();
        }
    }
}
