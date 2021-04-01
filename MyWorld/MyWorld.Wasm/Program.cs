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

            PlatformSpecific.FFmpeg.InitAsync = FFmpeg.InitAsync;
            PlatformSpecific.FFmpeg.MergeToFileAsync = FFmpeg.MergeToFileAsync;
            PlatformSpecific.FFmpeg.IsLoaded = () => FFmpeg.Loaded;
            PlatformSpecific.FFmpeg.GetInputFileStream = (string name) =>
            {
                return FFmpegFileStream.OpenWrite(name);
            };
            FFmpeg.ProgressChanged += (sender, eventArgs) => { PlatformSpecific.FFmpeg.RaiseProgressChanged(sender, eventArgs); };

            Windows.UI.Xaml.Application.Start(_ => _app = new App());

            return 0;
        }
    }
}
