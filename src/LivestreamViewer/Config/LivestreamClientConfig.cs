using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace LivestreamViewer.Config
{
    /// <summary>
    /// Defines runtime config for the livestream client.
    /// </summary>
    public class LivestreamClientConfig
    {
        private const string DefaultVideoPath = "video";
        private const string DefaultVideoExtension = "mp4";
        private const string DefaultInternetTestUrl = "https://google.com";
        private const int DefaultHealthCheckGracePeriod = 30;
        private const int DefaultHealthCheckDelay = 30;

        /// <summary>
        /// The path to the folder on disk where FFMPEG compiled executables reside.
        /// If provided, this folder must contain the following files:
        /// 1. ffmpeg.exe
        /// 2. ffplay.exe
        /// If not provided, then the application will assume that both ffmpeg
        /// and ffplay can be invoked from the shell.
        /// </summary>
        public string VideoPlayerPath { get; private set; }

        /// <summary>
        /// The URL for the livestream, including stream key (if applicable).
        /// </summary>
        public string LivestreamUrl { get; private set; }

        /// <summary>
        /// The path to a directory that contains static video files used
        /// when a livestream is not actively being viewed.
        /// </summary>
        public string VideoPath
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_videoPath) ? _videoPath : DefaultVideoPath;
            }
            set { _videoPath = value; }
        }
        private string _videoPath;

        /// <summary>
        /// The extension that static video files will share.
        /// </summary>
        public string VideoExtension
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_videoExtension) ? _videoExtension : DefaultVideoExtension;
            }
            set { _videoExtension = value; }
        }
        private string _videoExtension;

        /// <summary>
        /// The URL to use during Internet connectivity testing. The URL's
        /// relationship to the device's network should be equivalent to that
        /// of the livestream URL; for instance, if this device accesses the
        /// livestream across the Internet (as opposed to the local network),
        /// then this URL should also be an Internet-facing URL. If this device
        /// accesses the livestream across the local network, then this URL
        /// should be an internal network-facing address. In either case,
        /// the URL must accept HTTP GET requests.
        /// </summary>
        public string InternetTestUrl
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_internetTestUrl) ? _internetTestUrl : DefaultInternetTestUrl;
            }
            set { _internetTestUrl = value; }
        }
        private string _internetTestUrl;

        /// <summary>
        /// The amount of time (in seconds) to stream frames from the live stream
        /// during a health check before terminating the test and inspecting
        /// the downloaded frames.
        /// </summary>
        private int _healthCheckGracePeriod;
        public int HealthCheckGracePeriod
        {
            get
            {
                return _healthCheckGracePeriod > 0 ? _healthCheckGracePeriod : DefaultHealthCheckGracePeriod;
            }
            set { _healthCheckGracePeriod = value; }
        }

        /// <summary>
        /// The amount of time (in seconds) to wait between inspections of the
        /// live stream's current state.
        /// </summary>
        private int _healthCheckDelay;
        public int HealthCheckDelay
        {
            get
            {
                return _healthCheckDelay > 0 ? _healthCheckDelay : DefaultHealthCheckDelay;
            }
            set { _healthCheckDelay = value; }
        }

        /// <summary>
        /// Generates a new LivestreamClientConfig instance using values
        /// in a local appsettings.json file.
        /// </summary>
        public static LivestreamClientConfig FromLocalFile()
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", true, true)
              .Build();
            var appConfig = new LivestreamClientConfig
            {
                VideoPlayerPath = config[nameof(VideoPlayerPath)],
                VideoExtension = config[nameof(VideoExtension)],
                LivestreamUrl = config[nameof(LivestreamUrl)],
                VideoPath = config[nameof(VideoPath)],
                InternetTestUrl = config[nameof(InternetTestUrl)]
            };
            // Special handling for non-string properties. Use defaults if conversion fails.
            if(int.TryParse(config[nameof(HealthCheckDelay)], out var healthCheckDelay))
            {
                appConfig.HealthCheckDelay = healthCheckDelay;
            }
            if (int.TryParse(config[nameof(HealthCheckGracePeriod)], out var healthCheckGracePeriod))
            {
                appConfig.HealthCheckGracePeriod = healthCheckGracePeriod;
            }
            return appConfig;
        }

        /// <summary>
        /// Validates the current configuration options, throwing an exception
        /// if invalid.
        /// </summary>
        public void EnsureValid()
        {
            if (string.IsNullOrWhiteSpace(LivestreamUrl))
            {
                throw new Exception($"Missing required option: {nameof(LivestreamUrl)}");
            }
        }

        public override string ToString()
        {
            // For now, config values are primitives, so this will do.
            return string.Join(
                "; ", 
                GetType()
                .GetProperties()
                .Select(p => $"{p.Name}: {p.GetValue(this)}"));
        }
    }
}
