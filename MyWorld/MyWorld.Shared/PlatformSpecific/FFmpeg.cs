using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MyWorld.PlatformSpecific
{
    public static class FFmpeg
    {
        public static Func<bool> IsLoaded;
        public static Func<string, string, Task<byte[]>> MergeAsync = (videoPath, audioPath) => { throw new PlatformNotSupportedException("FFmpeg is not implemented for this platform"); };
        public static Func<string, string, string, Task<Stream>> MergeToFileAsync = (videoPath, audioPath, outputPath) => { throw new PlatformNotSupportedException("FFmpeg is not implemented for this platform"); };
        public static Func<Task> InitAsync = () => { return Task.Run(() => { }); };
        public static Func<string, Stream> GetInputFileStream = (name) => { throw new PlatformNotSupportedException(); };
        public static event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public static Action<object, ProgressChangedEventArgs> RaiseProgressChanged = (sender, args) => ProgressChanged?.Invoke(sender, args);
    };
}