using log4net;
using System;

namespace LivestreamViewer.Util
{
    /// <summary>
    /// Supports configurable retry of operations.
    /// </summary>
    public static class Retry
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Retry));

        /// <summary>
        /// Repeatedly executes the provided function until it either returns a value of true or
        /// the maximum number of retries is reached. Note that a value of 0 for maxRetries is
        /// automatically increased to 1 (i.e. this function always executes func at least once).
        /// </summary>
        public static void WithRetry(Func<bool> func, int maxRetries)
        {
            if (maxRetries < 1)
            {
                maxRetries = 1;
            }
            for (var i = 0; i < maxRetries; i++)
            {
                Log.Debug($"WithRetry beginning attempt {i + 1} of {maxRetries}.");
                var currentResult = false;
                try
                {
                    currentResult = func();
                    if (currentResult)
                    {
                        // Exit early if the function returned a successful value.
                        Log.Debug($"WithRetry completed successfully during attempt {i} of {maxRetries}.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"WithRetry error during attempt {i} of {maxRetries}: {ex}");
                }
            }
            Log.Warn($"WithRetry completed all attempts ({maxRetries}) without a success code.");
        }
    }
}
