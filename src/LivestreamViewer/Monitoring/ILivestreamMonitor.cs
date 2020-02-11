using System.Threading;
using System.Threading.Tasks;

namespace LivestreamViewer.Monitoring
{
    /// <summary>
    /// Defines operations for livestream monitor implementations.
    /// </summary>
    public interface ILivestreamMonitor
    {
        /// <summary>
        /// Verifies whether a live broadcast is available at the specified URL.
        /// </summary>
        /// <param name="livestreamUrl">The URL of a livestream to test, including the stream key (if applicable).</param>
        /// <param name="token">A Cancellation Token which can terminate the test.</param>
        /// <returns>True if a live broadcast is available at the specified URL, and false if not.</returns>
        Task<bool> IsLivestreamHealthyAsync(string livestreamUrl, CancellationToken token);
    }
}
