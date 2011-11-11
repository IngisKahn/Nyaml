namespace Nyaml.Canonical
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public class Scanner : IScanner
    {
        private readonly string data;
        private int index;
        private Queue<Tokens.Base> tokens = new Queue<Tokens.Base>();
        private bool hasScanned;

        public Scanner(string data)
        {
            this.data = data + '\0';
        }

        public bool CheckToken()
        {
            if (!this.hasScanned)
                this.Scan();
            return this.tokens.Count != 0;
        }

        public bool CheckToken<T>() where T : Tokens.Base
        {
            return this.PeekToken() is T;
        }

        public bool CheckToken<T1, T2>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
        {
            var token = this.PeekToken();
            return token != null && (token is T1 || token is T2);
        }

        public bool CheckToken<T1, T2, T3>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
        {
            var token = this.PeekToken();
            return token != null && (token is T1 || token is T2 || token is T3);
        }

        public bool CheckToken<T1, T2, T3, T4>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
            where T4 : Tokens.Base
        {
            var token = this.PeekToken();
            return token != null 
                && (token is T1 || token is T2 || token is T3 || token is T4);
        }

        public Tokens.Base PeekToken()
        {
            if (!this.hasScanned)
                this.Scan();
            return this.tokens.Count != 0 ? this.tokens.Peek() : null;
        }

        public Tokens.Base GetToken()
        {
            return this.GetToken<Tokens.Base>();
        }

        public T GetToken<T>() where T : Tokens.Base
        {
            var token = this.PeekToken() as T;
            if (token == null)
                throw new Error("unexpected token: " + this.PeekToken());
            this.tokens.Dequeue();
            return token;
        }

        private void Scan()
        {
            this.tokens.Enqueue(new Tokens.StreamStart());   
            for (;;)
            {
                this.FindToken();
                switch (this.data[this.index])
                {
                    case '\0':
                        this.tokens.Enqueue(new Tokens.StreamEnd());
                        this.hasScanned = true;
                        return;
                    case '%':
                        this.tokens.Enqueue(this.ScanDirective());
                        break;
                    case '-':
                        if (this.data.Substring(this.index, 3) != "---")
                            throw new Error("invalid token");
                        this.index += 3;
                        this.tokens.Enqueue(new Tokens.DocumentStart());
                        break;
                    case '[':
                        this.index++;
                        this.tokens.Enqueue(new Tokens.FlowSequenceStart());
                        break;
                    case '{':
                        this.index++;
                        this.tokens.Enqueue(new Tokens.FlowMappingStart());
                        break;
                    case ']':
                        this.index++;
                        this.tokens.Enqueue(new Tokens.FlowSequenceEnd());
                        break;
                    case '}':
                        this.index++;
                        this.tokens.Enqueue(new Tokens.FlowMappingEnd());
                        break;
                    case '?':
                        this.index++;
                        this.tokens.Enqueue(new Tokens.Key());
                        break;
                    case ':':
                        this.index++;
                        this.tokens.Enqueue(new Tokens.Value());
                        break;
                    case ',':
                        this.index++;
                        this.tokens.Enqueue(new Tokens.FlowEntry());
                        break;
                    case '*':
                    case '&':
                        this.tokens.Enqueue(this.ScanAlias());
                        break;
                    case '!':
                        this.tokens.Enqueue(this.ScanTag());
                        break;
                    case '"':
                        this.tokens.Enqueue(this.ScanScalar());
                        break;
                    default:
                        throw new Error("invalid token");
                }
            }
        }

        private void FindToken()
        {
            var found = false;
            while (!found)
            {
                while (" \t".IndexOf(this.data[this.index]) != -1)
                    this.index++;
                if (this.data[this.index] == '#')
                    while (this.data[this.index] != '\n')
                        this.index++;
                if (this.data[this.index] == '\n')
                    this.index++;
                else
                    found = true;
            }
        }

        private Tokens.Directive ScanDirective()
        {
            const string directive = "%YAML 1.1";
            if (this.data.Substring(this.index, directive.Length) == directive
                && " \n\0".IndexOf(this.data[this.index + directive.Length]) != -1)
            {
                this.index += directive.Length;
                return new Tokens.Directive { Name = "YAML", Value = Tuple.Create("1", "1") };
            }

            throw new Error("invalid token");
        }

        private Tokens.SimpleValue ScanAlias()
        {
            var token = this.data[this.index] == '*'
                                     ? (Tokens.SimpleValue)new Tokens.Alias()
                                     : new Tokens.Anchor();
            var start = ++this.index;
            while (", \n\0".IndexOf(this.data[this.index]) == -1)
                this.index++;
            token.Value = this.data.Substring(start, this.index - start);
            return token;
        }

        private Tokens.Tag ScanTag()
        {
            var start = ++this.index;
            while (" \n\0".IndexOf(this.data[this.index]) == -1)
                this.index++;
            var value = this.data.Substring(start, this.index - start);
            var len = value.Length;
            if (len == 0)
                value = "!";
            else if (value[0] == '!')
                value = "tag:yaml.org,2002:" + value.Substring(1);
            else if (value[0] == '<' && value[len - 1] == '>')
                value = value.Substring(1, len - 2);
            else
                value = '!' + value;
            return new Tokens.Tag { Value = Tuple.Create((string)null, value) };
        }

        private static readonly Dictionary<char, int> quoteCodes =
            new Dictionary<char, int>
            {
                { 'x', 2 },
                { 'u', 4 },
                { 'U', 8 }
            };

        private static readonly Dictionary<char, char> quoteReplaces =
            new Dictionary<char, char>
            {
                {'0', '\0'},
                {'a', '\x7'},
                {'b', '\x8'},
                {'t', '\x9'},
                {'n', '\xA'},
                {'v', '\xB'},
                {'f', '\xC'},
                {'r', '\xD'},
                {'e', '\x1B'},
                {' ', '\x20'},
                {'"', '"'},
                {'\\', '\\'},
                {'N', '\x85'},
                {'_', '_'},
                {'L', '\u2028'},
                {'P', '\u2029'}
            };

        private Tokens.Scalar ScanScalar()
        {
            this.index++;
            var chunks = new StringBuilder();
            var start = this.index;
            var ignoreSpaces = false;
            while (this.data[this.index] != '"')
                if (this.data[this.index] == '\\')
                {
                    ignoreSpaces = false;
                    chunks.Append(this.data.Substring(start, this.index++ - start));
                    var ch = this.data[this.index++];
                    char rc;
                    int rl;
                    if (ch == '\n')
                        ignoreSpaces = true;
                    else if (quoteCodes.TryGetValue(ch, out rl))
                    {
                        var code = int.Parse(this.data.Substring(this.index, rl), NumberStyles.AllowHexSpecifier);
                        if (code < char.MaxValue)
                            chunks.Append((char)code);
                        else
                            chunks.Append(Encoding.UTF32.GetString(BitConverter.GetBytes(code)));
                        this.index += rl;
                    }
                    else if (!quoteReplaces.TryGetValue(ch, out rc))
                        throw new Error("invalid escape code");
                    else
                        chunks.Append(rc);
                    start = this.index;
                }
                else if (this.data[this.index] == '\n')
                {
                    chunks.Append(this.data.Substring(start, this.index - start));
                    chunks.Append(' ');
                    start = ++this.index;
                    ignoreSpaces = true;
                }
                else if (ignoreSpaces && this.data[this.index] == ' ')
                    start = ++this.index;
                else
                {
                    ignoreSpaces = false;
                    this.index++;
                }
            chunks.Append(this.data.Substring(start, this.index++ - start));
            return new Tokens.Scalar { Value = chunks.ToString(), IsPlain = false };
        }
    }
}
