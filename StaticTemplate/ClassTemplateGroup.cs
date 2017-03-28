using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace StaticTemplate
{
    public class ClassTemplateGroup : IEnumerable<ClassTemplate>
    {
        public IEnumerable<ClassTemplate> Templates { get; }
        public ClassTemplate PrimaryTemplate { get; }

        public int TypeParamCount => PrimaryTemplate.TypeParamCount;
        public string TemplateName => PrimaryTemplate.TemplateName;

        public ClassTemplateGroup(IEnumerable<ClassTemplate> templates)
        {
            // TODO(leasunhy): add checking for conflicts among templates
            //                 (e.g. no same specialization on the same type param,
            //                 all templates should have equal number of type params, etc.)
            // TODO(leasunhy): add checking for type parameter identifiers (force same names)

            if (templates == null)
                throw new ArgumentException("Argument cannot be null.", nameof(templates));

            Templates = templates.ToList();

            var templateName = Templates.First().TemplateName;
            if (Templates.Any(t => t.TemplateName != templateName))
                throw new ArgumentException("Templates must have same name.", nameof(templates));

            if (!Templates.Any())
                throw new ArgumentException("Argument cannot be empty.", nameof(templates));

            PrimaryTemplate = Templates.Single(t => !t.IsSpecialized);
        }

        public IEnumerator<ClassTemplate> GetEnumerator() { return Templates.GetEnumerator(); }

        // TODO(leasunhy): make this method use full qualified name of type args
        public string GetInstantiationNameFor(IEnumerable<INamedTypeSymbol> typeArgs) =>
            $"{TemplateName}#{string.Join(":", typeArgs.Select(a => a.ToDisplayString()))}#";

        public void Instantiate(IEnumerable<INamedTypeSymbol> typeArgs)
        {
            var namedTypeSymbols = typeArgs as INamedTypeSymbol[] ?? typeArgs.ToArray();
            FindTemplateForArguments(namedTypeSymbols).Instantiate(GetInstantiationNameFor(namedTypeSymbols), namedTypeSymbols);
        }

        public ClassTemplate FindTemplateForArguments(IEnumerable<INamedTypeSymbol> typeArgs)
        {
            var namedTypeSymbols = typeArgs as INamedTypeSymbol[] ?? typeArgs.ToArray();
            if (namedTypeSymbols.Length != TypeParamCount)
                throw new InvalidOperationException("Incorrect number of type arguements for template" + TemplateName);

            var matched = Templates.Select(temp => Tuple.Create(temp, MatchTypeArgs(temp, namedTypeSymbols)))
                                                        .Where(t => t.Item2.Item1);
            var highestRanked = matched.MaxBy(t => t.Item2.Item2).Item2
                                       .Select(t => t.Item1).ToList();
            if (highestRanked.Count == 1)
                return highestRanked.Single();

            // if there are more than one candidates, try determine which one is the most specialized (accepts fewest args)
            var hasFewestParams = highestRanked.MinBy(t => t.RemainingParamCount).Item2.ToList();
            if (hasFewestParams.Count == 1)
                return hasFewestParams.Single();

            throw new Exception("Ambiguous partial specializations for " + GetInstantiationNameFor(namedTypeSymbols));
        }

        private static Tuple<bool, int> MatchTypeArgs(ClassTemplate template, IEnumerable<INamedTypeSymbol> typeArgs)
        {
            // TODO(leasunhy): use semantic model?
            var success = true;
            var matched = 0;
            template.SpecialiedTypeArgList.ZipWith(typeArgs, (a1, a2) =>
            {
                success = success && (a1 == null || a1.ToDisplayString() == a2.ToDisplayString());
                if (!success) return;
                matched += a1 != null && a1.ToDisplayString() == a2.ToDisplayString() ? 1 : 0;
            });
            return Tuple.Create(success, matched);
        }

        IEnumerator IEnumerable.GetEnumerator() { return Templates.GetEnumerator(); }
    }
}
