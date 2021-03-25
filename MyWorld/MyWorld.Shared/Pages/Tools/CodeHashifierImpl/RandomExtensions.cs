using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWorld.Pages.Tools.CodeHashifierImpl
{
    internal static class RandomExtensions
    {
        private static char[] Letters = Enumerable.Range(0, 128).Where(x => char.IsLetter((char)x)).Select(x => (char)x).ToArray();
        private static char[] LettersAndDigits = Enumerable.Range(0, 128).Where(x => char.IsLetterOrDigit((char)x)).Select(x => (char)x).ToArray();
        public static bool NextBoolean(this Random random)
        {
            return random.Next(0, 1) == 1;
        }
        public static string NextAlphaNumericString(this Random random)
        {
            // In the original C++ code, we had 9 elements in the buffer.
            // C# does not use the null character for termination, so we only need 8.
            char[] buffer = new char[8];

            buffer[0] = Letters[random.Next(0, Letters.Length - 1)];
            for (int i = 1; i < 8; ++i)
            {
                buffer[i] = LettersAndDigits[random.Next(0, LettersAndDigits.Length - 1)];
            }

            return new string(buffer);
        }
        public static void ShuffleList<T>(this Random random, IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; --i)
            {
                int index = random.Next(0, i);
                T temp = list[i];
                list[i] = list[index];
                list[index] = temp;
            }
        }
    }
}
