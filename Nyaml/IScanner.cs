namespace Nyaml
{
    public interface IScanner
    {
        bool CheckToken();

        bool CheckToken<T>() where T : Tokens.Base;

        bool CheckToken<T1, T2>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base;

        bool CheckToken<T1, T2, T3>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base;

        bool CheckToken<T1, T2, T3, T4>()
            where T1 : Tokens.Base
            where T2 : Tokens.Base
            where T3 : Tokens.Base
            where T4 : Tokens.Base;

        Tokens.Base PeekToken();

        Tokens.Base GetToken();

        T GetToken<T>() where T : Tokens.Base;
    }
}