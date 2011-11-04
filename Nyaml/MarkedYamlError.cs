using System.Collections.Generic;

namespace Nyaml
{
    [System.Serializable]
    public class MarkedYamlError : YamlError
    {
        public MarkedYamlError(string context = null, Mark contextMark = null,
            string problem = null, Mark problemMark = null, string note = null) 
            : base(MakeMessage(context, contextMark, problem, problemMark, note))
        {
        }

        private static string MakeMessage(string context, Mark contextMark,
            string problem, Mark problemMark, string note)
        {
            var lines = new List<string>();
            if (context != null)
                lines.Add(context);
            if (contextMark != null 
                && (problem == null
                 || problemMark == null
                 || contextMark.Name != problemMark.Name
                 || contextMark.Line != problemMark.Line
                 || contextMark.Column != problemMark.Column))
                lines.Add(contextMark.ToString());
            if (problem != null)
                lines.Add(problem);
            if (problemMark != null)
                lines.Add(problemMark.ToString());
            if (note != null)
                lines.Add(note);
            return string.Join("\n", lines);
        }
    }
}
