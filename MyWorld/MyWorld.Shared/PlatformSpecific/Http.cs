using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace MyWorld.PlatformSpecific
{
    public class Http
    {
        public static HttpClient DefaultClient = new HttpClient();
        public static Func<string, Task<byte[]>> Fetch = (string url) =>
        {
            return DefaultClient.GetByteArrayAsync(url);
        };
        public static Func<string, long, long, Task<byte[]>> FetchRange = async (string url, long from, long to) =>
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("range", $"bytes={from}-{to}");
            var response = await DefaultClient.SendAsync(requestMessage);
            return await response.Content.ReadAsByteArrayAsync();
        };
    }
}
