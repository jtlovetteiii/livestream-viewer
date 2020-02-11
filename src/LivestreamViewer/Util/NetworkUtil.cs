using System.Net.Http;
using System.Threading.Tasks;

namespace LivestreamViewer.Util
{
    public static class NetworkUtil
    {
        /// <summary>
        /// Tests internet connectivity by performing an HTTP GET on the
        /// specified URL and testing for a successful response.
        /// </summary>
        /// <param name="url">A URL to test.</param>
        /// <returns>A boolean value indicating whether the HTTP GET 
        /// received a successful response.</returns>
        public static async Task<bool> IsInternetAvailable(string url)
        {
            using (var client = new HttpClient())
            {
                var resp = await client.GetAsync(url);
                return resp.IsSuccessStatusCode;
            }
        }
    }
}
