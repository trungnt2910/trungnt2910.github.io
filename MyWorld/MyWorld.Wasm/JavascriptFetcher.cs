using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uno.Foundation;

namespace MyWorld.Wasm
{
    public static class JavascriptFetcher
    {
        public static async Task FetchNoCors(string url)
        {
            Console.WriteLine("LMAO");
            var result = await WebAssemblyRuntime.InvokeAsync($"fetch('{url}', {{mode: 'no-cors'}}).then(res => res.blob()).then(blob => {{return blob.text();}});");
            Console.WriteLine(result);
            Console.WriteLine($"{result.Length} bytes received.");
        }

        public static async Task FetchNoCors(string url, long from, long to)
        {
            Console.WriteLine("LMAO");
            var result = await WebAssemblyRuntime.InvokeAsync($"fetch('{url}', {{mode: 'no-cors', headers: {{'range': 'bytes={from}-{to}'}}}});");
            Console.WriteLine(result);
        }

        public static async Task<byte[]> FetchProxy(string url)
        {
            var result = await WebAssemblyRuntime.InvokeAsync($@"
            {{
                    const buf2hex = (buffer) => 
                    {{ // buffer is an ArrayBuffer
                        return Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join('');
                    }}
                    return fetch('https://cors.bridged.cc/{url}')
                    .then(response => response.blob())
                    .then(blob => blob.arrayBuffer())
                    .then(buffer => buf2hex(buffer));
            }}");

            return HexStringToByte(result);
        }

        [Obsolete("Fixed already, suckers!")]
        // This thing is only needed as Mono doesn't seem to handle range queries properly.
        public static async Task<byte[]> FetchProxy(string url, long from, long to)
        {
            Console.WriteLine("No, don't go here!");
            var result = await WebAssemblyRuntime.InvokeAsync($@"
            {{
                    const buf2hex = (buffer) => 
                    {{ // buffer is an ArrayBuffer
                        return Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join('');
                    }}

                    return fetch('https://cors.bridged.cc/{url}',
                        {{
                            headers: {{'range': 'bytes={from}-{to}'}}
                        }})
                    .then(response => response.blob())
                    .then(blob => blob.arrayBuffer())
                    .then(buffer => buf2hex(buffer));
            }}");

            return HexStringToByte(result);
        }

        private static byte[] HexStringToByte(string hex)
        {
            if ((hex.Length & 1) == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}