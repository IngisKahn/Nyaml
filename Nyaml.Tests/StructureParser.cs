namespace Nyaml.Tests
{
    using System;
    using System.Collections.Generic;

    // this isn't a real python parser of course, but it should
    // be enough for the structure files
    public static class StructureParser
    {
        public static object Parse(string data)
        {
            var pos = 0;
            return GetNextObject(data, ref pos);
        }

        private static object GetNextObject(string data, ref int pos)
        {
            SkipWhiteSpace(data, ref pos);
            switch (data[pos])
            {
                case '(':
                    return GetMap(data, ref pos);
                case '[':
                    return GetList(data, ref pos);
                case '\'':
                    return GetString(data, ref pos);
                case 'T':
                    pos += 4;
                    return true;
                case 'F':
                    pos += 5;
                    return false;
                case 'N':
                    pos += 4;
                    return null;
                default:
                    throw new InvalidOperationException(string.Format("Invalid structure item '{0}' at pos {1}", data[pos], pos));
            }
        }

        private static void SkipWhiteSpace(string data, ref int pos)
        {
            for (;;)
                switch (data[pos])
                {
                    case ' ':
                    case '\n':
                        pos++;
                        break;
                    default:
                        return;
                }
        }

        private static string GetString(string data, ref int pos)
        {
            var start = ++pos;
            while (data[pos++] != '\'') { }
            return data.Substring(start, pos - 1 - start);
        }

        private static EquatableList<object> GetList(string data, ref int pos)
        {
            pos++;
            SkipWhiteSpace(data, ref pos);
            var list = new List<object>();
            if (data[pos] != ']')
            {
                list.Add(GetNextObject(data, ref pos));
                SkipWhiteSpace(data, ref pos);
                while (data[pos++] == ',')
                {
                    SkipWhiteSpace(data, ref pos);
                    if (data[pos] == ']')
                    {
                        pos++;
                        break;
                    }
                    list.Add(GetNextObject(data, ref pos));
                    SkipWhiteSpace(data, ref pos);
                }
            }
            return new EquatableList<object>(list);
        }

        private static Tuple<object, object> GetMap(string data, ref int pos)
        {
            pos++;
            SkipWhiteSpace(data, ref pos);
            
            var key = GetNextObject(data, ref pos);
            SkipWhiteSpace(data, ref pos);
            pos++;
            var value = GetNextObject(data, ref pos);
            SkipWhiteSpace(data, ref pos);
            pos++;
            
            return new Tuple<object, object>(key, value);
        }
    }
}
