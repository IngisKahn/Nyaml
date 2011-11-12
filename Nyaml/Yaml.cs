namespace Nyaml
{
    using System.Collections.Generic;
    using System.IO;

    public static partial class Yaml
    {
        public static IEnumerable<Tokens.Base> Scan(string data)
        {
            using (var loader = new Loader(new Reader(data)))
                return Scan(loader);
        }

        public static IEnumerable<Tokens.Base> Scan(byte[] data)
        {
            using (var loader = new Loader(new Reader(data)))
                return Scan(loader);
        }

        public static IEnumerable<Tokens.Base> Scan(Stream stream)
        {
            using (var loader = new Loader(new Reader(stream)))
                return Scan(loader);
        }

        public static IEnumerable<Tokens.Base> Scan(ILoader loader)
        {
            while (loader.CheckToken())
                yield return loader.GetToken();
        }

        public static IEnumerable<Events.Base> Parse(string data)
        {
            using (var loader = new Loader(new Reader(data)))
                return Parse(loader);
        }

        public static IEnumerable<Events.Base> Parse(byte[] data)
        {
            using (var loader = new Loader(new Reader(data)))
                return Parse(loader);
        }

        public static IEnumerable<Events.Base> Parse(Stream stream)
        {
            using (var loader = new Loader(new Reader(stream)))
                return Parse(loader);
        }

        public static IEnumerable<Events.Base> Parse(ILoader loader)
        {
            while (loader.CheckEvent())
                yield return loader.GetEvent();
        }

        public static Nodes.Base Compose(string data)
        {
            using (var loader = new Loader(new Reader(data)))
                return Compose(loader);
        }

        public static Nodes.Base Compose(byte[] data)
        {
            using (var loader = new Loader(new Reader(data)))
                return Compose(loader);
        }

        public static Nodes.Base Compose(Stream stream)
        {
            using (var loader = new Loader(new Reader(stream)))
                return Compose(loader);
        }

        public static Nodes.Base Compose(ILoader loader)
        {
            return loader.GetSingleNode();
        }

        public static IEnumerable<Nodes.Base> ComposeAll(string data)
        {
            using (var loader = new Loader(new Reader(data)))
                return ComposeAll(loader);
        }

        public static IEnumerable<Nodes.Base> ComposeAll(byte[] data)
        {
            using (var loader = new Loader(new Reader(data)))
                return ComposeAll(loader);
        }

        public static IEnumerable<Nodes.Base> ComposeAll(Stream stream)
        {
            using (var loader = new Loader(new Reader(stream)))
                return ComposeAll(loader);
        }

        public static IEnumerable<Nodes.Base> ComposeAll(ILoader loader)
        {
            while (loader.CheckNode())
                yield return loader.GetNode();
        }

        public static object Load(string data)
        {
            using (var loader = new Loader(new Reader(data)))
                return Load(loader);
        }

        public static object Load(byte[] data)
        {
            using (var loader = new Loader(new Reader(data)))
                return Load(loader);
        }

        public static object Load(Stream stream)
        {
            using (var loader = new Loader(new Reader(stream)))
                return Load(loader);
        }

        public static object Load(ILoader loader)
        {
            return loader.GetSingleData();
        }

        public static object LoadAll(string data)
        {
            using (var loader = new Loader(new Reader(data)))
                return Load(loader);
        }

        public static object LoadAll(byte[] data)
        {
            using (var loader = new Loader(new Reader(data)))
                return LoadAll(loader);
        }

        public static object LoadAll(Stream stream)
        {
            using (var loader = new Loader(new Reader(stream)))
                return LoadAll(loader);
        }

        public static object LoadAll(ILoader loader)
        {
            return loader.GetData();
        }
    }
}
