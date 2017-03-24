using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticTemplate
{
    public class ClassTemplateGroup : IEnumerable<ClassTemplate>
    {
        public IEnumerable<ClassTemplate> Templates { get; }
        public ClassTemplate PrimaryTemplate { get; }

        public int TypeParamCount { get { return PrimaryTemplate.TypeParamCount; } }
        public string TemplateName { get { return PrimaryTemplate.TemplateName; } }
        public int MaxParamCount { get { return PrimaryTemplate.TypeParamCount; } }

        public ClassTemplateGroup(IEnumerable<ClassTemplate> templates)
        {
            Templates = templates.ToList();  // we store a copy
            PrimaryTemplate = Templates.Where(t => !t.IsSpecialized)
                                       .Aggregate((l, r) => l.RemainingParamCount > r.RemainingParamCount ? l : r);
            // TODO(leasunhy): add checking for conflicts among templates (e.g. same specialization on the same type param)
            // TODO(leasunhy): add checking for type parameter identifiers (force same names)
        }

        public IEnumerator<ClassTemplate> GetEnumerator() { return Templates.GetEnumerator(); }

        public ClassTemplate FindTemplateForArguments(IEnumerable<TypeSyntax> typeArgs)
        {
            if (typeArgs.Count() != TypeParamCount)
                throw new InvalidOperationException("Incorrect number of type arguements for template" + TemplateName);

            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() { return Templates.GetEnumerator(); }
    }
}
