namespace Nyaml.Canonical
{
    using System.Collections.Generic;

    public class Parser : IParser
    {
        private readonly Queue<Events.Base> events = new Queue<Events.Base>();
        private bool isParsed;

        private readonly IScanner scanner;

        public Parser(IScanner scanner)
        {
            this.scanner = scanner;
        }

        private T GetToken<T>() where T : Tokens.Base
        {
            return this.scanner.GetToken<T>();
        }

        private string GetTokenValue()
        {
            return this.GetToken<Tokens.SimpleValue>().Value;
        }

        private bool CheckToken<T>() where T : Tokens.Base
        {
            return this.scanner.CheckToken<T>();
        }

        private bool CheckToken<T1, T2>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
        {
            return this.scanner.CheckToken<T1, T2>();
        }

        private Tokens.Base PeekToken()
        {
            return this.scanner.PeekToken();
        }

        private void ParseStream()
        {
            this.GetToken<Tokens.StreamStart>();
            this.events.Enqueue(new Events.StreamStart());
            while (!this.CheckToken<Tokens.StreamEnd>())
                if (this.CheckToken<Tokens.Directive, Tokens.DocumentStart>())
                    this.ParseDocument();
                else
                    throw new Error("document is expected, but found " + this.PeekToken());
            this.GetToken<Tokens.StreamEnd>();
            this.events.Enqueue(new Events.StreamEnd());
        }

        private void ParseDocument()
        {
            if (this.CheckToken<Tokens.Directive>())
                this.GetToken<Tokens.Directive>();
            this.GetToken<Tokens.DocumentStart>();
            this.events.Enqueue(new Events.DocumentStart());
            this.ParseNode();
            this.events.Enqueue(new Events.DocumentEnd());
        }

        private void ParseNode()
        {
            if (this.CheckToken<Tokens.Alias>())
            {
                this.events.Enqueue(new Events.Alias { Anchor = this.GetTokenValue() });
                return;
            }
            var anchor = this.CheckToken<Tokens.Anchor>()
                             ? this.GetTokenValue()
                             : null;
            var tagT = this.CheckToken<Tokens.Tag>()
                             ? this.GetToken<Tokens.Tag>().Value
                             : null;
            var tag = tagT != null ? tagT.Item1 + tagT.Item2 : null;
            
            if (this.CheckToken<Tokens.Scalar>())
                this.events.Enqueue(
                    new Events.Scalar
                    {
                        Anchor = anchor, 
                        Tag = tag,
                        Value = this.GetTokenValue()
                    });
            else if (this.CheckToken<Tokens.FlowSequenceStart>())
            {
                this.events.Enqueue(new Events.SequenceStart { Anchor = anchor, Tag = tag });
                this.ParseSequence();
            }
            else if (this.CheckToken<Tokens.FlowMappingStart>())
            {
                this.events.Enqueue(new Events.MappingStart { Anchor = anchor, Tag = tag });
                this.ParseMapping();
            } 
            else
                throw new Error("SCALAR, '[', or '{' is expected, found " + this.PeekToken());
        }

        private void ParseSequence()
        {
            this.GetToken<Tokens.FlowSequenceStart>();
            if (!this.CheckToken<Tokens.FlowSequenceEnd>())
            {
                this.ParseNode();
                while (!this.CheckToken<Tokens.FlowSequenceEnd>())
                {
                    this.GetToken<Tokens.FlowEntry>();
                    if (!this.CheckToken<Tokens.FlowSequenceEnd>())
                        this.ParseNode();
                }
            }
            this.GetToken<Tokens.FlowSequenceEnd>();
            this.events.Enqueue(new Events.SequenceEnd());
        }

        private void ParseMapping()
        {
            this.GetToken<Tokens.FlowMappingStart>();
            if (!this.CheckToken<Tokens.FlowMappingEnd>())
            {
                this.ParseMapEntry();
                while (!this.CheckToken<Tokens.FlowMappingEnd>())
                {
                    this.GetToken<Tokens.FlowEntry>();
                    if (!this.CheckToken<Tokens.FlowMappingEnd>())
                        this.ParseMapEntry();
                }
            }
            this.GetToken<Tokens.FlowMappingEnd>();
            this.events.Enqueue(new Events.MappingEnd());
        }

        private void ParseMapEntry()
        {
            this.GetToken<Tokens.Key>();
            this.ParseNode();
            this.GetToken<Tokens.Value>();
            this.ParseNode();
        }

        private void Parse()
        {
            this.ParseStream();
            this.isParsed = true;
        }

        public void Reset() { }

        public bool CheckEvent()
        {
            if (!this.isParsed)
                this.Parse();
            return this.events.Count != 0;
        }

        public bool CheckEvent<T>() where T : Events.Base
        {
            return this.PeekEvent() is T;
        }

        public bool CheckEvent<T1, T2, T3, T4>()
            where T1 : Events.Base
            where T2 : Events.Base
            where T3 : Events.Base
            where T4 : Events.Base
        {
            var e = this.PeekEvent();
            return e is T1
                   || e is T2
                   || e is T3
                   || e is T4;
        }

        public Events.Base PeekEvent()
        {
            if (!this.isParsed)
                this.Parse();
            return this.events.Peek();
        }

        public Events.Base GetEvent()
        {
            if (!this.isParsed)
                this.Parse();
            return this.events.Dequeue();
        }
    }
}
