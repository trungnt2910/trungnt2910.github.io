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
            PlatformSpecific.FFmpeg.RunAsync = FFmpeg.RunAsync;
            PlatformSpecific.FFmpeg.OpenInputFile = FFmpegFile.OpenWrite;
            PlatformSpecific.FFmpeg.DeleteFile = FFmpegFile.Delete;
            FFmpeg.ProgressChanged += (sender, eventArgs) => { PlatformSpecific.FFmpeg.RaiseProgressChanged(sender, eventArgs); };

            Windows.UI.Xaml.Application.Start(_ => _app = new App());

            return 0;
        }
    }
}
