using log4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LivestreamViewer.Config
{
    /// <summary>
    /// Defines runtime config for the livestream client.
    /// </summary>
    public class LivestreamClientConfig
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(LivestreamClientConfig));

        private const string TestModeSwitch = "--test";

        private const string DefaultVideoPath = "video";
        private const string DefaultVideoExtension = "mp4";
        private const string DefaultInternetTestUrl = "https://www.google.com";
        private const int DefaultHealthCheckGracePeriod = 30;
        private const int DefaultHealthCheckDelay = 30;
        private const int DefaultHealthCheckRetries = 1;

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
        /// Whether this is actually a livestream URL or a redirect depends on the value
        /// of EvaluateLivestreamUrl.
        /// </summary>
        private string _livestreamUrl;

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
        /// If set, ensures that only a single instance of the application
        /// runs at any given time. During startup, a new instance will check
        /// to see if there is already a healthy running instance (i.e. an
        /// instance of this application plus ffplay or omxplayer); if there is,
        /// then the new instance will terminate. If not, then the new instance
        /// will terminate any other instances of this application along with
        /// any running ffmpeg, ffplay, and omxplayer instances before proceeding.
        /// </summary>
        public bool ForceSingleInstance { get; set; }

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
        /// Additional arguments to send to the video player at runtime.
        /// </summary>
        public string VideoPlayerArguments { get; set; }

        /// <summary>
        /// Whether to treat the value of LivestreamUrl as a "redirect". If true, then the
        /// application will issue an HTTP GET to the URL represented by LivestreamUrl and,
        /// if the resulting HTTP response is itself a URL, use that value for the livestream.
        /// If false (or the resulting HTTP response is not a URL), then no substitution will occur.
        /// </summary>
        public bool EvaluateLivestreamUrl { get; set; }

        /// <summary>
        /// The number of times to retry FFMPEG stream-monitoring activities in a single monitoring iteration.
        /// </summary>
        private int _healthCheckRetries;
        public int HealthCheckRetries
        {
            get
            {
                return _healthCheckRetries > 0 ? _healthCheckRetries : DefaultHealthCheckRetries;
            }
            set { _healthCheckRetries = value; }
        }

        /// <summary>
        /// Generates a new LivestreamClientConfig instance using values
        /// in a local appsettings.json file.
        /// </summary>
        public static LivestreamClientConfig FromLocalFile(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", true, true)
              .Build();
            var appConfig = new LivestreamClientConfig
            {
                // _livestreamUrl is unique in that it is a private, yet configurable, member.
                // The reason is to force callers to evaluate it via ResolveLivestreamUrl().
                _livestreamUrl = config["LivestreamUrl"],
                VideoPlayerPath = config[nameof(VideoPlayerPath)],
                VideoExtension = config[nameof(VideoExtension)],
                VideoPath = config[nameof(VideoPath)],
                InternetTestUrl = config[nameof(InternetTestUrl)],
                VideoPlayerArguments = config[nameof(VideoPlayerArguments)]
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
            if(bool.TryParse(config[nameof(ForceSingleInstance)], out var forceSingleInstance))
            {
                appConfig.ForceSingleInstance = forceSingleInstance;
            }
            if (int.TryParse(config[nameof(HealthCheckRetries)], out var healthCheckRetries))
            {
                appConfig.HealthCheckRetries = healthCheckRetries;
            }
            if (bool.TryParse(config[nameof(EvaluateLivestreamUrl)], out var evaluateLivestreamUrl))
            {
                appConfig.EvaluateLivestreamUrl = evaluateLivestreamUrl;
            }

            // Special handling for incoming arguments.
            if (args.Contains(TestModeSwitch))
            {
                appConfig.TestModeEnabled = true;
            }
            return appConfig;
        }

        /// <summary>
        /// Indicates whether the application will execute in test mode.
        /// Currently, this merely fixes video playback size.
        /// </summary>
        public bool TestModeEnabled { get; private set; }

        /// <summary>
        /// Validates the current configuration options, throwing an exception
        /// if invalid.
        /// </summary>
        public void EnsureValid()
        {
            // LivestreamUrl must be provided and be a valid URI.
            if (string.IsNullOrWhiteSpace(_livestreamUrl))
            {
                throw new Exception($"Missing required option: LivestreamUrl.");
            }
            if(!Uri.TryCreate(_livestreamUrl, UriKind.Absolute, out _))
            {
                throw new Exception($"Invalid value provided for LivestreamUrl: Must be a valid URI.");
            }
        }

        /// <summary>
        /// Evaluates the proper URL for the livestream, performing redirection if necessary.
        /// </summary>
        public async Task<string> ResolveLivestreamUrlAsync()
        {
            if (!EvaluateLivestreamUrl)
            {
                return _livestreamUrl;
            }
            _log.Debug($"Looking up livestream URL using redirect: {_livestreamUrl}");
            try
            {
                // Treat the value of LivestreamUrl as a URI which, when evaluated as HTTP GET, yields the real livestream URL.
                using(var client = new HttpClient())
                {
                    var response = await client.GetAsync(_livestreamUrl);
                    var redirectUrl = await response?.Content?.ReadAsStringAsync();
                    if (Uri.TryCreate(redirectUrl, UriKind.Absolute, out var parsedUrl))
                    {
                        // Let Uri clean up any bad chars for us.
                        _log.Debug($"Evaluated livestream URL: {parsedUrl}");
                        return parsedUrl.ToString();
                    }
                    _log.Warn($"Evaluated livestream URL ({redirectUrl}) is invalid. " +
                        $"Falling back to the configured value of LivestreamUrl.");
                    return _livestreamUrl;
                }
            }
            catch(Exception ex)
            {
                _log.Warn($"Unable to resolve livestream URL from redirect ({_livestreamUrl}). " +
                    $"Interpreting {_livestreamUrl} as the actual livestream URL. " +
                    $"The error was: {ex}");
                return _livestreamUrl;
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
