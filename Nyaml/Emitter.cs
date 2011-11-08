namespace Nyaml
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    public enum LineBreak
    {
        CarriageReturn,
        LineFeed,
        CarriageReturnLineFeed
    }

    public interface IEmitter
    {
        void Reset();
        void Emit(Events.Base @event);
    }

    public class Emitter : IEmitter
    {
        [Serializable]
        public class Error : YamlError
        {
            public Error(string message) : base(message) { }
        }

        private class ScalarAnalysis
        {
            public string Scalar { get; set; }
            public bool IsEmpty { get; set; }
            public bool IsMultiline { get; set; }
            public bool AllowFlowPlain { get; set; }
            public bool AllowBlockPlain { get; set; }
            public bool AllowSingleQuoted { get; set; }
            public bool AllowDoubleQuoted { get; set; }
            public bool AllowBlock { get; set; }
        }

        private static readonly Dictionary<string, string> defaultTagPrefixes =
            new Dictionary<string, string>()
            {
                { "!", "!" },
                { "tag:yaml.org,2002:", "!!" }
            };

        private readonly Stream stream;
        private StreamWriter writer;

        private Encoding encoding;

        private readonly Stack<Action> states = new Stack<Action>();
        private Action state;

        private readonly Queue<Events.Base> events = new Queue<Events.Base>();
        private Events.Base currentEvent;

        private readonly Stack<int> indents = new Stack<int>();
        private int indent = -1;

        private int flowLevel;

        private bool isRootContext;
        private bool isSequenceContext;
        private bool isMappingContext;
        private bool isSimpleKeyContext;

        private int line;
        private int column;
        private bool isWhitespace = true;
        private bool isIndentation = true;

        private bool isOpenEnded;

        private readonly bool isCanonical;
        private readonly bool allowUnicode;
        
        private readonly int bestIndent = 2;
        private readonly int bestWidth = 80;
        private readonly string bestLineBreak;

        private Dictionary<string, string> tagPrefixes;

        private string preparedAnchor;
        private string preparedTag;

        private ScalarAnalysis analysis;
        private Style style;

        public Emitter(Stream stream, bool isCanonical = false, int indent = 2, 
            int width = 80,
            bool allowUnicode = true, LineBreak lineBreak = LineBreak.LineFeed)
        {
            this.stream = stream;

            this.state = this.ExpectStreamStart;

            this.isCanonical = isCanonical;
            this.allowUnicode = allowUnicode;

            if (indent > 1 && indent < 10)
                this.bestIndent = indent;
            if (width > this.bestIndent * 2)
                this.bestWidth = width;
            switch (lineBreak)
            {
                case LineBreak.CarriageReturn:
                    this.bestLineBreak = "\r";
                    break;
                case LineBreak.LineFeed:
                    this.bestLineBreak = "\n";
                    break;
                case LineBreak.CarriageReturnLineFeed:
                    this.bestLineBreak = "\r\n";
                    break;
            }
        }

        public void Reset()
        {
            this.states.Clear();
            this.state = null;
        }

        public void Emit(Events.Base @event)
        {
            this.events.Enqueue(@event);
            while (!this.NeedMoreEvents)
            {
                this.currentEvent = this.events.Dequeue();
                this.state();
                this.currentEvent = null;
            }
        }

        private bool NeedMoreEvents
        {
            get
            {
                if (this.events.Count == 0)
                    return true;
                var @event = this.events.Peek();
                if (@event is Events.DocumentStart)
                    return this.NeedEvents(1);
                if (@event is Events.SequenceStart)
                    return this.NeedEvents(2);
                if (@event is Events.MappingStart)
                    return this.NeedEvents(3);
                return false;
            }
        }

        private bool NeedEvents(int count)
        {
            var level = 0;
            var index = 0;
            foreach (var @event in this.events.Where(@event => index++ != 0))
            {
                if (@event is Events.DocumentStart || @event is Events.CollectionStart)
                    level++;
                else if (@event is Events.DocumentEnd || @event is Events.CollectionEnd)
                    level--;
                else if (@event is Events.StreamEnd)
                    level = -1;
                if (level < 0)
                    return false;
            }
            return this.events.Count < count + 1;
        }

        private void IncreaseIndent(bool isFlow = false, bool isIndentless = false)
        {
            this.indents.Push(this.indent);
            if (this.indent == -1)
                this.indent = isFlow ? this.bestIndent : 0;
            else if (!isIndentless)
                this.indent += this.bestIndent;
        }

        private void ExpectStreamStart()
        {
            var start = this.currentEvent as Events.StreamStart;
            if (start == null)
                throw new Error("expected StreaStart event, but got " + this.currentEvent);
            if (start.Encoding != null)
                this.encoding = start.Encoding;
            this.writer = new StreamWriter(this.stream, this.encoding);
            // this.WriteStreamStart();
            this.state = this.ExpectFirstDocumentStart;
        }

        private void ExpectNothing()
        {
            throw new Error("expected no events, but got " + this.currentEvent);
        }

        private void ExpectFirstDocumentStart()
        {
            this.ExpectDocumentStart(true);
        }

        private void ExpectDocumentStart()
        {
            this.ExpectDocumentStart(false);
        }

        private void ExpectDocumentStart(bool isFirst)
        {
            var start = this.currentEvent as Events.DocumentStart;
            if (start != null)
            {
                if ((start.Version != null || start.Tags.Count > 0) && this.isOpenEnded)
                {
                    this.WriteIndicator("...", true);
                    this.WriteIndent();
                }
                if (start.Version != null)
                    this.WriteVersionDirective(PrepareVersion(start.Version));
                this.tagPrefixes = new Dictionary<string, string>(defaultTagPrefixes);
                if (start.Tags.Count != 0)
                    foreach (var kvp in start.Tags)
                    {
                        var handle = kvp.Key;
                        var prefix = kvp.Value;
                        this.tagPrefixes[prefix] = handle;
                        var handleText = PrepareTagHandle(handle);
                        var prefixText = PrepareTagPrefix(prefix);
                        this.WriteTagDirective(handleText, prefixText);
                    }
                if (!isFirst || start.IsExplicit || this.isCanonical ||
                     start.Version != null || start.Tags.Count > 0 ||
                     this.CheckEmptyDocumnet())
                {
                    this.WriteIndent();
                    this.WriteIndicator("---", true);
                    if (this.isCanonical)
                        this.WriteIndent();
                }
                this.state = this.ExpectDocumnetRoot;
                return;
            }
            var end = this.currentEvent as Events.StreamEnd;
            if (end != null)
            {
                if (this.isOpenEnded)
                {
                    this.WriteIndicator("...", true);
                    this.WriteIndent();
                }
                // this.WriteStreamEnd();
                this.writer.Flush();
                this.state = this.ExpectNothing;
            }
            else
                throw new Error("expected DocumnetStart event, but got " + this.currentEvent);
        }

        private void ExpectDocumentEnd()
        {
            var end = this.currentEvent as Events.DocumentEnd;
            if (end == null) 
                throw new Error("expected DocumnetEnd event, but got " + this.currentEvent);
            this.WriteIndent();
            if (end.IsExplicit)
            {
                this.WriteIndicator("...", true);
                this.WriteIndent();
            }
            this.writer.Flush();
            this.state = this.ExpectDocumentStart;
        }

        private void ExpectDocumnetRoot()
        {
            this.states.Push(this.ExpectDocumentEnd);
            this.ExpectNode(true);
        }

        private void ExpectNode(bool isRoot = false, bool isSequence = false,
            bool isMapping = false, bool isSimpleKey = false)
        {
            this.isRootContext = isRoot;
            this.isSequenceContext = isSequence;
            this.isMappingContext = isMapping;
            this.isSimpleKeyContext = isSimpleKey;

            if (this.currentEvent is Events.Alias)
            {
                this.ExpectAlias();
                return;
            }
            if (!(this.currentEvent is Events.Scalar) && !(this.currentEvent is Events.CollectionStart))
                throw new Error("expected Node event, but got " + this.currentEvent);
            this.ProcessAnchor("&");
            this.ProcessTag();
            if (this.currentEvent is Events.Scalar)
            {
                this.ExpectScalar();
                return;
            }
            var ce = (Events.CollectionStart) this.currentEvent;
            if (ce is Events.SequenceStart)
            {
                if (this.flowLevel != 0 || this.isCanonical
                    || ce.FlowStyle != FlowStyle.None || this.CheckEmptySequence())
                    this.ExpectFlowSequence();
                else
                    this.ExpectBlockSequence();
            }
            else
            {
                if (this.flowLevel != 0 || this.isCanonical
                    || ce.FlowStyle != FlowStyle.None || this.CheckEmptyMapping())
                    this.ExpectFlowMapping();
                else
                    this.ExpectBlockMapping();
            }
        }

        private void ExpectAlias()
        {
            if (((Events.Alias)this.currentEvent).Anchor == null)
                throw new Error("anchor is not specified for alias");
            this.ProcessAnchor("*");
            this.state = this.states.Pop();
        }

        private void ExpectScalar()
        {
            this.IncreaseIndent(true);
            this.ProcessScalar();
            this.indent = this.indents.Pop();
            this.state = this.states.Pop();
        }

        private void ExpectFlowSequence()
        {
            this.WriteIndicator("[", true, true);
            this.flowLevel++;
            this.IncreaseIndent(true);
            this.state = this.ExpectFirstFlowSequenceItem;
        }

        private void ExpectFirstFlowSequenceItem()
        {
            if (this.currentEvent is Events.SequenceEnd)
            {
                this.indent = this.indents.Pop();
                this.flowLevel--;
                this.WriteIndicator("]", false);
                this.state = this.states.Pop();
            }
            else
            {
                if (this.isCanonical || this.column > this.bestWidth)
                    this.WriteIndent();
                this.states.Push(this.ExpectFlowSequenceItem);
                this.ExpectNode(true);
            }
        }

        private void ExpectFlowSequenceItem()
        {
            if (this.currentEvent is Events.SequenceEnd)
            {
                this.indent = this.indents.Pop();
                this.flowLevel--;
                if (this.isCanonical)
                {
                    this.WriteIndicator(",", false);
                    this.WriteIndent();
                }
                this.WriteIndicator("]", false);
                this.state = this.states.Pop();
            }
            else
            {
                this.WriteIndicator(",", false);
                if (this.isCanonical || this.column > this.bestWidth)
                    this.WriteIndent();
                this.states.Push(this.ExpectFlowSequenceItem);
                this.ExpectNode(isSequence:true);
            }
        }

        private void ExpectFlowMapping()
        {
            this.WriteIndicator("{", true, true);
            this.flowLevel++;
            this.IncreaseIndent(true);
            this.state = this.ExpectFirstFlowMappingKey;
        }

        private void ExpectFirstFlowMappingKey()
        {
            if (this.currentEvent is Events.MappingEnd)
            {
                this.indent = this.indents.Pop();
                this.flowLevel--;
                this.WriteIndicator("}", false);
                this.state = this.states.Pop();
            }
            else
            {
                if (this.isCanonical || this.column > this.bestWidth)
                    this.WriteIndent();
                if (!this.isCanonical && this.CheckSimpleKey())
                {
                    this.states.Push(this.ExpectFlowMappingSimpleValue);
                    this.ExpectNode(isMapping:true,isSimpleKey:true);
                }
                else
                {
                    this.WriteIndicator("?", true);
                    this.states.Push(this.ExpectFlowMappingValue);
                    this.ExpectNode(isMapping: true);
                }
            }
        }

        private void ExpectFlowMappingKey()
        {
            if (this.currentEvent is Events.MappingEnd)
            {
                this.indent = this.indents.Pop();
                this.flowLevel--;
                if (this.isCanonical)
                {
                    this.WriteIndicator(",", false);
                    this.WriteIndent();
                }
                this.WriteIndicator("}", false);
                this.state = this.states.Pop();
            }
            else
            {
                this.WriteIndicator(",", false);
                if (this.isCanonical || this.column > this.bestWidth)
                    this.WriteIndent();
                if (!this.isCanonical && this.CheckSimpleKey())
                {
                    this.states.Push(this.ExpectFlowMappingSimpleValue);
                    this.ExpectNode(isMapping: true, isSimpleKey: true);
                }
                else
                {
                    this.WriteIndicator("?", true);
                    this.states.Push(this.ExpectFlowMappingValue);
                    this.ExpectNode(isMapping: true);
                }
            }
        }

        private void ExpectFlowMappingSimpleValue()
        {
            this.WriteIndicator(":", false);
            this.states.Push(this.ExpectFlowMappingKey);
            this.ExpectNode(isMapping:true);
        }

        private void ExpectFlowMappingValue()
        {
            if (this.isCanonical || this.column > this.bestWidth)
                this.WriteIndent();
            this.WriteIndicator(":", true);
            this.states.Push(this.ExpectFlowMappingKey);
            this.ExpectNode(isMapping: true);
        }

        private void ExpectBlockSequence()
        {
            var indentless = this.isMappingContext && !this.isIndentation;
            this.IncreaseIndent(false, indentless);
            this.state = this.ExpectFirstBlockSequenceItem;
        }

        private void ExpectFirstBlockSequenceItem()
        {
            this.ExpectBlockSequenceItem(true);
        }

        private void ExpectBlockSequenceItem()
        {
            this.ExpectBlockSequenceItem(false);
        }

        private void ExpectBlockSequenceItem(bool isFirst)
        {
            if (!isFirst && this.currentEvent is Events.SequenceEnd)
            {
                this.indent = this.indents.Pop();
                this.state = this.states.Pop();
            }
            else
            {
                this.WriteIndent();
                this.WriteIndicator("-", true, hasIndentation: true);
                this.states.Push(this.ExpectBlockSequenceItem);
                this.ExpectNode(isSequence:true);
            }
        }

        private void ExpectBlockMapping()
        {
            this.IncreaseIndent();
            this.state = this.ExpectFirstBlockMappingKey;
        }

        private void ExpectFirstBlockMappingKey()
        {
            this.ExpectBlockMappingKey(true);
        }

        private void ExpectBlockMappingKey()
        {
            this.ExpectBlockMappingKey(false);
        }

        private void ExpectBlockMappingKey(bool isFirst)
        {
            if (!isFirst && this.currentEvent is Events.MappingEnd)
            {
                this.indent = this.indents.Pop();
                this.state = this.states.Pop();
            }
            else
            {
                this.WriteIndent();
                if (this.CheckSimpleKey())
                {
                    this.states.Push(this.ExpectBlockMappingSimpleValue);
                    this.ExpectNode(isMapping:true);
                }
                else
                {
                    this.WriteIndicator("?", true, hasIndentation: true);
                    this.states.Push(this.ExpectBlockMappingValue);
                    this.ExpectNode(isMapping:true);
                }
            }
        }

        private void ExpectBlockMappingSimpleValue()
        {
            this.WriteIndicator(":", false);
            this.states.Push(this.ExpectBlockMappingKey);
            this.ExpectNode(isMapping:true);
        }

        private void ExpectBlockMappingValue()
        {
            this.WriteIndent();
            this.WriteIndicator(":", true, hasIndentation: true);
            this.states.Push(this.ExpectBlockMappingKey);
            this.ExpectNode(isMapping:true);
        }

        private bool CheckEmptySequence()
        {
            return this.currentEvent is Events.SequenceStart && this.events.Count != 0
                   && this.events.Peek() is Events.SequenceEnd;
        }

        private bool CheckEmptyMapping()
        {
            return this.currentEvent is Events.MappingStart && this.events.Count != 0
                   && this.events.Peek() is Events.MappingEnd;
        }

        private bool CheckEmptyDocumnet()
        {
            if (!(this.currentEvent is Events.DocumentStart) || this.events.Count == 0)
                return false;
            var sa = this.events.Peek() as Events.Scalar;
            return sa != null && sa.Anchor == null && sa.Tag == null &&
                   sa.ImplicitLevel != ScalarImplicitLevel.None && string.IsNullOrEmpty(sa.Value);
        }

        private bool CheckSimpleKey()
        {
            var length = 0;
            var nodeEvent = this.currentEvent as Events.Node;
            if (nodeEvent != null)
            {
                if (nodeEvent.Anchor != null)
                {
                    if (this.preparedAnchor == null)
                        this.preparedAnchor = PrepareAnchor(nodeEvent.Anchor);
                    length += this.preparedAnchor.Length;
                }
                var scalarEvent = nodeEvent as Events.Scalar;
                var collectionEvent = nodeEvent as Events.CollectionStart;
                if (scalarEvent != null || collectionEvent != null)
                {
                    var tag = scalarEvent != null ? scalarEvent.Tag : collectionEvent.Tag;
                    if (tag != null)
                    {
                        if (this.preparedTag == null)
                            this.preparedTag = this.PrepareTag(tag);
                        length += this.preparedTag.Length;
                    }
                }
                if (scalarEvent != null)
                {
                    if (this.analysis == null)
                        this.analysis = this.AnalyzeScalar(scalarEvent.Value);
                    length += this.analysis.Scalar.Length;
                }
            }
            return length < 128 && (this.currentEvent is Events.Alias
                                     || (this.currentEvent is Events.Scalar && !this.analysis.IsEmpty
                                     && !this.analysis.IsMultiline) || this.CheckEmptySequence()
                    || this.CheckEmptyMapping());
        }

        private void ProcessAnchor(string indicator)
        {
            var nodeEvent = this.currentEvent as Events.Node;
            if (nodeEvent == null || nodeEvent.Anchor == null)
            {
                this.preparedAnchor = null;
                return;
            }
            if (this.preparedAnchor == null)
                this.preparedAnchor = PrepareAnchor(nodeEvent.Anchor);
            if (this.preparedAnchor != null)
                this.WriteIndicator(indicator + this.preparedAnchor, true);
            this.preparedAnchor = null;
        }

        private void ProcessTag()
        {
            var scalarEvent = this.currentEvent as Events.Scalar;
            var collectionEvent = this.currentEvent as Events.CollectionStart;
            if (scalarEvent == null && collectionEvent == null)
                return;
            var tag = scalarEvent != null ? scalarEvent.Tag : collectionEvent.Tag;
            if (scalarEvent != null)
            {
                if (scalarEvent.Style == Style.None)
                    this.style = this.ChooseScalarStyle();
                if ((!this.isCanonical || tag == null)
                    && ((this.style == Style.None && scalarEvent.ImplicitLevel == ScalarImplicitLevel.Plain)
                    || (this.style != Style.None && scalarEvent.ImplicitLevel == ScalarImplicitLevel.NonPlain)))
                {
                    this.preparedTag = null;
                    return;
                }
                if (scalarEvent.ImplicitLevel == ScalarImplicitLevel.Plain && tag == null)
                {
                    tag = "!";
                    this.preparedTag = null;
                }
            }
            else if ((this.isCanonical || tag == null) && collectionEvent.IsImplicit)
            {
                this.preparedTag = null;
                return;
            }
            if (tag == null)
                throw new Error("tag is not specified");
            if (this.preparedTag == null)
                this.preparedTag = this.PrepareTag(tag);
            if (this.preparedTag != null)
                this.WriteIndicator(this.preparedTag, true);
            this.preparedTag = null;
        }

        private Style ChooseScalarStyle()
        {
            var eventNode = (Events.Scalar)this.currentEvent;
            if (this.analysis == null)
                this.analysis = this.AnalyzeScalar(eventNode.Value);
            if (eventNode.Style == Style.Double || this.isCanonical)
                return Style.Double;
            if (eventNode.Style == Style.None && eventNode.ImplicitLevel == ScalarImplicitLevel.Plain)
            {
                if (!(this.isSimpleKeyContext &&
                    (this.analysis.IsEmpty || this.analysis.IsMultiline))
                    && (this.flowLevel != 0 && this.analysis.AllowFlowPlain
                    || (this.flowLevel == 0 && this.analysis.AllowBlockPlain)))
                    return Style.None;
            }
            if (eventNode.Style == Style.Folded || eventNode.Style == Style.Literal)
                if (this.flowLevel == 0 && !this.isSimpleKeyContext
                    && this.analysis.AllowBlock)
                    return eventNode.Style;
            if (eventNode.Style == Style.None || eventNode.Style == Style.Single)
                if (this.analysis.AllowSingleQuoted && !
                    (this.isSimpleKeyContext
                    && this.analysis.IsMultiline))
                    return Style.Single;
            return Style.Double;
        }

        private void ProcessScalar()
        {
            if (this.analysis == null)
                this.analysis = this.AnalyzeScalar(((Events.Scalar) this.currentEvent).Value);
            if (this.style == Style.None)
                this.style = this.ChooseScalarStyle();
            var split = !this.isSimpleKeyContext;
            switch (this.style)
            {
                case Style.Double:
                    this.WriteDoubleQuoted(this.analysis.Scalar, split);
                    break;
                case Style.Single:
                    this.WriteSingleQuoted(this.analysis.Scalar, split);
                    break;
                case Style.Folded:
                    this.WriteFolded(this.analysis.Scalar);
                    break;
                case Style.Literal:
                    this.WriteLiteral(this.analysis.Scalar);
                    break;
                default:
                    this.WritePlain(this.analysis.Scalar, split);
                    break;
            }
            this.analysis = null;
            this.style = Style.None;
        }

        private static string PrepareVersion(Tuple<string, string> version)
        {
            if (version.Item1 != "1")
                throw new Error("unsupported YAML version: " + version.Item1 + "." + version.Item2);
            return version.Item1 + "." + version.Item2;
        }

        private static string PrepareTagHandle(string handle)
        {
            if (string.IsNullOrEmpty(handle))
                throw new Error("tag handle must not be empty");
            if (handle[0] != '!' || handle[handle.Length - 1] != '!')
                throw new Error("tag handle must start and end with '!': " + handle);
            for (var i = 1; i < handle.Length - 1; i++)
            {
                var ch = handle[i];
                if ((ch < '0' || ch > '9') && (ch < 'A' || ch > 'Z')
                    && (ch < 'a' || ch > 'z') && ch != '-' && ch != '_')
                    throw new Error(string.Format("invalid character {0} in the tag handle: {1}", ch, handle));
            }
            return handle;
        }

        private static string PrepareTagPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new Error("tag prefix must not be empty");
            var chunks = new StringBuilder();
            var start = 0;
            var end = 0;
            if (prefix[0] == '!')
                end = 1;
            while (end < prefix.Length)
            {
                var ch = prefix[end];
                if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z')
                    || (ch >= 'a' && ch <= 'z') || "-;/?!:@&=+$,_.~*'()[]".IndexOf(ch) != -1)
                    end++;
                else
                {
                    if (start < end)
                        chunks.Append(prefix.Substring(start, end - start));
                    start = end = end + 1;
                    foreach (var data in Encoding.UTF8.GetBytes(new[] { ch }))
                        chunks.AppendFormat("%{0:X2}", data);
                }
            }
            if (start < end)
                chunks.Append(prefix.Substring(start, end - start));
            return chunks.ToString();
        }

        private string PrepareTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                throw new Error("tag must not be empty");
            if (tag == "!")
                return tag;
            string handle = null;
            var suffix = tag;
            foreach (var kvp in this.tagPrefixes)
            {
                var prefix = kvp.Key;
                if (!tag.StartsWith(prefix) || (prefix != "!" && prefix.Length >= tag.Length)) 
                    continue;
                handle = kvp.Value;
                suffix = tag.Substring(prefix.Length);
            }
            var chunks = new StringBuilder();
            var start = 0;
            var end = 0;
            while (end< suffix.Length)
            {
                var ch = suffix[end];
                if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z')
                    || (ch >= 'a' && ch <= 'z') || "-;/?!:@&=+$,_.~*'()[]".IndexOf(ch) != -1
                    || (ch == '!' && handle != "!"))
                    end++;
                else
                {
                    if (start < end)
                        chunks.Append(suffix.Substring(start, end - start));
                    start = ++end;

                    foreach (var data in Encoding.UTF8.GetBytes(new[] { ch }))
                        chunks.AppendFormat("%{0:X2}", data);
                } 
            }
            if (start  <end)
                chunks.Append(suffix.Substring(start, end - start));
            return string.IsNullOrEmpty(handle)
                       ? string.Format("!<{0}>", chunks)
                       : string.Format("{0}.{1}", handle, chunks);
        }

        private static string PrepareAnchor(string anchor)
        {
            if (string.IsNullOrEmpty(anchor))
                throw new Error("anchor must not be empty");
            foreach (var ch in anchor.Where(ch => (ch < '0' || ch > '9') && (ch < 'A' || ch > 'Z')
                                                  && (ch < 'a' || ch > 'z') && ch != '-' && ch != '_'))
                throw new Error(string.Format("invalid character {0} in the anchor: {1}", ch, anchor));
            return anchor;
        }

        private ScalarAnalysis AnalyzeScalar(string scalar)
        {
            if (string.IsNullOrEmpty(scalar))
                return new ScalarAnalysis
                       {
                           Scalar = scalar,
                           IsEmpty = true,
                           IsMultiline = false,
                           AllowFlowPlain = false,
                           AllowBlockPlain = true,
                           AllowSingleQuoted = true,
                           AllowDoubleQuoted = true,
                           AllowBlock = false
                       };
            var hasBlockIndicators = false;
            var hasFlowIndicators = false;
            var hasLineBreaks = false;
            var hasSpecialCharacters = false;

            var hasLeadingSpace = false;
            var hasLeadingBreak = false;
            var hasTrailingSpace = false;
            var hasTrailingBreak = false;
            var hasBreakSpace = false;
            var hasSpaceBreak = false;

            if (scalar.StartsWith("---") || scalar.StartsWith("..."))
                hasBlockIndicators = hasFlowIndicators = true;

            var isPreceededByWhitespace = true;

            var isFollowedByWhitespace = scalar.Length == 1 ||
                                         "\0 \t\r\n\x85\u2028\u2029".IndexOf(scalar[1]) != -1;

            var hasPreviousSpace = false;

            var hasPreviousBreak = false;

            var index = 0;
            while (index < scalar.Length)
            {
                var ch = scalar[index];

                if (index == 0)
                {
                    if ("#,[]{}&*!|>'\"%@`".IndexOf(ch) != -1)
                        hasFlowIndicators = hasBlockIndicators = true;
                    if (ch == '?' || ch == ':')
                    {
                        hasFlowIndicators = true;
                        if (isFollowedByWhitespace)
                            hasBlockIndicators = true;
                    }
                    if (ch == '-' && isFollowedByWhitespace)
                        hasFlowIndicators = hasBlockIndicators = true;
                }
                else
                {
                    if (",?[]{}".IndexOf(ch) != -1)
                        hasFlowIndicators = true;
                    if (ch == ':')
                    {
                        hasFlowIndicators = true;
                        if (isFollowedByWhitespace)
                            hasBlockIndicators = true;
                    }
                    if (ch == '#' && isPreceededByWhitespace)
                        hasFlowIndicators = hasBlockIndicators = true;
                }

                if ("\n\x85\u2028\u2029".IndexOf(ch) != -1)
                    hasLineBreaks = true;
                if (ch != '\n' && ch < '\x20' && ch > '\x7E')
                {
                    if ((ch == '\x85' || (ch >= '\xA0' && ch <= '\uD7FF')
                        || (ch >= '\uE000' && ch <= '\uFFFD')) && ch != '\uFEFF')
                    {
                        if (!this.allowUnicode)
                            hasSpecialCharacters = true;
                    }
                    else
                        hasSpecialCharacters = true;
                }

                if (ch == ' ')
                {
                    if (index == 0)
                        hasLeadingSpace = true;
                    if (index == scalar.Length - 1)
                        hasTrailingSpace = true;
                    if (hasPreviousBreak)
                        hasBreakSpace = true;
                    hasPreviousBreak = false;
                    hasPreviousSpace = true;
                }
                else if ("\n\x85\u2028\u2029".IndexOf(ch) != -1)
                {
                    if (index == 0)
                        hasLeadingBreak = true;
                    if (index == scalar.Length - 1)
                        hasTrailingBreak = true;
                    if (hasPreviousSpace)
                        hasSpaceBreak = true;
                    hasPreviousBreak = true;
                    hasPreviousSpace = false;
                }
                else
                    hasPreviousBreak = hasPreviousSpace = false;

                index++;
                isPreceededByWhitespace = "\0 \t\r\n\x85\u2028\u2029".IndexOf(ch) != -1;
                isFollowedByWhitespace = index + 1 >= scalar.Length ||
                                         "\0 \t\r\n\x85\u2028\u2029".IndexOf(scalar[index + 1]) != -1;
            }

            var allowFlowPlain = true;
            var allowBlockPlain = true;
            var allowSingleQuoted = true;
            const bool allowDoubleQuoated = true;
            var allowBlock = true;

            if (hasLeadingSpace || hasLeadingBreak || hasTrailingSpace || hasTrailingBreak)
                allowFlowPlain = allowBlockPlain = false;

            if (hasTrailingSpace)
                allowBlock = false;

            if (hasBreakSpace)
                allowFlowPlain = allowBlockPlain = allowSingleQuoted = false;

            if (hasSpaceBreak || hasSpecialCharacters)
                allowFlowPlain = allowBlockPlain = allowSingleQuoted = allowBlock = false;

            if (hasLineBreaks)
                allowFlowPlain = allowBlockPlain = false;

            if (hasFlowIndicators)
                allowFlowPlain = false;

            if (hasBlockIndicators)
                allowBlockPlain = false;

            return new ScalarAnalysis
                   {
                       Scalar = scalar,
                       IsEmpty = false,
                       IsMultiline = hasLineBreaks,
                       AllowFlowPlain = allowFlowPlain,
                       AllowBlockPlain = allowBlockPlain,
                       AllowSingleQuoted = allowSingleQuoted,
                       AllowDoubleQuoted = allowDoubleQuoated,
                       AllowBlock = allowBlock
                   };
        }

        // private void WriteStreamStart()

        // private void WriteStreamEnd()
        
        private void WriteIndicator(string indicator, bool needsWhitespace,
            bool hasWhitespace = false, bool hasIndentation = false)
        {
            var data = this.isWhitespace || !needsWhitespace 
                ? indicator 
                : " " + indicator;
            this.isWhitespace = hasWhitespace;
            this.isIndentation &= hasIndentation;
            this.column += data.Length;
            this.isOpenEnded = false;
            this.writer.Write(data);
        }

        private void WriteIndent()
        {
            var indentation = Math.Max(this.indent, 0);
            if (!this.isIndentation || this.column > indentation
                || (this.column == indentation && !this.isWhitespace))
                this.WriteLineBreak();
            if (this.column < indentation)
            {
                this.isWhitespace = true;
                var data = new string(' ', indentation - this.column);
                this.column = indentation;
                this.writer.Write(data);
            }
        }

        private void WriteLineBreak(string data = null)
        {
            if (data == null)
                data = this.bestLineBreak;
            this.isWhitespace = true;
            this.isIndentation = true;
            this.line++;
            this.column = 0;
            this.writer.Write(data);
        }

        private void WriteVersionDirective(string versionText)
        {
            var data = "%YAML " + versionText;
            this.writer.Write(data);
            this.WriteLineBreak();
        }

        private void WriteTagDirective(string handleText, string prefixText)
        {
            var data = string.Format("%TAG {0} {1}", handleText, prefixText);
            this.writer.Write(data);
            this.WriteLineBreak();
        }

        private void WriteSingleQuoted(string text, bool split = true)
        {
            this.WriteIndicator("'", true);
            var spaces = false;
            var breaks = false;
            var start = 0;
            var end = 0;
            while (end <= text.Length)
            {
                char? ch = null;
                if (end < text.Length)
                    ch = text[end];
                if (spaces)
                {
                    if (ch == null || ch != ' ')
                    {
                        if (start + 1 == end && this.column > this.bestWidth && split
                            && start != 0 && end != text.Length)
                            this.WriteIndent();
                        else
                        {
                            var data = text.Substring(start, end - start);
                            this.column += end - start;
                            this.writer.Write(data);
                        }
                        start = end;
                    }
                }
                else if(breaks)
                {
                    if (ch == null || "\n\x85\u2028\u2029".IndexOf((char)ch) == -1)
                    {
                        if (text[start] == '\n')
                            this.WriteLineBreak();
                        for (var i = start; i <= end; i++)
                        {
                            var br = text[i];
                            if (br == '\n')
                                this.WriteLineBreak();
                            else
                                this.WriteLineBreak(br.ToString());
                        }
                        this.WriteIndent();
                        start = end;
                    }
                }
                else if (ch == null || " \n\x85\u2028\u2029".IndexOf((char)ch) != -1 || ch == '\'')
                {
                    if (start < end)
                    {
                        var data = text.Substring(start, end - start);
                        this.column += data.Length;
                        this.writer.Write(data);
                        start = end;
                    }
                }
                if (ch == '\'')
                {
                    const string data = "''";
                    this.column += 2;
                    this.writer.Write(data);
                    start = end + 1;
                }
                if (ch != null)
                {
                    spaces = ch == ' ';
                    breaks = "\n\x85\u2028\u2029".IndexOf((char)ch) != -1;
                }
                end++;
            }
            this.WriteIndicator("'", false);
        }

        private static readonly Dictionary<char, char> escapeReplacements =
            new Dictionary<char, char>
            {
                { '\0',       '0' },
                { '\x07',     'a' },
                { '\x08',     'b' },
                { '\x09',     't' },
                { '\x0A',     'n' },
                { '\x0B',     'v' },
                { '\x0C',     'f' },
                { '\x0D',     'r' },
                { '\x1B',     'e' },
                { '\"',       '\"' },
                { '\\',       '\\' },
                { '\x85',     'N' },
                { '\xA0',     '_' },
                { '\u2028',   'L' },
                { '\u2029',   'P' },
            };

        private void WriteDoubleQuoted(string text, bool split = true)
        {
            this.WriteIndicator("\"", true);
            var start = 0;
            var end = 0;
            while (end <= text.Length)
            {
                char? ch = null;
                if (end < text.Length)
                    ch = text[end];
                if (ch == null || "\"\\\x85\u2028\u2029\uFEFF".IndexOf((char)ch) != -1
                    || !((ch >= '\x20' && ch <= '\x7E')
                    || (this.allowUnicode && ((ch >= '\xA0' && ch <= '\uD7FF')
                    || (ch >= '\uE000' && ch <= '\uFFFD')))))
                {
                    if (start < end)
                    {
                        var data = text.Substring(start, end - start);
                        this.column += end - start;
                        this.writer.Write(data);
                        start = end;
                    }
                    if (ch != null)
                    {
                        char e;
                        string data;
                        if (escapeReplacements.TryGetValue((char)ch, out e))
                            data = "\\" + e;
                        else if (ch <= '\xFF')
                            data = string.Format("\\x{0:X2}", (int)ch);
                        else
                            data = string.Format("\\x{0:X4}", (int)ch);
                        this.column += data.Length;
                        this.writer.Write(data);
                        start = end + 1;
                    }
                }
                if (end > 0 && end < text.Length - 1 && (ch == ' ' || start >= end)
                    && this.column + (end - start) > this.bestWidth && split)
                {
                    var data = text.Substring(start, end - start) + "\\";
                    if (start < end)
                        start = end;
                    this.column += data.Length;
                    this.writer.Write(data);
                    this.WriteIndent();
                    this.isWhitespace = false;
                    this.isIndentation = false;
                    if (text[start] == ' ')
                    {
                        data = "\\";
                        this.column++;
                        this.writer.Write(data);
                    }
                }
                end++;
            }
            this.WriteIndicator("\"", false);
        }

        private string DetermineBlockHints(string text)
        {
            var hints = string.Empty;
            if (!string.IsNullOrEmpty(text))
            {
                if (" \n\x85\u2028\u2029".IndexOf(text[0]) != -1)
                    hints = this.bestIndent.ToString(CultureInfo.InvariantCulture);
                if ("\n\x85\u2028\u2029".IndexOf(text[text.Length - 1]) == -1)
                    hints += "-";
                else if (text.Length == 1 || "\n\x85\u2028\u2029".IndexOf(text[text.Length - 2]) != -1)
                    hints += "+";
            }
            return hints;
        }

        private void WriteFolded(string text)
        {
            var hints = this.DetermineBlockHints(text);
            this.WriteIndicator(">" + hints, true);
            if (hints.EndsWith("+"))
                this.isOpenEnded = true;
            this.WriteLineBreak();
            var leadingSpace = true;
            var spaces = false;
            var breaks = true;
            var start = 0;
            var end = 0;
            while (end <= text.Length)
            {
                char? ch = null;
                if (end < text.Length)
                    ch = text[end];
                if (breaks)
                {
                    if (ch == null || "\n\x85\u2028\u2029".IndexOf((char)ch) == -1)
                    {
                        if (!leadingSpace && ch != null && ch != ' ' && text[start] == '\n')
                            this.WriteLineBreak();
                        leadingSpace = ch == ' ';
                        for (int i = start; i <= end; i++)
                        {
                            var br = text[i];
                            if (br == '\n')
                                this.WriteLineBreak();
                            else
                                this.WriteLineBreak(br.ToString());
                        }
                        if (ch != null)
                            this.WriteIndent();
                        start = end;
                    }
                }
                else if (spaces)
                {
                    if (ch != ' ')
                    {
                        if (start + 1 == end && this.column > this.bestWidth)
                            this.WriteIndent();
                        else
                        {
                            var data = text.Substring(start, end - start);
                            this.column += end - start;
                            this.writer.Write(data);
                        }
                        start = end;
                    }
                }
                else if (ch == null || " \n\x85\u2028\u2029".IndexOf((char)ch) != -1)
                {
                    var data = text.Substring(start, end - start);
                    this.column += end - start;
                    this.writer.Write(data);
                    if (ch == null)
                        this.WriteLineBreak();
                    start = end;
                }
                if (ch != null)
                {
                    breaks = "\n\x85\u2028\u2029".IndexOf((char) ch) != -1;
                    spaces = ch == ' ';
                }
                end++;
            }
        }

        private void WriteLiteral(string text)
        {
            var hints = this.DetermineBlockHints(text);
            this.WriteIndicator("|" + hints, true);
            if (hints.EndsWith("+"))
                this.isOpenEnded = true;
            this.WriteLineBreak();
            var breaks = true;
            var start = 0;
            var end = 0;
            while (end <= text.Length)
            {
                char? ch = null;
                if (end < text.Length)
                    ch = text[end];
                if (breaks)
                {
                    if (ch == null || "\n\x85\u2028\u2029".IndexOf((char)ch) == -1)
                    {
                        for (var i = start; i <= end; i++)
                        {
                            var br = text[i];
                            if (br == '\n')
                                this.WriteLineBreak();
                            else
                                this.WriteLineBreak(br.ToString());
                        }
                        if (ch != null)
                            this.WriteIndent();
                        start = end;
                    }
                }
                else if (ch == null || " \n\x85\u2028\u2029".IndexOf((char)ch) != -1)
                {
                    var data = text.Substring(start, end - start);
                    this.writer.Write(data);
                    if (ch == null)
                        this.WriteLineBreak();
                    start = end;
                }
                if (ch != null)
                    breaks = "\n\x85\u2028\u2029".IndexOf((char)ch) != -1;
                end++;
            }
        }

        private void WritePlain(string text, bool split = true)
        {
            if (this.isRootContext)
                this.isOpenEnded = true;
            if (string.IsNullOrEmpty(text))
                return;
            if (!this.isWhitespace)
            {
                this.column++;
                this.writer.Write(' ');
            }
            this.isWhitespace = false;
            this.isIndentation = false;
            var spaces = false;
            var breaks = false;
            var start = 0;
            var end = 0;
            while (end <= text.Length)
            {
                char? ch = null;
                if (end < text.Length)
                    ch = text[end];
                if (spaces)
                {
                    if (ch != ' ')
                    {
                        if (start + 1 == end && this.column > this.bestWidth && split)
                        {
                            this.WriteIndent();
                            this.isWhitespace = false;
                            this.isIndentation = false;
                        }
                        else
                        {
                            var data = text.Substring(start, end - start);
                            this.column += end - start;
                            this.writer.Write(data);
                        }
                        start = end;
                    }
                }
                else if (breaks)
                {
                    if (ch == null || "\n\x85\u2028\u2029".IndexOf((char)ch) == -1)
                    {
                        if (text[start] == '\n')
                            this.WriteLineBreak();
                        for (var i = start; i <= end; i++)
                        {
                            var br = text[i];
                            if (br == '\n')
                                this.WriteLineBreak();
                            else
                                this.WriteLineBreak(br.ToString());
                        }
                        this.WriteIndent();
                        this.isWhitespace = false;
                        this.isIndentation = false;
                        start = end;
                    }
                }
                else if (ch == null || " \n\x85\u2028\u2029".IndexOf((char)ch) != -1)
                {
                    var data = text.Substring(start, end - start);
                    this.column += end - start;
                    this.writer.Write(data);
                    start = end;
                }
                if (ch != null)
                {
                    breaks = "\n\x85\u2028\u2029".IndexOf((char)ch) != -1;
                    spaces = ch == ' ';
                }
                end++;
            }
        }
    }
}
