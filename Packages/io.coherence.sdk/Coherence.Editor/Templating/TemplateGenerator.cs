// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Templating
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;

    internal class TemplateGenerator
    {
        private readonly string templatePath;
        private readonly string destinationPath;
        private readonly string className;
        private readonly string defaultName;

        public string ClassName => className;

        public TemplateGenerator(string templatePath, string destinationPath, string defaultName)
        {
            this.templatePath = templatePath;
            this.destinationPath = destinationPath;
            this.defaultName = defaultName;

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(destinationPath);
            this.className = new string(nameWithoutExtension.Where(char.IsLetter).ToArray());
        }

        public void Generate()
        {
            File.Copy(templatePath, destinationPath);

            var text = File.ReadAllText(destinationPath);
            File.WriteAllText(destinationPath, text.Replace(defaultName, className));

            AssetDatabase.ImportAsset(destinationPath);
        }
    }
}
