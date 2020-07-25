using LivestreamViewer.Config;
using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LivestreamViewer.Monitoring
{
    /// <summary>
    /// Provides livestream monitoring capabilities using the frame-exporting
    /// capabilities of the FFMPEG utility.
    /// </summary>
    public class FFMPEGLivestreamMonitor : ILivestreamMonitor
    {
        // File extension to use for pulling frames during the test.
        private const string TempFileExtension = "jpg";

        // File prefix for pulling frames during the test.
        //
        // Note about file paths:
        // I would prefer to use a configurable temp path, but, on some
        // platforms (looking at you, Debian), FFMPEG's output won't
        // respect pathing, choosing for some reason to always put the
        // files in the current directory. This prefix helps us find files
        // we created so we can purge them.
        private const string TempFilePrefix = "liveframe-";

        private readonly LivestreamClientConfig _config;
        private readonly ILog _log = LogManager.GetLogger(typeof(FFMPEGLivestreamMonitor));

        public FFMPEGLivestreamMonitor(LivestreamClientConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Tests for livestream availability by writing frames to the current
        /// directory over a period of time and testing whether any frames
        /// were read.
        /// </summary>
        /// <param name="livestreamUrl">The URL of a livestream to test, including the stream key (if applicable).</param>
        /// <param name="token">A Cancellation Token which can terminate the test.</param>
        /// <returns>True, if any frames were downloaded over the configured time period, and false if not.</returns>
        public async Task<bool> IsLivestreamHealthyAsync(string livestreamUrl, CancellationToken token)
        {
            try
            {
                try
                {
                    await GrabStreamFramesAsync(livestreamUrl, token);
                }
                catch (OperationCanceledException)
                {
                    // Swallow--this means the caller cancelled.
                }

                // Are there any files?
                // TODO: Consider validating frames for content (e.g. are all of the frames solid black, or is there variety indicating real visible content?).
                return Directory.GetFiles(Directory.GetCurrentDirectory(), $"{TempFilePrefix}*.{TempFileExtension}", SearchOption.TopDirectoryOnly).Length > 0;
            }
            catch (Exception ex)
            {
                _log.Error($"Error testing livestream: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Uses FFMPEG to read frames from the livestream URL to the current directory for a period of time.
        /// </summary>
        /// <param name="livestreamUrl">The URL of a livestream to test, including the stream key (if applicable).</param>
        /// <param name="token">A Cancellation Token which can terminate the test.</param>
        /// <returns></returns>
        private async Task GrabStreamFramesAsync(string livestreamUrl, CancellationToken token)
        {
            await Task.Factory.StartNew(() =>
            {
                PrepTempFolder();

                //
                // ENVIRONMENT NOTE
                // 
                // Not all environments support ".exe" invocations; in such
                // environments, we assume that FFMPEG has been installed
                // already and is available in the shell. The absence of
                // the FFMPEGPath configuration option instructs us to 
                // assume such an environment.
                //
                ProcessStartInfo processInfo = null;
                // -i (input stream)
                // -r (frames per second)
                // This pulls one frame per second from the live stream and stores the frames in the current directory.
                var ffmpegArgs = $@"-i {livestreamUrl} -r 1 {TempFilePrefix}out%03d.{TempFileExtension}";
                if (!string.IsNullOrWhiteSpace(_config.VideoPlayerPath))
                {
                    _log.Debug("Performing livestream test in FFMPEG explicit executable mode.");
                    //var ffmpegPath = Path.Combine(_config.VideoPlayerPath, "ffmpeg.exe");
                    var ffmpegPath = Directory.GetFiles(_config.VideoPlayerPath).FirstOrDefault(f => Path.GetFileName(f).Contains("ffmpeg", StringComparison.OrdinalIgnoreCase));
                    if (!File.Exists(ffmpegPath))
                    {
                        _log.Error($"Could not find FFMPEG at path {ffmpegPath}.");
                        return;
                    }
                    // Example: ffmpeg -i rtmp://mysite.org/live/mykey -r 1 "C:\rtmp\out%03d.jpg"
                    processInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = ffmpegArgs
                    };
                }
                else
                {
                    _log.Debug("Performing livestream test in FFMPEG shell mode.");
                    processInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"ffmpeg {ffmpegArgs}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                
                _log.Debug($"Launching FFMPEG with arguments: {processInfo.Arguments}");
                var proc = Process.Start(processInfo);
                token.WaitHandle.WaitOne(_config.HealthCheckGracePeriod * 1000);
                if (!proc.HasExited)
                {
                    _log.Debug("Test period elapsed and FFMPEG is still running. Stopping FFMPEG.");
                    proc.Kill();
                }
                else
                {
                    _log.Debug("Test period elapsed and FFMPEG has already stopped.");
                }
            });
        }

        private void PrepTempFolder()
        {
            // Clear any files we created in previous tests.
            var existingFiles = Directory.GetFiles(
                Directory.GetCurrentDirectory(), 
                $"{TempFilePrefix}*.{TempFileExtension}", 
                SearchOption.TopDirectoryOnly);
            if (existingFiles.Length > 0)
            {
                _log.Debug("Purging temp files.");
                foreach (var f in existingFiles)
                {
                    File.Delete(f);
                }
                _log.Debug($"{existingFiles.Length} temp file(s) deleted.");
            }
        }
    }
}
