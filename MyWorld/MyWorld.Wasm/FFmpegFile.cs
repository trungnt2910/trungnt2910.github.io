using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Uno.Foundation;

namespace MyWorld.Wasm
{
    public static class FFmpegFile
    {
        public static Stream OpenRead(string fileName)
        {
            return new FFmpegFileStream(fileName, "r");
        }

        public static Stream OpenWrite(string fileName)
        {
            return new FFmpegFileStream(fileName, "w");
        }

        public static void Delete(string fileName)
        {
            WebAssemblyRuntime.InvokeJS($@"ffmpeg.FS('unlink', '{WebAssemblyRuntime.EscapeJs(fileName)}');");
        }
    }
}
