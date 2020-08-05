using LivestreamViewer.Config;
using LivestreamViewer.Constants;
using LivestreamViewer.Monitoring;
using LivestreamViewer.Util;
using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LivestreamViewer
{
    public class LivestreamViewer
    {
        private readonly LivestreamClientConfig _config;
        private readonly ILivestreamMonitor _monitor;
        private VideoCommandResolver _videoResolver;
        private readonly ILog _log = LogManager.GetLogger(typeof(LivestreamViewer));

        // Reference to the process for the currently-playing video.
        private Process _videoProcess;

        private ViewerState _state = ViewerState.Unset;

        public LivestreamViewer(LivestreamClientConfig config, ILivestreamMonitor monitor, VideoCommandResolver videoResolver)
        {
            _config = config;
            _monitor = monitor;
            _videoResolver = videoResolver;
        }

        public async Task KeepViewerActive(CancellationToken token)
        {
            _log.Info("Starting viewer keep-alive thread.");
            await Task.Factory.StartNew(async () =>
            {
                // Start with the "off-air" video.
                await Transition(ViewerState.OffAir);

                // Main keep-alive portion of the thread.
                while (!token.IsCancellationRequested)
                {
                    // Are we healthy? Make sure to re-evaluate the livestream URL in case it has changed.
                    var livestreamUrl = await _config.ResolveLivestreamUrlAsync();
                    var isStreamHealthy = await _monitor.IsLivestreamHealthyAsync(livestreamUrl, token);
                    if (isStreamHealthy)
                    {
                        await Transition(ViewerState.Livestream);
                    }
                    else
                    {
                        // Perform a simple internet test so we can
                        // alert the user if they are offline.
                        var isOnline = false;
                        try
                        {
                            isOnline = await NetworkUtil.IsInternetAvailable(_config.InternetTestUrl);
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"Error checking Internet connectivity. Assume no connectivity. Error: {ex}");
                        }

                        // Show the "offline" video if no Internet connectivity,
                        // or the "off-air" video if there is connectivity.
                        await Transition(isOnline ? ViewerState.OffAir : ViewerState.Offline);
                    }

                    // Wait for a period of time, respecting cancellation.
                    token.WaitHandle.WaitOne(_config.HealthCheckDelay * 1000);
                }
                _log.Info("Viewer keep-alive thread stopped.");
                StopVideo();
            });

        }

        private async Task Transition(ViewerState state)
        {
            if (_state == state && _videoProcess != null && !_videoProcess.HasExited)
            {
                return;
            }
            else
            {
                _state = state;
                _log.Info($"Transitioning to state: {Enum.GetName(typeof(ViewerState), state)}.");

                var command = await _videoResolver.Resolve(state);
                PlayVideo(command);
            }
        }

        private void StopVideo()
        {
            if (_videoProcess != null && !_videoProcess.HasExited)
            {
                _log.Info($"Video is currently playing. Terminating process {_videoProcess.Id}.");
                try
                {
                    _videoProcess.Kill();
                    _log.Info("Video playback terminated.");
                }
                catch (Exception ex)
                {
                    _log.Error($"Error terminating video process {_videoProcess.Id}: {ex}");
                }
            }

            // Kill any rogue video processes that we aren't tracking.
            StopRogueProcesses();
        }

        /// <summary>
        /// Terminates any running video processes that use one of the known players.
        /// </summary>
        private void StopRogueProcesses()
        {
            _log.Info("Searching for rogue video processes.");
            foreach (var player in _videoResolver.GetKnownVideoPlayers())
            {
                FindAndStopProcess(player);
            }
        }

        private void FindAndStopProcess(string name)
        {
            try
            {
                foreach (var p in Process.GetProcesses().Where(p => p.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                {
                    _log.Info($"Stopping rogue {name} process (PID: {p.Id})");
                    p.Kill();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Failed to stop one or more processes with name {name}: {ex}");
            }
        }

        private void PlayVideo(VideoPlayerCommand command)
        {
            // TODO: How long can we defer StopVideo? We want as brief a delay as possible; FFPLAY sometimes takes a moment to start displaying the video, but I'm concerned about the risk of overlapping instances.
            // TODO: Do we need to check if the player is "on top"?
            // TODO: Consider some sort of check that the video player is still visible.
            //       We have had trouble, for instance, with OMXPLAYER running but not showing video.
            StopVideo();
            ProcessStartInfo processInfo = null;
            if (command.UseExplicitMode)
            {
                _log.Debug($"Configuring {command.PlayerName} for launch in explicit executable mode.");
                var playerPath = Directory.GetFiles(_config.VideoPlayerPath).FirstOrDefault(f => Path.GetFileName(f).Contains(command.PlayerName, StringComparison.OrdinalIgnoreCase));
                if (!File.Exists(playerPath))
                {
                    _log.Error($"Could not find {command.PlayerName} at {playerPath}.");
                    return;
                }

                // Surround the player path with quotes if necessary.
                if(playerPath.Contains(' ') && !playerPath.StartsWith('"'))
                {
                    playerPath = $"\"{playerPath}\"";
                }
                processInfo = new ProcessStartInfo
                {
                    FileName = playerPath,
                    Arguments = command.PlayerArgs
                };
            }
            else
            {
                _log.Debug($"Configuring {command.PlayerName} for launch in shell mode.");
                processInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command.PlayerName} {command.PlayerArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            _log.Debug($"Launching video player with arguments: {processInfo.Arguments}");
            try
            {
                _videoProcess = Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                _log.Error($"Error launching video player: {ex}");
            }
        }
    }
}
