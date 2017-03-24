using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticTemplate
{
    public class ClassTemplateGroupBuilder : IEnumerable<ClassTemplate>
    {
        private List<ClassTemplate> templates;

        public string TemplateName { get; private set; } = null;

        public ClassTemplateGroupBuilder()
        {
            templates = new List<ClassTemplate>();
        }

        public ClassTemplateGroupBuilder(params ClassTemplate[] templates)
        {
            if (templates == null || templates.Length == 0)
                throw new ArgumentException("Argument cannot be null or empty.", "templates");
            TemplateName = templates.First().TemplateName;
            if (!templates.All(t => t.TemplateName == TemplateName))
                throw new ArgumentException("Templates must have same name.", "templates");

            this.templates = templates.ToList();
        }

        public void AddTemplate(ClassTemplate template)
        {
            // if there is no templates in the group
            if (TemplateName == null)
                TemplateName = template.TemplateName;

            if (TemplateName != template.TemplateName)
                throw new InvalidOperationException("All templates in a TemplateGroup must have same name.");
            templates.Add(template);
        }

        public ClassTemplate FindTemplateForArguments(IEnumerable<TypeSyntax> typeArgs)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<ClassTemplate> GetEnumerator() { return templates.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return templates.GetEnumerator(); }
    }
}
