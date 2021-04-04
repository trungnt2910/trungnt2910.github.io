const libraryLog = Module.mono_bind_static_method(
    "[MyWorld.Wasm] MyWorld.Wasm.FFmpeg:LibraryLog");
const interopLog = Module.mono_bind_static_method(
    "[MyWorld.Wasm] MyWorld.Wasm.FFmpeg:InteropLog");
const progressLog = Module.mono_bind_static_method(
    "[MyWorld.Wasm] MyWorld.Wasm.FFmpeg:ProgressLog");
const loadedEvent = Module.mono_bind_static_method(
    "[MyWorld.Wasm] MyWorld.Wasm.FFmpeg:LibraryLoaded");

interopLog('FFmpegLoader loaded.');

var ffmpeg;

async function Init() {
    await require(['https://unpkg.com/@ffmpeg/ffmpeg@0.9.7/dist/ffmpeg.min.js'], (FFmpeg) => {
        const { createFFmpeg, fetchFile } = FFmpeg;
        interopLog('Creating FFmpeg');
        ffmpeg = createFFmpeg({
            log: false,
            logger: ({ message }) => libraryLog(message.toString()),
            progress: ({ ratio }) => progressLog(ratio.toString())
        });
        interopLog('Done');
    });
}

async function Load() {
    interopLog('Loading FFmpeg');
    await ffmpeg.load();
    interopLog('Done');
}

let fileStreams = {};

function PtrToUint8Array(ptr, size) {
    interopLog('Created Uint8Array at address: ' + ptr);
    return new Uint8Array(Module.HEAPU8.buffer, ptr, size);
}