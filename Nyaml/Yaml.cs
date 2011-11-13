namespace Nyaml
{
    using System.Collections.Generic;
    using System.IO;

    public static partial class Yaml
    {
        public static IEnumerable<Tokens.Base> Scan(string data)
        {
            return Scan(new Loader(new Reader(data)), true);
        }

        public static IEnumerable<Tokens.Base> Scan(byte[] data)
        {
            return Scan(new Loader(new Reader(data)), true);
        }

        public static IEnumerable<Tokens.Base> Scan(Stream stream)
        {
            return Scan(new Loader(new Reader(stream)), true);
        }

        internal static IEnumerable<Tokens.Base> Scan(ILoader loader, bool dispose)
        {
            while (loader.CheckToken())
                yield return loader.GetToken();
            if (dispose)
                loader.Dispose();
        }

        public static IEnumerable<Tokens.Base> Scan(ILoader loader)
        {
            return Scan(loader, false);
        }

        public static IEnumerable<Events.Base> Parse(string data)
        {
            return Parse(new Loader(new Reader(data)), true);
        }

        public static IEnumerable<Events.Base> Parse(byte[] data)
        {
            return Parse(new Loader(new Reader(data)), true);
        }

        public static IEnumerable<Events.Base> Parse(Stream stream)
        {
            return Parse(new Loader(new Reader(stream)), true);
        }

        internal static IEnumerable<Events.Base> Parse(ILoader loader, bool dispose)
        {
            while (loader.CheckEvent())
                yield return loader.GetEvent();
            if (dispose)
                loader.Dispose();
        }

        public static IEnumerable<Events.Base> Parse(ILoader loader)
        {
            return Parse(loader, false);
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
            return ComposeAll(new Loader(new Reader(data)));
        }

        public static IEnumerable<Nodes.Base> ComposeAll(byte[] data)
        {
            return ComposeAll(new Loader(new Reader(data)));
        }

        public static IEnumerable<Nodes.Base> ComposeAll(Stream stream)
        {
            return ComposeAll(new Loader(new Reader(stream)));
        }

        internal static IEnumerable<Nodes.Base> ComposeAll(ILoader loader, bool dispose)
        {
            while (loader.CheckNode())
                yield return loader.GetNode();
            if (dispose)
                loader.Dispose();
        }

        public static IEnumerable<Nodes.Base> ComposeAll(ILoader loader)
        {
            return ComposeAll(loader, false);
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

        public static IEnumerable<object> LoadAll(string data)
        {
            return LoadAll(new Loader(new Reader(data)), true);
        }

        public static IEnumerable<object> LoadAll(byte[] data)
        {
            return LoadAll(new Loader(new Reader(data)), true);
        }

        public static IEnumerable<object> LoadAll(Stream stream)
        {
            return LoadAll(new Loader(new Reader(stream)), true);
        }

        internal static IEnumerable<object> LoadAll(ILoader loader, bool dispose)
        {
            while (loader.CheckData())
                yield return loader.GetData();
        }

        public static IEnumerable<object> LoadAll(ILoader loader)
        {
            return LoadAll(loader, false);
        }
    }
}
