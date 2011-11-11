namespace Nyaml
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public interface IParser
    {
        void Reset();

        bool CheckEvent();

        bool CheckEvent<T>() where T : Events.Base;

        bool CheckEvent<T1, T2, T3, T4>()
            where T1 : Events.Base
            where T2 : Events.Base
            where T3 : Events.Base
            where T4 : Events.Base;

        Events.Base PeekEvent();

        Events.Base GetEvent();
    }

    public class Parser : IParser
    {
        [Serializable]
        public class Error : MarkedYamlError
        {
            public Error(string context = null, Mark contextMark = null,
            string problem = null, Mark problemMark = null, string note = null)
                : base(context, contextMark, problem, problemMark, note)
            { }
        }

        private static readonly Dictionary<string, string> defaultTags =
            new Dictionary<string, string>
            {
                { "!", "!" },
                { "!!", "tag:yaml.org,2002:" }
            };

        private Events.Base currentEvent;
        private Tuple<string, string> yamlVersion;
        private Dictionary<string, string> tagHandles = new Dictionary<string, string>();
        private readonly Stack<Func<Events.Base>> states = new Stack<Func<Events.Base>>();
        private readonly Stack<Mark> marks = new Stack<Mark>();
        private Func<Events.Base> state;

        private readonly IScanner scanner;

        public Parser(IScanner scanner)
        {
            this.scanner = scanner;
            this.state = this.ParseStreamStart;
        }

        public void Reset()
        {
            this.states.Clear();
            this.state = null;
        }

        public bool CheckEvent()
        {
            this.PeekEvent();
            return this.currentEvent != null;
        }

        public bool CheckEvent<T>() where T : Events.Base
        {
            return this.CheckEvent() && this.currentEvent is T;
        }

        public bool CheckEvent<T1, T2, T3, T4>()
            where T1 : Events.Base
            where T2 : Events.Base
            where T3 : Events.Base
            where T4 : Events.Base
        {
            return this.CheckEvent() && 
                (  this.currentEvent is T1
                || this.currentEvent is T2
                || this.currentEvent is T3
                || this.currentEvent is T4
                ) ;
        }

        public Events.Base PeekEvent()
        {
            if (this.currentEvent == null && this.state != null)
                this.currentEvent = this.state();
            return this.currentEvent;
        }

        public Events.Base GetEvent()
        {
            var value = this.PeekEvent();
            this.currentEvent = null;
            return value;
        }

        private Tokens.Base GetToken()
        {
            return this.scanner.GetToken();
        }

        private T GetToken<T>() where T : Tokens.Base
        {
            return this.scanner.GetToken<T>();
        }

        private Tokens.Base PeekToken()
        {
            return this.scanner.PeekToken();
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

        private bool CheckToken<T1, T2, T3>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
        {
            return this.scanner.CheckToken<T1, T2, T3>();
        }

        private bool CheckToken<T1, T2, T3, T4>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
            where T4 : Tokens.Base
        {
            return this.scanner.CheckToken<T1, T2, T3, T4>();
        }

        private Events.StreamStart ParseStreamStart()
        {
            var token = this.GetToken<Tokens.StreamStart>();
            var value = new Events.StreamStart
                        {
                            StartMark = token.StartMark,
                            EndMark = token.EndMark,
                            Encoding = token.Encoding
                        };

            this.state = this.ParseImplicitDocumentStart;

            return value;
        }

        private Events.Base ParseImplicitDocumentStart()
        {
            if (!this.CheckToken<Tokens.Directive, Tokens.DocumentStart, Tokens.StreamEnd>())
            {
                this.tagHandles = new Dictionary<string, string>(defaultTags);
                var token = this.PeekToken();
                var start = token.StartMark;
                var end = start;
                var value = new Events.DocumentStart
                            {
                                StartMark = start,
                                EndMark = end,
                                IsExplicit = false
                            };

                this.states.Push(this.ParseDocumentEnd);
                this.state = this.ParseBlockNode;

                return value;
            }
            return this.ParseDocumentStart();
        }

        private Events.Base ParseDocumentStart()
        {
            while (this.CheckToken<Tokens.DocumentEnd>())
                this.GetToken();

            Tokens.Base token;
            Events.Base value;
            if (!this.CheckToken<Tokens.StreamEnd>())
            {
                token = this.PeekToken();
                var start = token.StartMark;
                Tuple<string, string> version;
                IDictionary<string, string> tags;
                this.ProcessDirectives(out version, out tags);
                if (!this.CheckToken<Tokens.DocumentStart>())
                    throw new Error(null, null,
                        "expected '<document start>', but found " + this.PeekToken().Id,
                        this.PeekToken().StartMark);
                token = this.GetToken();
                var end = token.EndMark;
                value = new Events.DocumentStart
                            {
                                StartMark = start,
                                EndMark = end,
                                IsExplicit = true,
                                Version = version,
                                Tags = tags
                            };
                this.states.Push(this.ParseDocumentEnd);
                this.state = this.ParseDocumentContent;
                return value;
            }
            token = this.GetToken();
            value = new Events.StreamEnd { StartMark = token.StartMark, EndMark = token.EndMark };
            Debug.Assert(this.states.Count == 0 && this.marks.Count == 0);
            this.state = null;
            return value;
        }

        private Events.DocumentEnd ParseDocumentEnd()
        {
            var token = this.PeekToken();
            var start = token.StartMark;
            var end = start;
            var isExplicit = false;
            token = this.GetToken<Tokens.DocumentEnd>();
            if (token != null)
            {
                end = token.EndMark;
                isExplicit = true;
            }

            var value = new Events.DocumentEnd { StartMark = start, EndMark = end, IsExplicit = isExplicit };

            this.state = this.ParseDocumentStart;

            return value;
        }

        private Events.Base ParseDocumentContent()
        {
            if (this.CheckToken<Tokens.Directive, Tokens.DocumentStart,
                Tokens.DocumentEnd, Tokens.StreamEnd>())
            {
                var value = this.ProcessEmptyScalar(this.PeekToken().StartMark);
                this.state = this.states.Pop();
                return value;
            }
            return this.ParseBlockNode();
        }

        private void ProcessDirectives(out Tuple<string, string> version, out IDictionary<string, string> tags)
        {
            this.yamlVersion = null;
            this.tagHandles.Clear();
            Tokens.Directive token;
            while ((token = this.GetToken<Tokens.Directive>()) != null)
            {
                switch (token.Name)
                {
                    case "YAML":
                        if (this.yamlVersion != null)
                            throw new Error(null, null,
                                "found duplicate YAML directive", token.StartMark);
                        if (token.Value.Item1 != "1")
                            throw new Error(null, null,
                                "found incompatible YAML document (version 1.* is required)", token.StartMark);
                        this.yamlVersion = token.Value;
                        break;
                    case "TAG":
                        if (this.tagHandles.ContainsKey(token.Value.Item1))
                            throw new Error(null, null,
                                "duplicate tag handle " + token.Value.Item1, token.StartMark);
                        this.tagHandles[token.Value.Item1] = token.Value.Item2;
                        break;
                }
            }
            version = this.yamlVersion;
            tags = this.tagHandles.Count > 0 ? new Dictionary<string, string>(this.tagHandles) : null;
            foreach (var key in defaultTags.Keys.Where(key => !this.tagHandles.ContainsKey(key)))
                this.tagHandles[key] = defaultTags[key];
        }

        private Events.Node ParseBlockNode()
        {
            return this.ParseNode(true);
        }

        private Events.Node ParseFlowNode()
        {
            return this.ParseNode();
        }

        private Events.Node ParseBlockNodeOrIndentlessSequence()
        {
            return this.ParseNode(true, true);
        }

        private Events.Node ParseNode(bool isBlock = false, bool isIndentlessSequence = false)
        {
            Events.Node value;
            if (this.CheckToken<Tokens.Alias>())
            {
                var token = this.GetToken<Tokens.Alias>();
                value = new Events.Alias { Anchor = token.Value, StartMark = token.StartMark, EndMark = token.EndMark };
                this.state = this.states.Pop();
            }
            else
            {
                string anchor = null;
                Tuple<string, string> tag = null;
                Mark start = null, end = null, tagMark = null;
                if (this.CheckToken<Tokens.Anchor>())
                {
                    var token = this.GetToken<Tokens.Anchor>();
                    start = token.StartMark;
                    end = token.EndMark;
                    anchor = token.Value;
                    var tagToken = this.GetToken<Tokens.Tag>();
                    if (tagToken != null)
                    {
                        tagMark = tagToken.StartMark;
                        end = tagToken.EndMark;
                        tag = tagToken.Value;
                    }
                }
                else if (this.CheckToken<Tokens.Tag>())
                {
                    var token = this.GetToken<Tokens.Tag>();
                    start = tagMark = token.StartMark;
                    end = token.EndMark;
                    tag = token.Value;
                    var anchorToken = this.GetToken<Tokens.Anchor>();
                    if (anchorToken != null)
                    {
                        end = anchorToken.EndMark;
                        anchor = anchorToken.Value;
                    }
                }

                string fullTag = null;
                if (tag != null)
                {
                    var handle = tag.Item1;
                    var suffix = tag.Item2;
                    if (handle != null)
                    {
                        if (!this.tagHandles.ContainsKey(handle))
                            throw new Error("while parsing a node", start,
                                "found undefined tag handle " + handle, tagMark);
                        fullTag = this.tagHandles[handle] + suffix;
                    }
                    else
                        fullTag = suffix;
                }

                if (start == null)
                    start = end = this.PeekToken().StartMark;
                var isImplicit = fullTag == null || fullTag == "!";
                if (isIndentlessSequence && this.CheckToken<Tokens.BlockEntry>())
                {
                    end = this.PeekToken().EndMark;
                    value = new Events.SequenceStart
                            {
                                Anchor = anchor,
                                Tag = fullTag,
                                IsImplicit = isImplicit,
                                StartMark = start, EndMark = end
                            };
                    this.state = this.ParseIndentlessSequenceEntry;
                }
                else
                {
                    if (this.CheckToken<Tokens.Scalar>())
                    {
                        var token = this.GetToken<Tokens.Scalar>();
                        end = token.EndMark;
                        ScalarImplicitLevel implcitLevel;
                        if ((token.IsPlain && fullTag == null) || fullTag == "!")
                            implcitLevel = ScalarImplicitLevel.Plain;
                        else if (fullTag == null)
                            implcitLevel = ScalarImplicitLevel.NonPlain;
                        else
                            implcitLevel = ScalarImplicitLevel.None;
                        value = new Events.Scalar
                                {
                                    Anchor = anchor,
                                    Tag = fullTag,
                                    ImplicitLevel = implcitLevel,
                                    Value = token.Value,
                                    StartMark = start,
                                    EndMark = end,
                                    Style = token.Style
                                };
                        this.state = this.states.Pop();
                    }
                    else if (this.CheckToken<Tokens.FlowSequenceStart>())
                    {
                        end = this.PeekToken().EndMark;
                        value = new Events.SequenceStart
                        {
                            Anchor = anchor,
                            Tag = fullTag,
                            IsImplicit = isImplicit,
                            StartMark = start,
                            EndMark = end,
                            FlowStyle = FlowStyle.Flow
                        };
                        this.state = this.ParseFlowSequenceFirstEntry;
                    }
                    else if (this.CheckToken<Tokens.FlowMappingStart>())
                    {
                        end = this.PeekToken().EndMark;
                        value = new Events.MappingStart
                        {
                            Anchor = anchor,
                            Tag = fullTag,
                            IsImplicit = isImplicit,
                            StartMark = start,
                            EndMark = end,
                            FlowStyle = FlowStyle.Flow
                        };
                        this.state = this.ParseFlowMappingFirstKey;
                    }
                    else if (isBlock && this.CheckToken<Tokens.BlockSequenceStart>())
                    {
                        end = this.PeekToken().StartMark;
                        value = new Events.SequenceStart
                        {
                            Anchor = anchor,
                            Tag = fullTag,
                            IsImplicit = isImplicit,
                            StartMark = start,
                            EndMark = end,
                            FlowStyle = FlowStyle.Block
                        };
                        this.state = this.ParseBlockSequenceFirstEntry;
                    }
                    else if (isBlock && this.CheckToken<Tokens.BlockMappingStart>())
                    {
                        end = this.PeekToken().StartMark;
                        value = new Events.MappingStart
                        {
                            Anchor = anchor,
                            Tag = fullTag,
                            IsImplicit = isImplicit,
                            StartMark = start,
                            EndMark = end,
                            FlowStyle = FlowStyle.Block
                        };
                        this.state = this.ParseBlockMappingFirstKey;
                    }
                    else if (anchor != null || tag != null)
                    {
                        value = new Events.Scalar
                                {
                                    Anchor = anchor,
                                    Tag = fullTag,
                                    ImplicitLevel = isImplicit ? ScalarImplicitLevel.Plain : ScalarImplicitLevel.None,
                                    Value = string.Empty,
                                    StartMark = start,
                                    EndMark = end
                                };
                        this.state = this.states.Pop();
                    }
                    else
                    {
                        var node = isBlock ? "block" : "flow";
                        var token = this.PeekToken();
                        throw new Error(string.Format("while parsing a {0} node", node),
                            start, "expected to find node content, but found " + token.Id,
                            token.StartMark);
                    }
                }
            }
            return value;
        }

        private Events.Base ParseBlockSequenceFirstEntry()
        {
            var token = this.GetToken();
            this.marks.Push(token.StartMark);
            return this.ParseBlockSequenceEntry();
        }

        private Events.Base ParseBlockSequenceEntry()
        {
            Tokens.Base token;
            if (this.CheckToken<Tokens.BlockEntry>())
            {
                token = this.GetToken();
                if (!this.CheckToken<Tokens.BlockEntry, Tokens.BlockEnd>())
                {
                    this.states.Push(this.ParseBlockSequenceEntry);
                    return this.ParseBlockNode();
                }
                this.state = this.ParseBlockSequenceEntry;
                return this.ProcessEmptyScalar(token.EndMark);
            }
            if (!this.CheckToken<Tokens.BlockEnd>())
            {
                token = this.GetToken();
                throw new Error("while parsing a block collection", this.marks.Peek(),
                    "expected <block end>, but found " + token.Id, token.StartMark);
            }
            token = this.GetToken();
            var value = new Events.SequenceEnd
                        {
                            StartMark = token.StartMark,
                            EndMark = token.EndMark
                        };
            this.state = this.states.Pop();
            this.marks.Pop();
            return value;
        }

        private Events.Base ParseIndentlessSequenceEntry()
        {
            Tokens.Base token = this.GetToken<Tokens.BlockEntry>();
            if (token != null)
            {
                if (!this.CheckToken<Tokens.BlockEntry, Tokens.Key,
                    Tokens.Value, Tokens.BlockEnd>())
                {
                    this.states.Push(this.ParseIndentlessSequenceEntry);
                    return this.ParseBlockNode();
                }
                this.state = this.ParseIndentlessSequenceEntry;
                return this.ProcessEmptyScalar(token.EndMark);
            }
            token = this.PeekToken();
            var value = new Events.SequenceEnd
                        {
                            StartMark = token.StartMark,
                            EndMark = token.StartMark
                        };
            this.state = this.states.Pop();
            return value;
        }

        private Events.Base ParseBlockMappingFirstKey()
        {
            var token = this.GetToken();
            this.marks.Push(token.StartMark);
            return this.ParseBlockMappingKey();
        }

        private Events.Base ParseBlockMappingKey()
        {
            var key = this.GetToken<Tokens.Key>();
            if (key != null)
            {
                if (!this.CheckToken<Tokens.Key, Tokens.Value, Tokens.BlockEnd>())
                {
                    this.states.Push(this.ParseBlockMappingValue);
                    return this.ParseBlockNodeOrIndentlessSequence();
                }
                this.state = this.ParseBlockMappingValue;
                return ProcessEmptyScalar(key.EndMark);
            }
            if (!this.CheckToken<Tokens.BlockEnd>())
            {
                var bad = this.PeekToken();
                throw new Error("while parsing a block mapping", this.marks.Peek(),
                    "expected <block end>, but found " + bad.Id, bad.StartMark);
            }
            var token = this.GetToken();
            var value = new Events.MappingEnd
                        {
                            StartMark = token.StartMark,
                            EndMark = token.EndMark
                        };
            this.state = this.states.Pop();
            this.marks.Pop();
            return value;
        }

        private Events.Base ParseBlockMappingValue()
        {
            var value = this.GetToken<Tokens.Value>();
            if (value != null)
            {
                if (!this.CheckToken<Tokens.Key, Tokens.Value, Tokens.BlockEnd>())
                {
                    this.states.Push(this.ParseBlockMappingKey);
                    return this.ParseBlockNodeOrIndentlessSequence();
                }
                this.state = this.ParseBlockMappingKey;
                return this.ProcessEmptyScalar(value.EndMark);
            }
            this.state = this.ParseBlockMappingKey;
            return this.ProcessEmptyScalar(this.PeekToken().StartMark);
        }

        private Events.Base ParseFlowSequenceFirstEntry()
        {
            var token = this.GetToken();
            this.marks.Push(token.StartMark);
            return this.ParseFlowSequenceEntry(true);
        }

        private Events.Base ParseFlowSequenceEntry()
        {
            return this.ParseFlowSequenceEntry(false);
        }

        private Events.Base ParseFlowSequenceEntry(bool isFirst)
        {
            Tokens.Base token;
            Events.Base value;
            if (!this.CheckToken<Tokens.FlowSequenceEnd>())
            {
                if (!isFirst)
                {
                    if (this.CheckToken<Tokens.FlowEntry>())
                        this.GetToken();
                    else
                    {
                        token = this.PeekToken();
                        throw new Error("while parsing a flow sequence", this.marks.Peek(),
                            "expected ',' or ']', but found " + token.Id, token.StartMark);
                    }
                }
                if (this.CheckToken<Tokens.Key>())
                {
                    token = this.PeekToken();
                    value = new Events.MappingStart
                            {
                                StartMark = token.StartMark,
                                EndMark = token.EndMark,
                                IsImplicit = true,
                                FlowStyle = FlowStyle.Flow
                            };
                    this.state = this.ParseFlowSequenceEntryMappingKey;
                    return value;
                }
                if (!this.CheckToken<Tokens.FlowSequenceEnd>())
                {
                    this.states.Push(this.ParseFlowSequenceEntry);
                    return this.ParseFlowNode();
                }
            }
            token = this.GetToken();
            value = new Events.SequenceEnd
                    {
                        StartMark = token.StartMark,
                        EndMark = token.EndMark
                    };
            this.state = this.states.Pop();
            this.marks.Pop();
            return value;
        }

        private Events.Node ParseFlowSequenceEntryMappingKey()
        {
            var token = this.GetToken();
            if (!this.CheckToken<Tokens.Value, Tokens.FlowEntry, Tokens.FlowSequenceEnd>())
            {
                this.states.Push(this.ParseFlowSequenceEntryMappingValue);
                return this.ParseFlowNode();
            }
            this.state = this.ParseFlowSequenceEntryMappingValue;
            return this.ProcessEmptyScalar(token.EndMark);
        }

        private Events.Base ParseFlowSequenceEntryMappingValue()
        {
            Tokens.Base token;
            if (this.CheckToken<Tokens.Value>())
            {
                token = this.GetToken();
                if (!this.CheckToken<Tokens.FlowEntry, Tokens.FlowSequenceEnd>())
                {
                    this.states.Push(this.ParseFlowSequenceEntryMappingEnd);
                    return this.ParseFlowNode();
                }
                this.state = this.ParseFlowSequenceEntryMappingEnd;
                return this.ProcessEmptyScalar(token.EndMark);
            }
            this.state = this.ParseFlowSequenceEntryMappingEnd;
            token = this.PeekToken();
            return this.ProcessEmptyScalar(token.StartMark);
        }

        private Events.MappingEnd ParseFlowSequenceEntryMappingEnd()
        {
            this.state = this.ParseFlowSequenceEntry;
            var token = this.PeekToken();
            return new Events.MappingEnd { StartMark = token.StartMark, EndMark = token.EndMark };
        }

        private Events.Base ParseFlowMappingFirstKey()
        {
            var token = this.GetToken();
            this.marks.Push(token.StartMark);
            return this.ParseFlowMappingKey(true);
        }

        private Events.Base ParseFlowMappingKey()
        {
            return this.ParseFlowMappingKey(false);
        }

        private Events.Base ParseFlowMappingKey(bool isFirst)
        {
            Tokens.Base token;
            if (!this.CheckToken<Tokens.FlowMappingEnd>())
            {
                if (!isFirst)
                {
                    if (this.CheckToken<Tokens.FlowEntry>())
                        this.GetToken();
                    else
                    {
                        token = this.PeekToken();
                        throw new Error("while parsing a flow mapping", this.marks.Peek(),
                            "expected ',' or '}', but found " + token.Id, token.StartMark);
                    }
                }
                if (this.CheckToken<Tokens.Key>())
                {
                    token = this.GetToken();
                    if (!this.CheckToken<Tokens.Value, Tokens.FlowEntry,
                        Tokens.FlowMappingEnd>())
                    {
                        this.states.Push(this.ParseFlowMappingValue);
                        return this.ParseFlowNode();
                    }
                    this.state = this.ParseFlowMappingValue;
                    return this.ProcessEmptyScalar(token.EndMark);
                }
                if (!this.CheckToken<Tokens.FlowMappingEnd>())
                {
                    this.states.Push(this.ParseFlowMappingEmptyValue);
                    return this.ParseFlowNode();
                }
            }
            token = this.GetToken();
            var value = new Events.MappingEnd
                        {
                            StartMark = token.StartMark,
                            EndMark = token.EndMark
                        };
            this.state = this.states.Pop();
            this.marks.Pop();
            return value;
        }

        private Events.Base ParseFlowMappingValue()
        {
            Tokens.Base token;
            if (this.CheckToken<Tokens.Value>())
            {
                token = this.GetToken();
                if (!this.CheckToken<Tokens.FlowEntry, Tokens.FlowMappingEnd>())
                {
                    this.states.Push(this.ParseFlowMappingKey);
                    return this.ParseFlowNode();
                }
                this.state = this.ParseFlowMappingKey;
                return this.ProcessEmptyScalar(token.EndMark);
            }
            this.state = this.ParseFlowMappingKey;
            token = this.PeekToken();
            return this.ProcessEmptyScalar(token.StartMark);
        }

        private Events.Scalar ParseFlowMappingEmptyValue()
        {
            this.state = this.ParseFlowMappingKey;
            return this.ProcessEmptyScalar(this.PeekToken().StartMark);
        }

        private Events.Scalar ProcessEmptyScalar(Mark mark)
        {
            return new Events.Scalar
                   {
                       ImplicitLevel = ScalarImplicitLevel.Plain,
                       Value = string.Empty,
                       StartMark = mark,
                       EndMark = mark
                   };
        }
    }
}
