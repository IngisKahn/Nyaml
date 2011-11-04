namespace Nyaml
{
    using System;
    
    [Serializable]
    public class YamlError : Exception
    {
        public YamlError(string message) : base(message)
        {
        }
    }
}
