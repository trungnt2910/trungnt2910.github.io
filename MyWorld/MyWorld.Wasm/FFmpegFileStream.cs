using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.Foundation;

namespace MyWorld.Wasm
{
    public class FFmpegFileStream : Stream
    {
        private bool canWrite = false;
        private bool canRead = false;
        private bool closed = false;
        private long pos;
        private string uuid;
        private string fileName;

        internal FFmpegFileStream(string fileName, string mode)
        {
            if (!FFmpeg.Loaded)
                throw new InvalidOperationException("Cannot use FFmpegFileStream before initializing FFmpeg.");
            uuid = System.Guid.NewGuid().ToString();
            this.fileName = fileName;
            WebAssemblyRuntime.InvokeJS($@"fileStreams['{uuid}'] = ffmpeg.FS('open', '{WebAssemblyRuntime.EscapeJs(fileName)}', '{mode}')");
            canWrite = mode.Contains("r");
            canRead = mode.Contains("w");
        }

        public override void SetLength(long pos)
        {
            throw new NotSupportedException();
        }

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            //No-op
        }

        public override bool CanRead
        {
            get => canRead;
        }

        public override long Seek(long pos, SeekOrigin origin)
        {
            WebAssemblyRuntime.InvokeJS($@"ffmpeg.FS('llseek', fileStreams['{uuid}'], {(int)origin})");
            return 0;
        }

        public override bool CanSeek
        {
            get => false;
        }

        public override int Read(byte[] buffer, int offset, int length)
        {
            var handle = GCHandle.Alloc(buffer);
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
            WebAssemblyRuntime.InvokeJS($@"ffmpeg.FS('read', fileStreams['{uuid}'], PtrToUint8Array({ptr.ToInt64()}, {length}), 0, {length})");
            handle.Free();
            return length;
        }

        public override bool CanWrite
        {
            get => canWrite;
        }

        public override void Write(byte[] buffer, int offset, int length)
        {
            var handle = GCHandle.Alloc(buffer);
            var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
            WebAssemblyRuntime.InvokeJS($@"ffmpeg.FS('write', fileStreams['{uuid}'], PtrToUint8Array({ptr.ToInt64()}, {length}), 0, {length})");
            handle.Free();
        }

        public override long Length
        {
            get => int.Parse(WebAssemblyRuntime.InvokeJS($@"ffmpeg.FS('stat', '{WebAssemblyRuntime.EscapeJs(fileName)}').size.toString()"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (uuid != null)
                {
                    WebAssemblyRuntime.InvokeJS($@"
                    {{
                        ffmpeg.FS('close', fileStreams['{uuid}']);
                    }}");
                    WebAssemblyRuntime.InvokeJS($@"delete fileStreams['{uuid}']");
                    uuid = null;
                }
            }
        }
    }
}
