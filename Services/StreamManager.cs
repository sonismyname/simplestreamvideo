using System.Collections.Concurrent;
using System.Diagnostics;

namespace StreamAudio.Services
{
    public class StreamManager
    {
        private readonly string _baseStreamPath = "/tmp"; // FIFO nơi lưu
        private readonly string _wwwRootLive = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "live");
        private readonly ConcurrentDictionary<string, (FileStream writer, Process ffmpeg)> _map = new();

        public StreamManager()
        {
            Directory.CreateDirectory(_wwwRootLive);
        }

        public void StartStream(string streamId)
        {
            if (_map.ContainsKey(streamId)) return;

            Directory.CreateDirectory(Path.Combine(_wwwRootLive, streamId));
            var fifoPath = Path.Combine(_baseStreamPath, $"stream-{streamId}.webm");

            // create fifo (Linux)
            if (!File.Exists(fifoPath))
            {
                var mk = new ProcessStartInfo("mkfifo", fifoPath) { RedirectStandardOutput = true, RedirectStandardError = true };
                var p = Process.Start(mk);
                p.WaitForExit();
            }

            // Start FFmpeg reading from fifoPath and writing HLS to wwwroot/live/{streamId}
            var outputDir = Path.Combine(_wwwRootLive, streamId);
            var ffmpegArgs = $"-y -fflags +genpts -i {fifoPath} " +
                             "-c:v libx264 -preset veryfast -tune zerolatency -c:a aac -ar 44100 -b:a 96k " +
                             $"-f hls -hls_time 1 -hls_list_size 5 -hls_flags delete_segments+append_list " +
                             $"-hls_segment_filename {outputDir}/seg%03d.ts {outputDir}/stream.m3u8";

            var startInfo = new ProcessStartInfo("ffmpeg", ffmpegArgs)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var ffmpeg = Process.Start(startInfo);

            // open writer to FIFO (non-blocking write)
            var fifoWriter = new FileStream(fifoPath, FileMode.Open, FileAccess.Write, FileShare.Read);

            _map[streamId] = (fifoWriter, ffmpeg);
        }

        public async Task AppendChunkAsync(string streamId, Stream chunkStream, CancellationToken ct = default)
        {
            if (!_map.TryGetValue(streamId, out var entry))
            {
                // auto start stream if not started
                StartStream(streamId);
                entry = _map[streamId];
            }

            var writer = entry.writer;
            // write chunk bytes into fifo
            await chunkStream.CopyToAsync(writer, 81920, ct);
            await writer.FlushAsync(ct);
        }

        public void StopStream(string streamId)
        {
            if (_map.TryRemove(streamId, out var entry))
            {
                try { entry.writer.Close(); entry.writer.Dispose(); } catch { }
                try
                {
                    if (!entry.ffmpeg.HasExited)
                    {
                        entry.ffmpeg.Kill(true);
                        entry.ffmpeg.WaitForExit();
                    }
                }
                catch { }
            }
        }
    }
}
