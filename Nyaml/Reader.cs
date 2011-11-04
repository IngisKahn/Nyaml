namespace Nyaml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    
    public class Reader : IDisposable
    {
        [Serializable]
        public class Error : YamlError
        {
            public Error(string name, int position, int character, string encoding, string reason)
                : base(MakeMessage(name, position, character, encoding, reason))
            {
            }

            private static string MakeMessage(string name, int position, int character, string encoding, string reason)
            {
                if (character <= char.MaxValue)
                    return string.Format("'{0}' codec can't decode byte #x{1:2X}: {2}\n  in \"{3}\", position {4}",
                                         encoding, character, reason, name, position);
                return string.Format("unacceptable character #x{0:4X}: {1}\n  in \"{2}\", position {3}",
                                         character, reason, name, position);
            }
        }

        private readonly string name;
        private readonly StreamReader streamReader;
        //private int streamPointer;
        private bool isEof;
        private readonly MemoryStream memoryStream;
        private readonly StringBuilder buffer;
        private int pointer;
        public Encoding Encoding { get; private set; }
        public int Index { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        public Reader(string data)
        {
            this.name = "<unicode string>";
            this.buffer = new StringBuilder(data);
            this.Encoding = Encoding.Unicode;
            this.CheckPrintable(data);
            this.Encoding = Encoding.Unicode;
            this.buffer.Append('\0');
            this.isEof = true;
        }

        public Reader(byte[] data)
        {
            this.name = "<byte string>";
            this.memoryStream = new MemoryStream(data);
            this.streamReader = new StreamReader(this.memoryStream, Encoding.Default);
            this.buffer = new StringBuilder();
            this.DetermineEncoding();
        }

        public Reader(Stream stream)
        {
            this.streamReader = new StreamReader(stream, Encoding.Default);
            this.name = "<stream>";
            this.buffer = new StringBuilder();
            this.DetermineEncoding();
        }

        public char Peek(int index = 0)
        {
            index += this.pointer;
            if (index < 0)
                index = 0;
            if (index >= this.buffer.Length)
                this.Update(index + 1);
            return this.buffer[index];
        }

        public string Prefix(int length = 1)
        {
            if (this.pointer + length >= this.buffer.Length)
                this.Update(length);
            return this.buffer.ToString(this.pointer, this.pointer + length);
        }

        private static readonly HashSet<char> newLineChars = 
            new HashSet<char>(new[] { '\n', '\x85', '\u2028', '\u2029' });

        public void Forward(int length = 1)
        {
            if (this.pointer + length + 1 >= this.buffer.Length)
                this.Update(length + 1);
            while (length > 0)
            {
                var ch = this.buffer[this.pointer++];
                this.Index++;
                if (newLineChars.Contains(ch) || (ch == '\r' && this.buffer[this.pointer] != '\n'))
                {
                    this.Line++;
                    this.Column = 0;
                }
                else
                    this.Column++;
                length -= 1;
            }
        }

        public Mark Mark
        {
            get
            {
                if (this.streamReader == null)
                    return new Mark { Name = this.name, Index = this.Index, Line = this.Line,
                                      Column = this.Column, Buffer = this.buffer, Pointer = this.pointer };
                return new Mark { Name = this.name, Index = this.Index, Line = this.Line, Column = this.Column };
            }
        }

        private void DetermineEncoding()
        {
            while (!this.isEof && (this.buffer == null || this.buffer.Length < 2))
                this.UpdateRaw();
            if (this.streamReader != null)
                this.Encoding = this.streamReader.CurrentEncoding;
            this.Update(1);
        }

        private static readonly Regex re =
            new Regex("[^\x09\x0A\x0D\x20-\x7E\x85\xA0-\uD7FF\uE000-\uFFFD]", RegexOptions.Compiled); 

        private void CheckPrintable(string data)
        {
            var match = re.Match(data);
            if (!match.Success)
                return;
            var character = match.Groups[0].Captures[0].Value[0];
            var position = this.Index + (this.buffer.Length - this.pointer) + match.Index;
            throw new Error(this.name, position, character, this.Encoding.EncodingName, "special characters are not allowed");
        }

        private void Update(int length)
        {
            this.buffer.Remove(0, this.pointer);
            this.pointer = 0;
            while (this.buffer.Length < length)
            {
                if (!this.isEof)
                    this.UpdateRaw();
                if (this.isEof)
                    this.buffer.Append('\0');
            }
            this.CheckPrintable(this.buffer.ToString(this.pointer, Math.Min(this.buffer.Length - this.pointer, length)));
        }

        private void UpdateRaw(int size = 4096)
        {
            var temp = new char[size];
            var read = this.streamReader.ReadBlock(temp, 0, size);
            this.buffer.Append(temp, 0, read);
            if (read < size)
                this.isEof = true;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;
            if (this.streamReader != null)
                this.streamReader.Dispose();
            if (this.memoryStream != null)
                this.memoryStream.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
    }
}
