using LivestreamViewer.Config;
using LivestreamViewer.Constants;
using System;
using System.IO;

namespace LivestreamViewer
{
    /// <summary>
    /// Generates playback commands for videos used by the livestream client.
    /// </summary>
    public class VideoCommandResolver
    {
        private const int TestModeWidth = 640;
        private const int TestModeHeight = 480;

        private const string OmxPlayerName = "omxplayer";
        private const string FFMPEGPlayerName = "ffmpeg";
        private const string FFPlayPlayerName = "ffplay";

        private readonly LivestreamClientConfig _config;
        private readonly bool _explicitModeEnabled;

        public VideoCommandResolver(LivestreamClientConfig config)
        {
            _config = config;

            // Determine whether video players can execute in "explicit" mode.
            // If configuration specifies a path to a directory containing video
            // player executables, then generated commands will refer to players
            // assumed to exist in that folder.
            _explicitModeEnabled = !string.IsNullOrWhiteSpace(config.VideoPlayerPath);
        }

        /// <summary>
        /// Returns the names of known video player applications
        /// </summary>
        public string[] GetKnownVideoPlayers()
        {
            return new string[] { OmxPlayerName, FFMPEGPlayerName, FFPlayPlayerName };
        }

        /// <summary>
        /// Returns the names of known video player applications that are intended
        /// to display visible output to the user.
        /// </summary>
        public string[] GetKnownVisibleVideoPlayers()
        {
            return new string[] { OmxPlayerName, FFPlayPlayerName };
        }

        /// <summary>
        /// Returns the names of known applications that manipulate video but
        /// do not display visible output to the user.
        /// </summary>
        public string[] GetKnownVideoProcessors()
        {
            return new string[] { FFMPEGPlayerName };
        }

        /// <summary>
        /// Resolves a shell command for the given viewer state that, when
        /// executed, plays a video appropriate to the given state. 
        /// 
        /// Special controls such as looping are dependent upon the particular state.
        /// 
        /// The caller is responsible for starting the video player process using
        /// environment-specific syntax; this method produces environment-agnostic
        /// arguments for one or more types of video players.
        /// </summary>
        /// <param name="state">A livestream viewer state.</param>
        /// <param name="videoPlayerName">Stores the derived name of the video player that the caller should invoke.</param>
        /// <param name="useExplicitMode">Informs the caller whether to invoke the video player explicitly as an executable or implicitly from shell.</param>
        /// <returns>Arguments for a video player that will play an appropriate video for the given state.</returns>
        public string Resolve(ViewerState state, out string videoPlayerName, out bool useExplicitMode)
        {
            // FFPLAY seems to be the more-reliable video player, but
            // it performs poorly on some lower-end devices. OMXPLAYER has
            // better all-around performance. Either way, if config specifies
            // an explicit video player path, then we want to use FFPLAY.
            //
            // Be careful, though: OMXPLAYER has been known to exhibit some
            // inconsistencies in playback. Sometimes, it will start and print
            // the usual messages to the console, then hang without ever showing
            // any video.
            videoPlayerName = _explicitModeEnabled ? FFPlayPlayerName : OmxPlayerName;
            useExplicitMode = _explicitModeEnabled;
            switch (state)
            {
                case ViewerState.Livestream:
                    return _explicitModeEnabled
                                ? GetFFPLAYArgs(_config.LivestreamUrl, _config.TestModeEnabled, false)
                                : GetOmxplayerArgs(_config.LivestreamUrl, _config.TestModeEnabled, false);
                case ViewerState.Unset:
                    throw new Exception($"Invalid viewer state: [{Enum.GetName(typeof(ViewerState), state)}].");
                default:
                    // Convention: Viewer states which display static content
                    // (i.e. everything other than the livestream state) use
                    // video files whose names match their state.
                    //
                    // NOTE: Use forward slashes for greater platform compatibility.
                    var videoPath = $"{_config.VideoPath}/{Enum.GetName(typeof(ViewerState), state)}.{_config.VideoExtension}";
                    return _explicitModeEnabled
                                ? GetFFPLAYArgs(videoPath, _config.TestModeEnabled, true)
                                : GetOmxplayerArgs(videoPath, _config.TestModeEnabled, true);
            }
        }

        private string GetFFPLAYArgs(string url, bool useTestMode, bool loop)
        {
            // Always play in full-screen mode (fs) unless in test mode.
            // Optionally loop the video.
            // Example: ffplay "offline.mp4" -loop 0 -fs
            // Note: This doesn't support quote chars right now because, in shell mode, we have to surround the whole command with quotes.
            var scaleArgs = useTestMode ? $"-x {TestModeWidth} -y {TestModeHeight}" : "-fs";
            var ffplayArgs = $"{url} {scaleArgs}";
            if (loop)
            {
                ffplayArgs += " -loop 0";
            }
            return ffplayArgs;
        }

        private string GetOmxplayerArgs(string url, bool useTestMode, bool loop)
        {
            // Omxplayer auto-plays in full-screen (override for test mode).
            // Optionally loop the video.
            var omxArgs = $"\"{url}\"";
            if (loop)
            {
                omxArgs += " --loop";
            }
            if(useTestMode)
            {
                omxArgs += $" --win 0,0,{TestModeWidth},{TestModeHeight}";
            }
            return omxArgs;
        }
    }
}
