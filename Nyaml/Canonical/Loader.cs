namespace Nyaml
{
    using System.Collections.Generic;
    using System.IO;

    namespace Canonical
    {
        using Schemas;

        public class Loader : Nyaml.Loader
        {
            public Loader(string data)
                : this(new Scanner(data))
            {
            }

            public Loader(Stream stream)
                : this(new Scanner(new StreamReader(stream).ReadToEnd()))
            {
            }

            private Loader(IScanner scanner)
                : base(null, scanner, new Parser(scanner), schema: new Json())
            {
            }
        }
    }

    public static partial class Yaml
    {
        public static IEnumerable<Tokens.Base> CanonicalScan(string data)
        {
            return Scan(new Canonical.Loader(data), true);
        }

        public static IEnumerable<Tokens.Base> CanonicalScan(Stream stream)
        {
            return Scan(new Canonical.Loader(stream), true);
        }

        public static IEnumerable<Events.Base> CanonicalParse(string data)
        {
            return Parse(new Canonical.Loader(data), true);
        }

        public static IEnumerable<Events.Base> CanonicalParse(Stream stream)
        {
            return Parse(new Canonical.Loader(stream), true);
        }

        public static Nodes.Base CanonicalCompose(string data)
        {
            using (var loader = new Canonical.Loader(data))
                return Compose(loader);
        }

        public static Nodes.Base CanonicalCompose(Stream stream)
        {
            using (var loader = new Canonical.Loader(stream))
                return Compose(loader);
        }

        public static IEnumerable<Nodes.Base> CanonicalComposeAll(string data)
        {
            return ComposeAll(new Canonical.Loader(data), true);
        }

        public static IEnumerable<Nodes.Base> CanonicalComposeAll(Stream stream)
        {
            return ComposeAll(new Canonical.Loader(stream), true);
        }

        public static object CanonicalLoad(string data)
        {
            using (var loader = new Canonical.Loader(data))
                return Load(loader);
        }

        public static object CanonicalLoad(Stream stream)
        {
            using (var loader = new Canonical.Loader(stream))
                return Load(loader);
        }

        public static IEnumerable<object> CanonicalLoadAll(string data)
        {
            return LoadAll(new Canonical.Loader(data), true);
        }

        public static IEnumerable<object> CanonicalLoadAll(Stream stream)
        {
            return LoadAll(new Canonical.Loader(stream), true);
        }
    }
}

