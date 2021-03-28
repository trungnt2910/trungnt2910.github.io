using System;
using System.Net.Http;
using Windows.UI.Xaml;

namespace MyWorld.Wasm
{
    public class Program
    {
        private static App _app;

        static int Main(string[] args)
        {
            PlatformSpecific.Http.DefaultClient = new HttpClient(new CorsBypassHandler());
            PlatformSpecific.Http.FetchRange = JavascriptFetcher.FetchProxy;

            Windows.UI.Xaml.Application.Start(_ => _app = new App());

            return 0;
        }
    }
}
