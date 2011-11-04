namespace Nyaml.Events
{
    using System;
    using System.Collections.Generic;

    public class DocumentStart : Base
    {
        public bool IsExplicit { get; set; }
        public Tuple<string, string> Version { get; set; }
        public IDictionary<string, string> Tags { get; set; }
    }
}