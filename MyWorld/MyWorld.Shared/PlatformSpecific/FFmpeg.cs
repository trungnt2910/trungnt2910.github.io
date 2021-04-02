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
        public delegate Task<Stream> ParamsFunc(params string[] param);
        /// <summary>
        /// Runs the FFmpeg application, as if invoked on the command line.
        /// </summary>
        /// <param name="param">Command line arguments. The last argument MUST be the output file.</param>
        /// <returns>A Stream containing the temporary output file.</returns>
        public static ParamsFunc RunAsync = (param) => { throw new PlatformNotSupportedException("FFmpeg is not implemented for this platform"); };
        public static Func<Task> InitAsync = () => { return Task.Run(() => { }); };
        public static event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public static Action<object, ProgressChangedEventArgs> RaiseProgressChanged = (sender, args) => ProgressChanged?.Invoke(sender, args);

        #region IO
        /// <summary>
        /// Opens a temporary input file and make it visible to FFmpeg.
        /// </summary>
        /// <param name="path">Path of the input file in FFmpeg's virtual file system.</param>
        /// <returns>A Stream for writing input to the temporary file.</returns>
        public static Func<string, Stream> OpenInputFile = (path) => { throw new PlatformNotSupportedException("FFmpeg is not implemented for this platform"); };
        /// <summary>
        /// Deletes a file in FFmpeg's virtual file system.
        /// </summary>
        /// <param name="path">Path of the file in FFmpeg's virtual file system.</param>
        public static Action<string> DeleteFile = (path) => { throw new PlatformNotSupportedException("FFmpeg is not implemented for this platform"); };
        #endregion
    };
}