namespace LivestreamViewer.Util
{
    /// <summary>
    /// Defines a command for playing a video in a video player application.
    /// </summary>
    public class VideoPlayerCommand
    {
        /// <summary>
        /// The name of the video player application.
        /// </summary>
        public string PlayerName { get; set; }
        
        /// <summary>
        /// Arguments to provide to the video player application.
        /// </summary>
        public string PlayerArgs { get; set; }

        /// <summary>
        /// If true, interpret the value of PlayerName as a physical path.
        /// If false, interpret the value of PlayerName as a shell command.
        /// </summary>
        public bool UseExplicitMode { get; set; }
    }
}
