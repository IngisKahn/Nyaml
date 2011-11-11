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
            using (var loader = new Canonical.Loader(data))
                return Scan(loader);
        }

        public static IEnumerable<Tokens.Base> CanonicalScan(Stream stream)
        {
            using (var loader = new Canonical.Loader(stream))
                return Scan(loader);
        }

        public static IEnumerable<Events.Base> CanonicalParse(string data)
        {
            using (var loader = new Canonical.Loader(data))
                return Parse(loader);
        }

        public static IEnumerable<Events.Base> CanonicalParse(Stream stream)
        {
            using (var loader = new Canonical.Loader(stream))
                return Parse(loader);
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
            using (var loader = new Canonical.Loader(data))
                return ComposeAll(loader);
        }

        public static IEnumerable<Nodes.Base> CanonicalComposeAll(Stream stream)
        {
            using (var loader = new Canonical.Loader(stream))
                return ComposeAll(loader);
        }
    }
}

