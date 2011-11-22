namespace Nyaml
{
    using System;
    using System.Text;

    public interface IReader : IDisposable
    {
        char Peek(int index = 0);

        string Prefix(int length = 1);

        void Forward(int length = 1);

        Mark Mark { get; }

        int Index { get; }

        int Line { get; }

        int Column { get; }

        Encoding Encoding { get; }
    }
}