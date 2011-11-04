using System.Collections.Generic;
using System.Text;

namespace Nyaml
{
    public class Mark
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public StringBuilder Buffer { get; set; }
        public int Pointer { get; set; }

        private readonly HashSet<char> invalid = new HashSet<char>( new[] {'\0', '\r', '\n', '\x85', '\u2028', '\u2029' });

        public string GetSnippet(int indent = 4, int maxLength = 75)
        {
            if (this.Buffer == null)
                return null;
            var head = string.Empty;
            var start = this.Pointer;
            while (start > 0 && !invalid.Contains(this.Buffer[start - 1]))
            {
                start--;
                if (this.Pointer - start <= maxLength/2 - 1) 
                    continue;
                head = " ... ";
                start += 5;
                break;
            }
            var tail = string.Empty;
            var end = this.Pointer;

            while (end < this.Buffer.Length && !invalid.Contains(this.Buffer[end]))
            {
                end++;
                if (end - this.Pointer <= maxLength/2 - 1) 
                    continue;
                tail = " ... ";
                end -= 5;
                break;
            }
            var snippet = new StringBuilder();

            snippet.Append(new string(' ', indent));
            snippet.Append(head);
            snippet.Append(this.Buffer.ToString(start, end - start));
            snippet.AppendLine(tail);
            snippet.Append(new string(' ', indent + this.Pointer - start + head.Length));
            snippet.Append('^');

            return snippet.ToString();
        }

        public override string ToString()
        {
            var snippet = this.GetSnippet();
            var where = string.Format("  in \"{0}\", line {1}, column {2}", this.Name, this.Line + 1, this.Column + 1);
            if (snippet != null)
                where += snippet;
            return where;
        }
    }
}
