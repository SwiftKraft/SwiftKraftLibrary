namespace Coherence.CodeGen
{
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Scriban;
    using Scriban.Parsing;
    using Scriban.Runtime;

    public class TemplateLoaderFromDisk : ITemplateLoader
    {
        private const string scribanTemplatesPath = "Packages/io.coherence.sdk/Coherence.CodeGen/Scriban/Templates";

        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        {
            return Path.Combine(scribanTemplatesPath, $"{templateName}.sbncs");
        }

        public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return File.ReadAllText(templatePath, Encoding.UTF8);
        }

        public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            var text = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
            return text;
        }
    }
}
