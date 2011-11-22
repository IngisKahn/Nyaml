namespace Nyaml
{
    public interface IConstructor
    {
        bool CheckData();

        object GetData();

        object GetSingleData();
    }
}