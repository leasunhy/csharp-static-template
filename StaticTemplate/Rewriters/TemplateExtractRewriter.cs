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
        private List<ClassTemplate> classTemplates = new List<ClassTemplate>();
        public List<ClassTemplate> ClassTemplates => classTemplates;

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (!ClassTemplate.IsClassTemplate(node))
                return node;
            classTemplates.Add(new ClassTemplate(node));
            return null;
        }
    }
}