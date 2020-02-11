namespace LivestreamViewer.Constants
{
    /// <summary>
    /// Defines various states that the livestream viewer can exhibit.
    /// </summary>
    public enum ViewerState
    {
        Unset = 0,
        /// <summary>
        /// The viewer is online but the livestream is not active.
        /// </summary>
        OffAir,
        /// <summary>
        /// The livestream is active.
        /// </summary>
        Livestream,
        /// <summary>
        /// The viewer is offline.
        /// </summary>
        Offline
    }
}
