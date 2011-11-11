namespace Nyaml
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public interface IScanner
    {
        bool CheckToken();

        bool CheckToken<T>() where T : Tokens.Base;

        bool CheckToken<T1, T2>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base;

        bool CheckToken<T1, T2, T3>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base;

        bool CheckToken<T1, T2, T3, T4>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
            where T4 : Tokens.Base;

        Tokens.Base PeekToken();

        Tokens.Base GetToken();

        T GetToken<T>() where T : Tokens.Base;
    }

    public class Scanner : IScanner
    {
        [Serializable]
        public class Error : MarkedYamlError
        {
            public Error(string context = null, Mark contextMark = null,
            string problem = null, Mark problemMark = null, string note = null)
                : base(context, contextMark, problem, problemMark, note)
            { }
        }

        private class SimpleKey
        {
            public int TokenNumber { get; set; }
            public bool IsRequired { get; set; }
            public int Index { get; set; }
            public int Line { get; set; }
            public int Column { get; set; }
            public Mark Mark { get; set; }
        }

        private bool isDone;
        private int flowLevel;
        private readonly List<Tokens.Base> tokens = new List<Tokens.Base>();
        private int tokensTaken;
        private int indentLevel = -1;
        private readonly Stack<int> indents = new Stack<int>();

        private bool allowSimpleKey = true;
        private readonly Dictionary<int, SimpleKey> possibleSimpleKeys = new Dictionary<int, SimpleKey>();

        private readonly IReader reader;

        public Scanner(IReader reader)
        {
            this.reader = reader;
            this.FetchStreamStart();
        }

        private int Index { get { return this.reader.Index; } }
        private int Line { get { return this.reader.Line; } }
        private int Column { get { return this.reader.Column; } }
        private Mark Mark { get { return this.reader.Mark; } }

        private char Peek(int index = 0)
        {
            return this.reader.Peek(index);
        }

        private string Prefix(int length = 1)
        {
            return this.reader.Prefix(length);
        }

        private void Forward(int length = 1)
        {
            this.reader.Forward(length);
        }

        public bool CheckToken()
        {
            while (this.NeedMoreTokens)
                this.FetchMoreTokens();

            return this.tokens.Count > 0;
        }

        public bool CheckToken<T>() where T : Tokens.Base
        {
            return this.CheckToken() && this.tokens[0] is T;
        }

        public bool CheckToken<T1, T2>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
        {
            return this.CheckToken() && (this.tokens[0] is T1 || this.tokens[0] is T2);
        }

        public bool CheckToken<T1, T2, T3>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
        {
            return this.CheckToken() && (this.tokens[0] is T1 || this.tokens[0] is T2 || this.tokens[0] is T3);
        }

        public bool CheckToken<T1, T2, T3, T4>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
            where T4 : Tokens.Base
        {
            return this.CheckToken() &&
                (this.tokens[0] is T1
                || this.tokens[0] is T2
                || this.tokens[0] is T3
                || this.tokens[0] is T4
                );
        }

        public Tokens.Base PeekToken()
        {
            while (this.NeedMoreTokens)
                this.FetchMoreTokens();
            return this.tokens.Count > 0 ? this.tokens[0] : null;
        }

        public Tokens.Base GetToken()
        {
            return this.GetToken<Tokens.Base>();
        }

        public T GetToken<T>() where T : Tokens.Base
        {
            while (this.NeedMoreTokens)
                this.FetchMoreTokens();
            if (tokens.Count == 0)
                return null;
            var result = this.tokens[0] as T;
            if (result != null)
            {
                this.tokensTaken++;
                this.tokens.RemoveAt(0);
            }
            return result;
        }

        private bool NeedMoreTokens
        {
            get
            {
                if (this.isDone)
                    return false;
                if (this.tokens.Count == 0)
                    return true;

                this.StalePossibleSimpleKeys();
                return this.NextPossibleSimpleKey == this.tokensTaken;
            }
        }

        private void FetchMoreTokens()
        {
            this.ScanToNextToken();

            this.StalePossibleSimpleKeys();

            this.UnwindIndent(this.Column);

            var ch = this.Peek();

            switch (ch)
            {
                case '\0':
                    this.FetchStreamEnd();
                    return;
                case '%':
                    if (this.CheckDirective())
                    {
                        this.FetchDirective();
                        return;
                    }
                    break;
                case '-':
                    if (this.CheckDocumentStart())
                    {
                        this.FetchDocumentStart();
                        return;
                    }
                    break;
                case '.':
                    if (this.CheckDocumentEnd())
                    {
                        this.FetchDocumentEnd();
                        return;
                    }
                    break;
                case '[':
                    this.FetchFlowSequenceStart();
                    return;
                case '{':
                    this.FetchFlowMappingStart();
                    return;
                case ']':
                    this.FetchFlowSequenceEnd();
                    return;
                case '}':
                    this.FetchFlowMappingEnd();
                    return;
                case ',':
                    this.FetchFlowEntry();
                    return;
                case '?':
                    if (this.CheckKey())
                    {
                        this.FetchKey();
                        return;
                    }
                    break;
                case ':':
                    if (this.CheckValue())
                    {
                        this.FetchValue();
                        return;
                    }
                    break;
                case '*':
                    this.FetchAlias();
                    return;
                case '&':
                    this.FetchAnchor();
                    return;
                case '!':
                    this.FetchTag();
                    return;
                case '|':
                    if (this.flowLevel == 0)
                    {
                        this.FetchLiteral();
                        return;
                    }
                    break;
                case '>':
                    if (this.flowLevel == 0)
                    {
                        this.FetchFolded();
                        return;
                    }
                    break;
                case '\'':
                    this.FetchSingle();
                    return;
                case '"':
                    this.FetchDouble();
                    return;
            }

            if (ch == '-' && this.CheckBlockEntry())
            {
                this.FetchBlockEntry();
                return;
            }

            if (this.CheckPlain())
            {
                this.FetchPlain();
                return;
            }

            throw new Error("while scanning for the next token", null,
                string.Format("found character {0} that cannot start any token", ch),
                this.Mark);
        }

        private int NextPossibleSimpleKey
        {
            get
            {
                var minTokenNumber = -1;
                foreach (var kvp in this.possibleSimpleKeys)
                    if (minTokenNumber == -1 || kvp.Value.TokenNumber < minTokenNumber)
                        minTokenNumber = kvp.Value.TokenNumber;
                return minTokenNumber;
            }
        }

        private void StalePossibleSimpleKeys()
        {
            foreach (var kvp in this.possibleSimpleKeys.ToList()
                .Where(kvp => kvp.Value.Line != this.Line
                              || this.Index - kvp.Value.Index > 1024))
            {
                if (kvp.Value.IsRequired)
                    throw new Error("while scanning a simple key", kvp.Value.Mark,
                                    "could not find expected ':'", this.Mark);

                this.possibleSimpleKeys.Remove(kvp.Key);
            }
        }

        private void SavePossibleSimpleKey()
        {
            var isRequired = this.flowLevel > 0 && this.indentLevel == this.Column;

            Debug.Assert(this.allowSimpleKey || !isRequired);

            if (!this.allowSimpleKey)
                return;

            this.RemovePossibleSimpleKey();
            var tokenNumber = this.tokensTaken + this.tokens.Count;
            var key = new SimpleKey
                      {
                          TokenNumber = tokenNumber,
                          IsRequired = isRequired,
                          Index = this.Index,
                          Line = this.Line,
                          Column = this.Column,
                          Mark = this.Mark
                      };
            this.possibleSimpleKeys[this.flowLevel] = key;
        }

        private void RemovePossibleSimpleKey()
        {
            SimpleKey key;
            if (!this.possibleSimpleKeys.TryGetValue(this.flowLevel, out key))
                return;
            if (key.IsRequired)
                throw new Error("while scanning a simple key", key.Mark,
                                "could not find expected ':'", this.Mark);
            this.possibleSimpleKeys.Remove(this.flowLevel);
        }

        private void UnwindIndent(int column)
        {
            if (this.flowLevel != 0)
                return;
            while (this.indentLevel > column)
            {
                var mark = this.Mark;
                this.indentLevel = this.indents.Pop();
                this.tokens.Add(new Tokens.BlockEnd { StartMark = mark, EndMark = mark });
            }
        }

        private bool AddIndent(int column)
        {
            if (this.indentLevel >= column)
                return false;
            this.indents.Push(this.indentLevel);
            this.indentLevel = column;
            return true;
        }

        private void FetchStreamStart()
        {
            var mark = this.Mark;
            this.tokens.Add(new Tokens.StreamStart { StartMark = mark, EndMark = mark, Encoding = this.reader.Encoding });
        }

        private void FetchStreamEnd()
        {
            this.UnwindIndent(-1);

            this.RemovePossibleSimpleKey();
            this.allowSimpleKey = false;
            this.possibleSimpleKeys.Clear();

            var mark = this.Mark;

            this.tokens.Add(new Tokens.StreamEnd { StartMark = mark, EndMark = mark });
            this.isDone = true;
        }

        private void FetchDirective()
        {
            this.UnwindIndent(-1);

            this.RemovePossibleSimpleKey();
            this.allowSimpleKey = false;

            this.tokens.Add(this.ScanDirective());
        }

        private void FetchDocumentStart()
        {
            this.FetchDocumentIndicator<Tokens.DocumentStart>();
        }

        private void FetchDocumentEnd()
        {
            this.FetchDocumentIndicator<Tokens.DocumentEnd>();
        }

        private void FetchDocumentIndicator<T>() where T : Tokens.Base, new()
        {
            this.UnwindIndent(-1);

            this.RemovePossibleSimpleKey();
            this.allowSimpleKey = false;

            var start = this.Mark;
            this.Forward(3);
            var end = this.Mark;
            this.tokens.Add(new T { StartMark = start, EndMark = end });
        }

        private void FetchFlowSequenceStart()
        {
            this.FetchFlowCollectionStart<Tokens.FlowSequenceStart>();
        }

        private void FetchFlowMappingStart()
        {
            this.FetchFlowCollectionStart<Tokens.FlowMappingStart>();
        }

        private void FetchFlowCollectionStart<T>() where T : Tokens.Base, new()
        {
            this.SavePossibleSimpleKey();

            this.flowLevel++;

            this.allowSimpleKey = true;

            var start = this.Mark;
            this.Forward();
            var end = this.Mark;
            this.tokens.Add(new T { StartMark = start, EndMark = end });
        }
        private void FetchFlowSequenceEnd()
        {
            this.FetchFlowCollectionEnd<Tokens.FlowSequenceEnd>();
        }

        private void FetchFlowMappingEnd()
        {
            this.FetchFlowCollectionEnd<Tokens.FlowMappingEnd>();
        }

        private void FetchFlowCollectionEnd<T>() where T : Tokens.Base, new()
        {
            this.RemovePossibleSimpleKey();

            this.flowLevel--;

            this.allowSimpleKey = false;

            var start = this.Mark;
            this.Forward();
            var end = this.Mark;
            this.tokens.Add(new T { StartMark = start, EndMark = end });
        }

        private void FetchFlowEntry()
        {
            this.allowSimpleKey = true;

            this.RemovePossibleSimpleKey();

            var start = this.Mark;
            this.Forward();
            var end = this.Mark;
            this.tokens.Add(new Tokens.FlowEntry { StartMark = start, EndMark = end });
        }

        private void FetchBlockEntry()
        {
            if (this.flowLevel == 0)
            {
                if (!this.allowSimpleKey)
                    throw new Error(null, null, "sequence entries are not allowed here", this.Mark);

                if (this.AddIndent(this.Column))
                {
                    var mark = this.Mark;
                    this.tokens.Add(new Tokens.BlockSequenceStart { StartMark = mark, EndMark = mark });
                }
            }

            this.allowSimpleKey = true;

            this.RemovePossibleSimpleKey();

            var start = this.Mark;
            this.Forward();
            var end = this.Mark;
            this.tokens.Add(new Tokens.BlockEntry { StartMark = start, EndMark = end });
        }

        private void FetchKey()
        {
            if (this.flowLevel == 0)
            {
                if (!this.allowSimpleKey)
                    throw new Error(null, null, "mapping keys are not allowed here", this.Mark);

                if (this.AddIndent(this.Column))
                {
                    var mark = this.Mark;
                    this.tokens.Add(new Tokens.BlockMappingStart { StartMark = mark, EndMark = mark });
                }
            }

            this.allowSimpleKey = this.flowLevel == 0;

            this.RemovePossibleSimpleKey();

            var start = this.Mark;
            this.Forward();
            var end = this.Mark;
            this.tokens.Add(new Tokens.Key { StartMark = start, EndMark = end });
        }

        private void FetchValue()
        {
            SimpleKey key;
            if (this.possibleSimpleKeys.TryGetValue(this.flowLevel, out key))
            {
                this.possibleSimpleKeys.Remove(this.flowLevel);
                this.tokens.Insert(key.TokenNumber - this.tokensTaken,
                    new Tokens.Key { StartMark = key.Mark, EndMark = key.Mark });

                if (this.flowLevel == 0 && this.AddIndent(key.Column))
                    this.tokens.Insert(key.TokenNumber - this.tokensTaken,
                        new Tokens.BlockMappingStart { StartMark = key.Mark, EndMark = key.Mark });
            }
            else
            {
                if (this.flowLevel == 0)
                {
                    if (!this.allowSimpleKey)
                        throw new Error(null, null, "mapping values are not allowed here", this.Mark);
                    if (this.AddIndent(this.Column))
                    {
                        var mark = this.Mark;
                        this.tokens.Add(new Tokens.BlockMappingStart { StartMark = mark, EndMark = mark });
                    }
                }

                this.allowSimpleKey = this.flowLevel == 0;

                this.RemovePossibleSimpleKey();
            }

            var start = this.Mark;
            this.Forward();
            var end = this.Mark;
            this.tokens.Add(new Tokens.Value { StartMark = start, EndMark = end });
        }

        private void FetchAlias()
        {
            this.SavePossibleSimpleKey();

            this.allowSimpleKey = false;

            this.tokens.Add(this.ScanAnchor<Tokens.Alias>());
        }

        private void FetchAnchor()
        {
            this.SavePossibleSimpleKey();

            this.allowSimpleKey = false;

            this.tokens.Add(this.ScanAnchor<Tokens.Anchor>());
        }

        private void FetchTag()
        {
            this.SavePossibleSimpleKey();

            this.allowSimpleKey = false;

            this.tokens.Add(this.ScanTag());
        }

        private void FetchLiteral()
        {
            this.FetchBlockScalar(Style.Literal);
        }

        private void FetchFolded()
        {
            this.FetchBlockScalar(Style.Folded);
        }

        private void FetchBlockScalar(Style style)
        {
            this.allowSimpleKey = true;

            this.RemovePossibleSimpleKey();

            this.tokens.Add(this.ScanBlockScalar(style));
        }

        private void FetchSingle()
        {
            this.FetchFlowScalar(Style.Single);
        }

        private void FetchDouble()
        {
            this.FetchFlowScalar(Style.Double);
        }

        private void FetchFlowScalar(Style style)
        {
            this.SavePossibleSimpleKey();

            this.allowSimpleKey = false;

            this.tokens.Add(this.ScanFlowScalar(style));
        }

        private void FetchPlain()
        {
            this.SavePossibleSimpleKey();

            this.allowSimpleKey = false;

            this.tokens.Add(this.ScanPlain());
        }

        private bool CheckDirective()
        {
            return this.Column == 0;
        }

        private bool CheckDocumentStart()
        {
            return this.Column == 0 &&
                   this.Prefix(3) == "---" &&
                   "\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(3)) != -1;
        }

        private bool CheckDocumentEnd()
        {
            return this.Column == 0 &&
                   this.Prefix(3) == "..." &&
                   "\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(3)) != -1;
        }

        private bool CheckBlockEntry()
        {
            return "\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(1)) != -1;
        }

        private bool CheckKey()
        {
            return this.flowLevel > 0 ||
                "\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(1)) != -1;
        }

        private bool CheckValue()
        {
            return this.flowLevel > 0 ||
                "\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(1)) != -1;
        }

        private bool CheckPlain()
        {
            var ch = this.Peek();
            return "\0 \t\r\n\x85\u2028\u2029-?:,[]{}#&*!|>\'\"%@`".IndexOf(ch) == -1 ||
                   ("\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(1)) == -1 &&
                    (ch == '-' || (this.flowLevel == 0 && "?:".IndexOf(ch) != -1))
                   );
        }

        private void ScanToNextToken()
        {
            if (this.Index == 0 && this.Peek() == '\uFEFF')
                this.Forward();

            var found = false;
            while (!found)
            {
                while (this.Peek() == ' ')
                    this.Forward();
                if (this.Peek() == '#')
                    while ("\0\r\n\x85\u2028\u2029".IndexOf(this.Peek()) == -1)
                        this.Forward();
                if (this.ScanLineBreak().Length != 0)
                {
                    if (this.flowLevel == 0)
                        this.allowSimpleKey = true;
                }
                else
                    found = true;
            }
        }

        private Tokens.Directive ScanDirective()
        {
            var start = this.Mark;
            this.Forward();
            var name = this.ScanDirectiveName(start);
            Tuple<string, string> value = null;
            Mark end;
            switch (name)
            {
                case "YAML":
                    value = this.ScanYamlDirectiveValue(start);
                    end = this.Mark;
                    break;
                case "TAG":
                    value = this.ScanTagDirectiveValue(start);
                    end = this.Mark;
                    break;
                default:
                    end = this.Mark;
                    while ("\0\r\n\x85\u2028\u2029".IndexOf(this.Peek()) == -1)
                        this.Forward();
                    break;
            }
            this.ScanDirectiveIgnoredLine(start);
            return new Tokens.Directive { Name = name, Value = value, StartMark = start, EndMark = end };
        }

        private static bool Between(char a, char b, char c)
        {
            return a <= b && b <= c;
        }

        private string ScanDirectiveName(Mark start)
        {
            var length = 0;
            var ch = this.Peek(length);
            while (Between('0', ch, '9') ||
                   Between('A', ch, 'Z') ||
                   Between('a', ch, 'z') ||
                   ch == '-' || ch == '_')
            {
                length++;
                ch = this.Peek(length);
            }

            if (length == 0)
                throw new Error("while scanning a directive", start,
                    "expected an alpha-numeric character, but found " + ch,
                    this.Mark);
            var value = this.Prefix(length);
            this.Forward(length);
            ch = this.Peek();
            if ("\0 \r\n\x85\u2028\u2029".IndexOf(ch) == -1)
                throw new Error("while scanning a directive", start,
                    "did not expect a non white-space character, but found " + ch,
                    this.Mark);
            return value;
        }

        private Tuple<string, string> ScanYamlDirectiveValue(Mark start)
        {
            while (this.Peek() == ' ')
                this.Forward();
            var major = this.ScanYamlDirectiveNumber(start);
            if (this.Peek() != '.')
                throw new Error("while scanning a directive", start,
                    "expected a digit or '.', but found" + this.Peek(),
                    this.Mark);
            this.Forward();
            var minor = this.ScanYamlDirectiveNumber(start);
            if ("\0 \r\n\x85\u2028\u2029".IndexOf(this.Peek()) == -1)
                throw new Error("while scanning a directive", start,
                    "expected a digit or ' ', but found" + this.Peek(),
                    this.Mark);
            return Tuple.Create(major.ToString(), minor.ToString());
        }

        private int ScanYamlDirectiveNumber(Mark start)
        {
            var ch = this.Peek();
            if (!Between('0', ch, '9'))
                throw new Error("while scanning a directive", start,
                    "expected a digit, but found" + this.Peek(),
                    this.Mark);
            var length = 0;
            while (Between('0', this.Peek(length), '9'))
                length++;
            var value = int.Parse(this.Prefix(length));
            this.Forward(length);
            return value;
        }

        private Tuple<string, string> ScanTagDirectiveValue(Mark start)
        {
            while (this.Peek() == ' ')
                this.Forward();
            var handle = this.ScanTagDirectiveHandle(start);
            while (this.Peek() == ' ')
                this.Forward();
            var prefix = this.ScanTagDirectivePrefix(start);
            return Tuple.Create(handle, prefix);
        }

        private string ScanTagDirectiveHandle(Mark start)
        {
            var value = this.ScanTagHandle("directive", start);
            var ch = this.Peek();
            if (ch != ' ')
                throw new Error("while scanning a directive", start,
                    "expected ' ', but found" + ch,
                    this.Mark);
            return value;
        }

        private string ScanTagDirectivePrefix(Mark start)
        {
            var value = this.ScanTagUri("directive", start);
            var ch = this.Peek();
            if ("\0 \r\n\x85\u2028\u2029".IndexOf(ch) == -1)
                throw new Error("while scanning a directive", start,
                    "expected ' ', but found" + ch,
                    this.Mark);
            return value;
        }

        private void ScanDirectiveIgnoredLine(Mark start)
        {
            while (this.Peek() == ' ')
                this.Forward();
            if (this.Peek() == '#')
                while ("\0\r\n\x85\u2028\u2029".IndexOf(this.Peek()) == -1)
                    this.Forward();
            var ch = this.Peek();
            if ("\0\r\n\x85\u2028\u2029".IndexOf(ch) == -1)
                throw new Error("while scanning a directive", start,
                    "expected comment or a line break, but found" + ch,
                    this.Mark);
            this.ScanLineBreak();
        }

        private Tokens.Base ScanAnchor<T>() where T : Tokens.SimpleValue, new()
        {
            var start = this.Mark;
            var name = this.Peek() == '*' ? "alias" : "anchor";
            this.Forward();
            var length = 0;
            var ch = this.Peek(length);
            while (Between('0', ch, '9') ||
                   Between('A', ch, 'Z') ||
                   Between('a', ch, 'z') ||
                   ch == '-' || ch == '_')
            {
                length++;
                ch = this.Peek(length);
            }

            if (length == 0)
                throw new Error("while scanning an " + name, start,
                    "expected an alpha-numeric character, but found " + ch,
                    this.Mark);
            var value = this.Prefix(length);
            this.Forward(length);
            ch = this.Peek();
            if ("\0 \t\r\n\x85\u2028\u2029?:,]}%@`".IndexOf(ch) == -1)
                throw new Error("while scanning an " + name, start,
                    "expected an alpha-numeric character, but found " + ch,
                    this.Mark);
            return new T { Value = value, StartMark = start, EndMark = this.Mark };
        }

        private Tokens.Tag ScanTag()
        {
            var start = this.Mark;
            var ch = this.Peek(1);
            string handle, suffix;
            if (ch == '<')
            {
                handle = null;
                this.Forward(2);
                suffix = this.ScanTagUri("tag", start);
                if (this.Peek() != '>')
                    throw new Error("while parsing a tag", start,
                        "expected '>', but found " + this.Peek(),
                        this.Mark);
                this.Forward();

            }
            else if ("\0 \t\r\n\x85\u2028\u2029".IndexOf(ch) != -1)
            {
                handle = null;
                suffix = "!";
                this.Forward();
            }
            else
            {
                var length = 1;
                var useHandle = false;
                while ("\0 \r\n\x85\u2028\u2029".IndexOf(ch) == -1)
                {
                    if (ch == '!')
                    {
                        useHandle = true;
                        break;
                    }
                    length++;
                    ch = this.Peek(length);
                }
                if (useHandle)
                    handle = this.ScanTagHandle("tag", start);
                else
                {
                    handle = "!";
                    this.Forward();
                }
                suffix = this.ScanTagUri("tag", start);
            }
            ch = this.Peek();
            if ("\0 \r\n\x85\u2028\u2029".IndexOf(ch) == -1)
                throw new Error("while parsing a tag", start,
                    "expected ' ', but found " + this.Peek(),
                    this.Mark);
            var value = Tuple.Create(handle, suffix);
            return new Tokens.Tag { Value = value, StartMark = start, EndMark = this.Mark };
        }

        private Tokens.Scalar ScanBlockScalar(Style style)
        {
            var folded = style == Style.Folded;

            var chunks = new List<string>();
            var start = this.Mark;

            this.Forward();
            bool? chomping;
            int? increment;
            this.ScanBlockScalarIndicators(start, out chomping, out increment);
            this.ScanBlockScalarIgnoredLine(start);

            var minIndent = this.indentLevel + 1;
            if (minIndent < 1)
                minIndent = 1;
            List<string> breaks;
            Mark end;
            int indent;
            if (!increment.HasValue)
            {
                int maxIndent;
                this.ScanBlockScalarIndentation(out breaks, out maxIndent, out end);
                indent = Math.Max(minIndent, maxIndent);
            }
            else
            {
                indent = minIndent + increment.Value - 1;
                this.ScanBlockScalarBreaks(indent, out breaks, out end);
            }
            var lineBreak = string.Empty;

            while (this.Column == indent && this.Peek() != '\0')
            {
                chunks.AddRange(breaks);
                var leadingNonSpace = " \t".IndexOf(this.Peek()) == -1;
                var length = 0;
                while ("\0\r\n\x85\u2028\u2029".IndexOf(this.Peek(length)) == -1)
                    length++;
                chunks.Add(this.Prefix(length));
                this.Forward(length);
                lineBreak = this.ScanLineBreak();
                this.ScanBlockScalarBreaks(indent, out breaks, out end);
                if (this.Column == indent && this.Peek() != '\0')
                {
                    if (folded && lineBreak == "\n" && leadingNonSpace
                        && " \t".IndexOf(this.Peek()) == -1)
                    {
                        if (breaks.Count == 0)
                            chunks.Add(" ");
                    }
                    else
                        chunks.Add(lineBreak);
                }
                else
                    break;
            }

            if (!chomping.HasValue || chomping.Value)
                chunks.Add(lineBreak);
            if (chomping.HasValue && chomping.Value)
                chunks.AddRange(breaks);

            return new Tokens.Scalar { Value = string.Join("", chunks) };
        }

        private void ScanBlockScalarIndicators(Mark start, out bool? chomping, out int? increment)
        {
            chomping = null;
            increment = null;
            var ch = this.Peek();
            if (ch == '+' || ch == '-')
            {
                chomping = ch == '+';
                this.Forward();
                ch = this.Peek();
                if ("0123456789".IndexOf(ch) != -1)
                {
                    increment = ch - '0';
                    if (increment == 0)
                        if ("\0 \r\n\x85\u2028\u2029".IndexOf(ch) == -1)
                            throw new Error("while scanning a block scalar", start,
                                "expected an indentation idicator in the range 1-9, but found 0",
                                this.Mark);
                    this.Forward();
                }
            }
            else if ("0123456789".IndexOf(ch) != -1)
            {
                increment = ch - '0';
                if (increment == 0)
                    if ("\0 \r\n\x85\u2028\u2029".IndexOf(ch) == -1)
                        throw new Error("while scanning a block scalar", start,
                            "expected an indentation idicator in the range 1-9, but found 0",
                            this.Mark);
                this.Forward();
                ch = this.Peek();
                switch (ch)
                {
                    case '+':
                        chomping = true;
                        break;
                    case '-':
                        chomping = false;
                        break;
                }
            }
            ch = this.Peek();
            if ("\0 \r\n\x85\u2028\u2029".IndexOf(ch) == -1)
                throw new Error("while scanning a block scalar", start,
                            "expected chomping or indentation indicators, but found 0",
                            this.Mark);
        }

        private void ScanBlockScalarIgnoredLine(Mark start)
        {
            while (this.Peek() == ' ')
                this.Forward();
            if (this.Peek() == '#')
                while ("\0\r\n\x85\u2028\u2029".IndexOf(this.Peek()) == -1)
                    this.Forward();
            var ch = this.Peek();
            if ("\0\r\n\x85\u2028\u2029".IndexOf(ch) == -1)
                throw new Error("while scanning a block scalar", start,
                            "expected a comment or line break, but found 0",
                            this.Mark);
            this.ScanLineBreak();
        }

        private void ScanBlockScalarIndentation(out List<string> chunks, out int maxIndent, out Mark end)
        {
            chunks = new List<string>();
            maxIndent = 0;
            end = this.Mark;
            char ch;
            while (" \r\n\x85\u2028\u2029".IndexOf(ch = this.Peek()) != -1)
            {
                if (ch != ' ')
                {
                    chunks.Add(this.ScanLineBreak());
                    end = this.Mark;
                }
                else
                {
                    this.Forward();
                    if (this.Column > maxIndent)
                        maxIndent = this.Column;
                }
            }
        }

        private void ScanBlockScalarBreaks(int indent, out List<string> chunks, out Mark end)
        {
            chunks = new List<string>();
            end = this.Mark;
            while (this.Column < indent && this.Peek() == ' ')
                this.Forward();
            while ("\r\n\x85\u2028\u2029".IndexOf(this.Peek()) != -1)
            {
                chunks.Add(this.ScanLineBreak());
                end = this.Mark;
                while (this.Column < indent && this.Peek() == ' ')
                    this.Forward();
            }
        }

        private Tokens.Scalar ScanFlowScalar(Style style)
        {
            var isDouble = style == Style.Double;
            var chunks = new List<string>();
            var start = this.Mark;
            var quote = this.Peek();
            this.Forward();
            chunks.AddRange(this.ScanFlowScalarNonSpaces(isDouble, start));
            while (this.Peek() != quote)
            {
                chunks.AddRange(this.ScanFlowScalarSpaces(start));
                chunks.AddRange(this.ScanFlowScalarNonSpaces(isDouble, start));
            }
            this.Forward();
            var end = this.Mark;
            return new Tokens.Scalar { Value = string.Join("", chunks), IsPlain = false, StartMark = start, EndMark = end };
        }

        private static readonly Dictionary<char, char> escapeReplacements =
            new Dictionary<char, char>
            {
                {'0', '\0'},
                {'a', '\x7'},
                {'b', '\x8'},
                {'t', '\x9'},
                {'\t', '\x9'},
                {'n', '\xA'},
                {'v', '\xB'},
                {'f', '\xC'},
                {'r', '\xD'},
                {'e', '\x1B'},
                {' ', '\x20'},
                {'"', '"'},
                {'\\', '\\'},
                {'N', '\x85'},
                {'_', '\xA0'},
                {'L', '\u2028'},
                {'P', '\u2029'}
            };

        private readonly Dictionary<char, int> escapeCodes =
            new Dictionary<char, int>
            {
                {'x', 2},
                {'u', 4},
                {'U', 8}                            
            };

        private IEnumerable<string> ScanFlowScalarNonSpaces(bool isDouble, Mark start)
        {
            var chunks = new List<string>();
            for (; ; )
            {
                var length = 0;
                while ("'\"\\\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(length)) == -1)
                    length++;
                if (length > 0)
                {
                    chunks.Add(this.Prefix(length));
                    this.Forward(length);
                }
                var ch = this.Peek();
                if (!isDouble && ch == '\'' && this.Peek(1) == '\'')
                {
                    chunks.Add("'");
                    this.Forward(2);
                }
                else if ((isDouble && ch == '\'') || (!isDouble && (ch == '"' || ch == '\\')))
                {
                    chunks.Add(ch.ToString());
                    this.Forward();
                }
                else if (isDouble && ch == '\\')
                {
                    this.Forward();
                    ch = this.Peek();
                    char e;
                    if (escapeReplacements.TryGetValue(ch, out e))
                    {
                        chunks.Add(e.ToString());
                        this.Forward();
                    }
                    else if (escapeCodes.TryGetValue(ch, out length))
                    {
                        this.Forward();
                        for (var k = 0; k < length; k++)
                            if ("0123456789ABCDEFabcdef".IndexOf(this.Peek(k)) == -1)
                                throw new Error("while scanning a double-quoted scalar", start,
                                            string.Format("expected an escape sequence of {0} hex numbers, but found {1}", length, this.Peek(k)),
                                            this.Mark);
                        var code = int.Parse(this.Prefix(length), NumberStyles.AllowHexSpecifier);
                        chunks.Add(code < char.MaxValue
                                       ? ((char) code).ToString()
                                       : Encoding.UTF32.GetString(BitConverter.GetBytes(code)));
                        // chunks.Add(int.Parse(this.Prefix(length), NumberStyles.AllowHexSpecifier).ToString());
                        this.Forward(length);
                    }
                    else if ("\r\n\x85\u2028\u2029".IndexOf(ch) != -1)
                    {
                        this.ScanLineBreak();
                        chunks.AddRange(this.ScanFlowScalarBreaks(start));
                    }
                    else
                        throw new Error("while scanning a quoted scalar", start,
                                            "found unknown escape character " + ch,
                                            this.Mark);
                }
                else
                    return chunks;
            }
        }

        private IEnumerable<string> ScanFlowScalarSpaces(Mark start)
        {
            var chunks = new List<string>();
            var length = 0;
            while (" \t".IndexOf(this.Peek(length)) != -1)
                length++;
            var whitespaces = this.Prefix(length);
            this.Forward(length);
            var ch = this.Peek();
            switch (ch)
            {
                case '\0':
                    throw new Error("while scanning a quoted scalar", start,
                                    "found unexpected end of stream",
                                    this.Mark);
                case '\r':
                case '\n':
                case '\x85':
                case '\u2028':
                case '\u2029':
                    var lineBreak = this.ScanLineBreak();
                    var breaks = this.ScanFlowScalarBreaks(start);
                    if (lineBreak != "\n")
                        chunks.Add(lineBreak);
                    else if (breaks.Count == 0)
                        chunks.Add(" ");
                    chunks.AddRange(breaks);
                    break;
                default:
                    chunks.Add(whitespaces);
                    break;
            }
            return chunks;
        }

        private IList<string> ScanFlowScalarBreaks(Mark start)
        {
            var chunks = new List<string>();
            for (; ; )
            {
                var prefix = this.Prefix(3);
                if ((prefix == "---" || prefix == "...")
                    && "\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(3)) != -1)
                    throw new Error("while scanning a quoted scalar", start,
                                    "found unexpected document seperator",
                                    this.Mark);
                while (" \t".IndexOf(this.Peek()) != -1)
                    this.Forward();
                if ("\r\n\x85\u2028\u2029".IndexOf(this.Peek()) != -1)
                    chunks.Add(this.ScanLineBreak());
                else
                    return chunks;
            }
        }

        private Tokens.Scalar ScanPlain()
        {
            var chunks = new List<string>();
            var start = this.Mark;
            var end = start;
            var indent = this.indentLevel + 1;
            var spaces = new List<string>();

            for (; ; )
            {
                var length = 0;
                if (this.Peek() == '#')
                    break;
                char ch;
                for (; ; )
                {
                    ch = this.Peek(length);
                    if ("\0 \t\r\n\x85\u2028\u2029".IndexOf(ch) != -1
                        || (this.flowLevel == 0 && ch == ':' &&
                            "\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(length + 1)) != -1)
                        || (this.flowLevel > 0 && ",:?[]{}".IndexOf(ch) != -1))
                        break;
                    length++;
                }

                if (this.flowLevel > 0 && ch == ':' &&
                    "\0 \t\r\n\x85\u2028\u2029,[]{}".IndexOf(this.Peek(length + 1)) == -1)
                {
                    this.Forward(length);
                    throw new Error("while scanning a plain scalar", start,
                                    "found unexpected ':'",
                                    this.Mark);
                }
                if (length == 0)
                    break;
                this.allowSimpleKey = false;
                chunks.AddRange(spaces);
                chunks.Add(this.Prefix(length));
                this.Forward(length);
                end = this.Mark;
                spaces = this.ScanPlainSpaces();
                if (spaces.Count == 0 || this.Peek() == '#'
                    || (this.flowLevel == 0 && this.Column < indent))
                    break;
            }
            return new Tokens.Scalar { Value = string.Join("", chunks), IsPlain = true, StartMark = start, EndMark = end };
        }

        private List<string> ScanPlainSpaces()
        {
            var chunks = new List<string>();
            var length = 0;
            while (this.Peek(length) == ' ')
                length++;
            var whitespaces = this.Prefix(length);
            this.Forward(length);
            var ch = this.Peek();

            if ("\r\n\x85\u2028\u2029".IndexOf(ch) != -1)
            {
                var lineBreak = this.ScanLineBreak();
                this.allowSimpleKey = true;
                var prefix = this.Prefix(3);
                if ((prefix == "---" || prefix == "...")
                    && "\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(3)) != -1)
                    return new List<string>();
                var breaks = new List<string>();
                while (" \r\n\x85\u2028\u2029".IndexOf(this.Peek()) != -1)
                {
                    if (this.Peek() == ' ')
                        this.Forward();
                    else
                    {
                        breaks.Add(this.ScanLineBreak());
                        prefix = this.Prefix(3);
                        if ((prefix == "---" || prefix == "...")
                            && "\0 \t\r\n\x85\u2028\u2029".IndexOf(this.Peek(3)) != -1)
                            return new List<string>();
                    }
                }
                if (lineBreak != "\n")
                    chunks.Add(lineBreak);
                else if (breaks.Count == 0)
                    chunks.Add(" ");
                chunks.AddRange(breaks);
            }
            else if (whitespaces.Length != 0)
                chunks.Add(whitespaces);

            return chunks;
        }

        private string ScanTagHandle(string name, Mark start)
        {
            var ch = this.Peek();
            if (ch != '!')
                throw new Error("while scanning a " + name, start,
                                "expected '!', but found " + ch,
                                this.Mark);
            var length = 1;
            ch = this.Peek(length);
            if (ch != ' ')
            {
                while (Between('0', ch, '9') || Between('A', ch, 'Z')
                    || Between('a', ch, 'z') || ch == '-' || ch == '_')
                    ch = this.Peek(++length);
                if (ch != '!')
                {
                    this.Forward(length);
                    throw new Error("while scanning a " + name, start,
                                    "expected '!', but found " + ch,
                                    this.Mark);
                }
                length++;
            }
            var value = this.Prefix(length);
            this.Forward(length);
            return value;
        }

        private string ScanTagUri(string name, Mark start)
        {
            var chunks = new List<string>();

            var length = 0;
            var ch = this.Peek(length);
            while (Between('0', ch, '9') || Between('A', ch, 'Z') || Between('a', ch, 'z')
                || "-;/?:@&=+$,_.!~*\'()[]%".IndexOf(ch) != -1)
            {
                if (ch == '%')
                {
                    chunks.Add(this.Prefix(length));
                    this.Forward(length);
                    length = 0;
                    chunks.Add(this.ScanUriEscapes(name, start));
                }
                else
                    length++;
                ch = this.Peek(length);
            }
            if (length != 0)
            {
                chunks.Add(this.Prefix(length));
                this.Forward(length);
            }
            if (chunks.Count == 0)
                throw new Error("while scanning a " + name, start,
                                    "expected URI, but found " + ch,
                                    this.Mark);
            return string.Join("", chunks);
        }

        private string ScanUriEscapes(string name, Mark start)
        {
            var codes = new List<byte>();
            var mark = this.Mark;
            while (this.Peek() == '%')
            {
                this.Forward();
                for (var k = 0; k < 2; k++)
                    if ("0123456789ABCDEFabcdef".IndexOf(this.Peek(k)) == -1)
                        throw new Error("while scanning a " + name, start,
                                    "expected URI escape of 2 hex characters, but found " + this.Peek(k),
                                    mark);
                codes.Add(byte.Parse(this.Prefix(2), NumberStyles.AllowHexSpecifier));
                this.Forward(2);
            }
            string value;
            try
            {
                value = Encoding.UTF8.GetString(codes.ToArray());
            }
            catch (Exception e)
            {
                throw new Error("while scanning a " + name, start,
                                    e.Message, this.Mark);
            }
            return value;
        }

        private string ScanLineBreak()
        {
            var ch = this.Peek();
            if ("\r\n\x85".IndexOf(ch) != -1)
            {
                if (this.Prefix(2) == "\r\n")
                    this.Forward(2);
                else
                    this.Forward();
                return "\n";
            }
            if ("\u2028\u2029".IndexOf(ch) != -1)
            {
                this.Forward();
                return ch.ToString();
            }
            return string.Empty;
        }
    }
}
