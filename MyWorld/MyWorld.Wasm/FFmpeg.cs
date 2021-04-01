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
                ffmpeg.run('-i', '{WebAssemblyRuntime.EscapeJs(video)}', '-i', '{WebAssemblyRuntime.EscapeJs(audio)}', '{WebAssemblyRuntime.EscapeJs(output)}');");
            return FFmpegFileStream.OpenRead(output);
        }
        private static void LibraryLog(string message)
        {
            Console.WriteLine($"FFmpeg: {message}");
        }
        private static void InteropLog(string message)
        {
            Console.WriteLine($"Interop: {message}");
        }
        private static void ProgressLog(string message)
        {
            ProgressChanged?.Invoke(null, new ProgressChangedEventArgs((int)(double.Parse(message) * 100), null));
            Console.WriteLine($"Progress: {message}");
        }
        private static void LibraryLoaded(object sender, EventArgs args)
        {
            Console.WriteLine("C#: Received library loaded event.");
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
            Console.WriteLine("C#: Begin init...");
            await WebAssemblyRuntime.InvokeAsync("Init()");
            await WebAssemblyRuntime.InvokeAsync("Load()");
            Interlocked.Exchange(ref libraryState, 2);
            Loaded = true;
            Console.WriteLine("C#: Done ffmpeg initialization");
        }
    }
}
