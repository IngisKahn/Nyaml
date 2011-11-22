namespace Nyaml
{
    public interface IComposer
    {
        bool CheckNode();

        Nodes.Base GetNode();

        Nodes.Base GetSingleNode();
    }
}