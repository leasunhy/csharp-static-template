﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        public ClassTemplateGroupBuilder(IEnumerable<ClassTemplate> templates)
        {
            if (templates == null)
                throw new ArgumentException("Argument cannot be null.", "templates");

            TemplateName = templates.First().TemplateName;
            if (!templates.All(t => t.TemplateName == TemplateName))
                throw new ArgumentException("Templates must have same name.", "templates");

            this.templates = templates.ToList();
            if (!this.templates.Any())
                throw new ArgumentException("Argument cannot be empty.", "templates");
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

        public ClassTemplateGroup Build()
        {
            // TODO(leasunhy): add checking for conflicts among templates
            //                 (e.g. no same specialization on the same type param,
            //                 all templates should have equal number of type params, etc.)
            // TODO(leasunhy): add checking for type parameter identifiers (force same names)
            return new ClassTemplateGroup(templates);
        }

        public IEnumerator<ClassTemplate> GetEnumerator() { return templates.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return templates.GetEnumerator(); }
    }
}
