namespace Nyaml
{
    public interface ILoader : IReader, IScanner, IParser, IComposer, IConstructor
    { }

    public class Loader : ILoader
    {
        private readonly IReader reader;
        private readonly IScanner scanner;
        private readonly IParser parser;
        private readonly IComposer composer;
        private readonly IConstructor constructor;
        private readonly Schemas.Base schema;

        public Loader(IReader reader, IScanner scanner = null, IParser parser = null,
            IComposer composer = null, IConstructor constructor = null,
            Schemas.Base schema = null)
        {
            this.schema = schema ?? new Schemas.Full();
            this.reader = reader;
            this.scanner = scanner ?? new Scanner(reader);
            this.parser = parser ?? new Parser(this.scanner);
            this.composer = composer ?? new Composer(this.parser, this.schema);
            this.constructor = constructor ?? new Constructor(this.composer);
        }

        public Loader(string data, Schemas.Base schema = null)
            : this(new Reader(data), schema: schema)
        { }

        public Loader(byte[] data, Schemas.Base schema = null)
            : this(new Reader(data), schema: schema)
        { }

        public Loader(System.IO.Stream stream, Schemas.Base schema = null)
            : this(new Reader(stream), schema: schema)
        { }

        public char Peek(int index = 0)
        {
            return this.reader.Peek(index);
        }

        public string Prefix(int length = 1)
        {
            return this.reader.Prefix(length);
        }

        public void Forward(int length = 1)
        {
            this.reader.Forward(length);
        }

        public Mark Mark
        {
            get { return this.reader.Mark; }
        }

        public int Index
        {
            get { return this.reader.Index; }
        }

        public int Line
        {
            get { return this.reader.Line; }
        }

        public int Column
        {
            get { return this.reader.Column; }
        }

        public System.Text.Encoding Encoding
        {
            get { return this.reader.Encoding; }
        }

        public void Dispose()
        {
            if (this.reader != null)
                this.reader.Dispose();
        }

        public bool CheckToken()
        {
            return this.scanner.CheckToken();
        }

        public bool CheckToken<T>() where T : Tokens.Base
        {
            return this.scanner.CheckToken<T>();
        }

        public bool CheckToken<T1, T2>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
        {
            return this.scanner.CheckToken<T1, T2>();
        }

        public bool CheckToken<T1, T2, T3>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
        {
            return this.scanner.CheckToken<T1, T2, T3>();
        }

        public bool CheckToken<T1, T2, T3, T4>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
            where T4 : Tokens.Base
        {
            return this.scanner.CheckToken<T1, T2, T3, T4>();
        }

        public Tokens.Base PeekToken()
        {
            return this.scanner.PeekToken();
        }

        public Tokens.Base GetToken()
        {
            return this.scanner.GetToken();
        }

        public T GetToken<T>() where T : Tokens.Base
        {
            return this.scanner.GetToken<T>();
        }

        public void Reset()
        {
            this.parser.Reset();
        }

        public bool CheckEvent()
        {
            return this.parser.CheckEvent();
        }

        public bool CheckEvent<T>() where T : Events.Base
        {
            return this.parser.CheckEvent<T>();
        }

        public bool CheckEvent<T1, T2, T3, T4>()
            where T1 : Events.Base
            where T2 : Events.Base
            where T3 : Events.Base
            where T4 : Events.Base
        {
            return this.parser.CheckEvent<T1, T2, T3, T4>();
        }

        public Events.Base PeekEvent()
        {
            return this.parser.PeekEvent();
        }

        public Events.Base GetEvent()
        {
            return this.parser.GetEvent();
        }

        public bool CheckNode()
        {
            return this.composer.CheckNode();
        }

        public Nodes.Base GetNode()
        {
            return this.composer.GetNode();
        }

        public Nodes.Base GetSingleNode()
        {
            return this.composer.GetSingleNode();
        }

        public bool CheckData()
        {
            return this.constructor.CheckData();
        }

        public object GetData()
        {
            return this.constructor.GetData();
        }

        public object GetSingleData()
        {
            return this.constructor.GetSingleData();
        }
    }
}
