using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StaticTemplate
{
    internal class TemplateExtractRewriter : CSharpSyntaxRewriter
    {
        private SemanticModel SemanticModel;
        private List<ClassTemplate> classTemplates = new List<ClassTemplate>();
        internal IEnumerable<ClassTemplate> ClassTemplates => classTemplates;

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // determine whether there is [StaticTemplate] attribute
            var stAttrList = node.AttributeLists.Select(
                (lst, li) => lst.DescendantNodes()
                                .OfType<AttributeSyntax>()
                                .Where(n => n.Name.ToString() == "StaticTemplate")).ToList();
            if (stAttrList.All(lst => lst.Count() == 0))
                return node;

            // remove those attributes
            var newAttrLists = new SyntaxList<AttributeListSyntax>();
            for (int i = 0; i < node.AttributeLists.Count; ++i)
            {
                var newAttrListSyntax = node.AttributeLists[i].RemoveNodes(stAttrList[i],
                                            SyntaxRemoveOptions.KeepExteriorTrivia);
                newAttrLists.Add(newAttrListSyntax);
            }

            node = node.WithAttributeLists(newAttrLists);
            var template = new ClassTemplate(node);
            classTemplates.Add(template);

            return null;
        }

        // TODO(leasunhy): remove this method in favor of one-rewriter-for-one-syntax-tree approach
        public SyntaxTree ExtractTemplatesFor(SemanticModel semanticModel, SyntaxTree syntaxTree)
        {
            SemanticModel = semanticModel;
            return Visit(syntaxTree.GetRoot()).SyntaxTree;
        }
    }
}