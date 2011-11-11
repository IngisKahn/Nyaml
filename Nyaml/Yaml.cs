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
    }
}
