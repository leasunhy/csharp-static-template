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

            var best = Templates.Select(temp => Tuple.Create(temp, TypeArgMatch(temp, typeArgs)))
                                .Where(t => t.Item2.Item1)
                                .OrderByDescending(t => t.Item2.Item2)
                                .First().Item1;
            return best;
        }

        private Tuple<bool, int> TypeArgMatch(ClassTemplate template, IEnumerable<TypeSyntax> typeArgs)
        {
            // TODO(leasunhy): use semantic model?
            var success = true;
            var matched = 0;
            foreach (var t in template.SpecialiedTypeArgList.Zip(typeArgs, (a1, a2) => Tuple.Create(a1, a2)))
            {
                success = t.Item1 == null || t.Item1.ToString() == t.Item2.ToString();
                if (!success) break;
                matched += (t.Item1 != null && t.Item1.ToString() == t.Item2.ToString()) ? 1 : 0;
            }
            return Tuple.Create(success, matched);
        }

        IEnumerator IEnumerable.GetEnumerator() { return Templates.GetEnumerator(); }
    }
}
