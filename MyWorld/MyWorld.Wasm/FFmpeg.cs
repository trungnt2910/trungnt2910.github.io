using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation;

namespace MyWorld.Wasm
{
    public static class FFmpeg
    {
        private static long libraryState = 0;
        public static bool Loaded { get; private set; } = false;
        public static async Task<Stream> MergeToFileAsync(string video, string audio, string output)
        {
            await WebAssemblyRuntime.InvokeAsync($@"
                ffmpeg.run('-i', '{WebAssemblyRuntime.EscapeJs(video)}', '-i', '{WebAssemblyRuntime.EscapeJs(audio)}', '-c', 'copy', '{WebAssemblyRuntime.EscapeJs(output)}');");
            return FFmpegFile.OpenRead(output);
        }
        /// <summary>
        /// Runs the FFmpeg application, as if invoked on the command line.
        /// </summary>
        /// <param name="param">Command line arguments. The last argument MUST be the output file.</param>
        /// <returns>A Stream containing the output file.</returns>
        public static async Task<Stream> RunAsync(params string [] param)
        {
            await WebAssemblyRuntime.InvokeAsync($@"
                ffmpeg.run({string.Join(",", param.Select(x => $@"'{WebAssemblyRuntime.EscapeJs(x)}'"))});
            ");
            return FFmpegFile.OpenRead(param.Last());
        }
        private static void LibraryLog(string message)
        {
            // Console spamming is not good 
            System.Diagnostics.Debug.WriteLine($"FFmpeg: {message}");
        }
        private static void InteropLog(string message)
        {
            System.Diagnostics.Debug.WriteLine($"Interop: {message}");
        }
        private static void ProgressLog(string message)
        {
            ProgressChanged?.Invoke(null, new ProgressChangedEventArgs((int)(double.Parse(message) * 100), null));
            Console.WriteLine($"Progress: {message}");
        }
        private static void LibraryLoaded(object sender, EventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("C#: Received library loaded event.");
            Interlocked.Exchange(ref libraryState, 2);
        }
        public static event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public static async Task InitAsync()
        {
            if (Interlocked.Read(ref libraryState) != 0)
            {
                throw new InvalidOperationException("Init() or InitAsync() has already been called.");
            }
            Interlocked.Exchange(ref libraryState, 1);
            System.Diagnostics.Debug.WriteLine("C#: Begin init...");
            await WebAssemblyRuntime.InvokeAsync("Init()");
            await WebAssemblyRuntime.InvokeAsync("Load()");
            Interlocked.Exchange(ref libraryState, 2);
            Loaded = true;
            System.Diagnostics.Debug.WriteLine("C#: Done ffmpeg initialization");
        }
    }
}
