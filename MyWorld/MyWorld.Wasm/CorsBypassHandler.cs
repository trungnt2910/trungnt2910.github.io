using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MyWorld.Wasm
{
    public class CorsBypassHandler : DelegatingHandler
    {
        public static bool IsEnabled = true;

        public CorsBypassHandler()
        {
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!IsEnabled)
            {
                Console.WriteLine(request.RequestUri);
                return await base.SendAsync(request, cancellationToken);
            }
            var requestMessage = new HttpRequestMessage(request.Method, $"https://cors.bridged.cc/{request.RequestUri}");

            // Forwards headers.
            foreach (var kvp in request.Headers)
            {
                requestMessage.Headers.Add(kvp.Key, kvp.Value);
            }

            // Enforce PC user agent. This should not affect our cross-platform web app.
            if (requestMessage.Headers.Contains("User-Agent"))
            {
                requestMessage.Headers.Remove("User-Agent");
            }

            // This agent is known to have worked.
            // Howver, setting the agent is currently impossible in Chrome and Edge because of a bug in Chromium.
            // https://bugs.chromium.org/p/chromium/issues/detail?id=571722
            requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.90 Safari/537.36 Edg/89.0.774.63");

            Console.WriteLine(request.RequestUri);
            Console.WriteLine(requestMessage.RequestUri);

            var response = await base.SendAsync(requestMessage, cancellationToken);
            response.RequestMessage = response.RequestMessage ?? requestMessage;

            return response;
        }
    }
}