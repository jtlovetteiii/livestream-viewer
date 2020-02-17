using LivestreamViewer.Config;
using LivestreamViewer.Monitoring;
using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LivestreamViewer
{
    // Notes (document this):
    // * Don't run this program on the same computer as you are broadcasting with OBS; it causes odd artifacts in FFPLAY.

    /* 
     * [Build (Linux)]
     *  dotnet publish --runtime linux-arm --self-contained
     *  Copy \LivestreamViewer\bin\Debug\netcoreapp2.1\linux-arm\publish to the target computer.
     * 
     * [Prerequisites (Linux)]
     *  $ sudo apt-get install omxplayer
     *  $ sudo apt-get install ffplay
     *  $ sudo apt-get install libssl-dev
     *  $ sudo apt-get install libssl1.0.2
     * 
     * [Run (Linux)]
     *  $ ./LivestreamViewer
     *  
     * [Build (Windows)]
     *  dotnet publish --runtime win-x86 --self-contained
     *  (https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)
     *  Copy \LivestreamViewer\bin\Debug\netcoreapp2.3\win-x86\publish to the target computer.
     * 
     * [Prerequisites (Windows)]
     *  Download FFMPEG: https://ffmpeg.zeranoe.com/builds/
     *  Copy the downloaded files to a folder on the target computer (e.g. C:\ffmpeg).
     *  Ensure that the folder contains the following files:
     *  - ffmpeg.exe
     *  - ffplay.exe
     *  - ffprobe.exe
     * 
     * [Run (Windows)]
     *  > LivestreamViewer.exe
    */
    class Program
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        static CancellationTokenSource TokenSource;

        public static void Main(string[] args)
        {
            // Console log to let us know the app started in case log4net can't configure.
            Console.WriteLine("Livestream Viewer is launching.");
            InitLogger();

            Log.Info("Loading configuration.");
            var config = LivestreamClientConfig.FromLocalFile(args);
            config.EnsureValid();
            Log.Info($"Configuration loaded: {config}");

            // TODO: Repeat on failure until stopped?
            TokenSource = new CancellationTokenSource();
            var task = RunClient(config, TokenSource.Token);

            // Support graceful stop for interactive console.
            // There isn't any cleanup that has to be performed, so we don't
            // need to worry about hooking into the kill signal (which only
            // gives us three seconds, anyway).
            // NOTE: Some environments do not allow STDIO calls (like Console.Read)
            // when full-screen apps like omxplayer are active.
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            // Wait until a cancellation request occurs.
            TokenSource.Token.WaitHandle.WaitOne();

            // After cancellation, wait for the main viewer task to finish.
            task.Wait();
            Log.Info("Livestream Client stopped.");
        }

        /// <summary>
        /// Sets up log4net using a local configuration file.
        /// </summary>
        private static void InitLogger()
        {
            // Note: Using Console logging before initialization and on error
            // since, until log4net is successfully initialized, that is the 
            // only way to know what is happening.
            try
            {
                Console.WriteLine("Initializing log4net.");
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing logger! Logs will not be written. {ex}");
            }
        }

        /// <summary>
        /// Returns a task that executes the persistent viewer.
        /// </summary>
        private static async Task RunClient(LivestreamClientConfig config, CancellationToken token)
        {
            Log.Info("Starting Livestream Client.");
            var monitor = new FFMPEGLivestreamMonitor(config);
            var videoResolver = new VideoCommandResolver(config);
            var viewer = new LivestreamViewer(config, monitor, videoResolver);
            await viewer.KeepViewerActive(token);
        }

        /// <summary>
        /// Attempts to initiate a graceful exit on process termination.
        /// </summary>
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Log.Info("Stopping Livestream Client.");
            TokenSource.Cancel();
        }
    }
}
