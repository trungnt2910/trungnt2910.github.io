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

            Console.WriteLine(request.RequestUri);
            Console.WriteLine(requestMessage.RequestUri);

            var response = await base.SendAsync(requestMessage, cancellationToken);
            response.RequestMessage = response.RequestMessage ?? requestMessage;

            return response;
        }
    }
}