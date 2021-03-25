using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWorld.Pages.Tools.CodeHashifierImpl
{
    class WordReader : StringReader
    {
        Queue<char> currentLine = new Queue<char>();
        public WordReader(string s) : base(s)
        {

        }

        public string ReadWord()
        {
            if (currentLine.Count == 0) ReloadQueue();
            if (currentLine.Count != 0)
            {
                var result = new List<char>();
                while (currentLine.Count != 0 && char.IsWhiteSpace(currentLine.Peek())) currentLine.Dequeue();
                while (currentLine.Count != 0 && (!char.IsWhiteSpace(currentLine.Peek()))) result.Add(currentLine.Dequeue());
                return new string(result.ToArray());
            }
            else return null;
        }

        public override string ReadLine()
        {
            if (currentLine.Count != 0)
            {
                string s = new string(currentLine.ToArray());
                currentLine.Clear();
                return s;
            }
            else return base.ReadLine();
        }

        private void ReloadQueue()
        {
            var line = base.ReadLine();
            if (line != null)
            {
                foreach (char ch in line)
                {
                    currentLine.Enqueue(ch);
                }
            }
        }
    }
}
