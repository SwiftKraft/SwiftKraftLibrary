namespace Coherence.CodeGen
{
    using System.Collections.Generic;
    using Scriban.Runtime;

    public struct ScribanOptions
    {
        public string Namespace;
        public List<string> UsingDirectives;
        public List<string> TemplateNames;
        public List<ScriptObject> Model;
        public ITemplateLoader TemplateLoader;
    }
}
