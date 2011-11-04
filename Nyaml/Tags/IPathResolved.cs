namespace Nyaml.Tags
{
    using System.Collections.Generic;

    // TODO: Implement according to YPath spec
    public interface IPathResolved
    {
        Kind Kind { get; }
        IEnumerable<IPathElement> Path { get; }
    }

    public interface IPathElement
    {
        Kind Kind { get; }
    }
}
