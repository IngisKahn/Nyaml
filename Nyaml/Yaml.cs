namespace Nyaml
{
    using System.Collections.Generic;
    using System.IO;

    public static class Yaml
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

        public static IEnumerable<Tokens.Base> Scan(Loader loader)
        {
            while (loader.CheckToken())
                yield return loader.GetToken();
        }
    }
}
