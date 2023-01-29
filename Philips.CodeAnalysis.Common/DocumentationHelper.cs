// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Philips.CodeAnalysis.Common
{
	public class DocumentationHelper
	{
		private readonly List<XmlElementSyntax> xmlElements;
	
		public static SyntaxNode FindAncestorThatCanHaveDocumentation(SyntaxNode node)
		{
			return node.AncestorsAndSelf().FirstOrDefault(ancestor => ancestor is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or TypeDeclarationSyntax);
		}

		public DocumentationHelper(SyntaxNode node)
		{
			SyntaxTrivia doc = node.GetLeadingTrivia().FirstOrDefault(IsCommentTrivia);
			if(doc == default)
			{
				if (node is MethodDeclarationSyntax method)
				{
					doc = method.Modifiers[0].LeadingTrivia.FirstOrDefault(IsCommentTrivia);
				}
				else if (node is PropertyDeclarationSyntax prop)
				{
					doc = prop.Modifiers[0].LeadingTrivia.FirstOrDefault(IsCommentTrivia);
				}
				else if(node is TypeDeclarationSyntax type)
				{
					doc = type.Modifiers[0].LeadingTrivia.FirstOrDefault(IsCommentTrivia);
				}
			}
			if (doc == default)
			{
				xmlElements = new List<XmlElementSyntax>();
				ExistingDocumentation = null;
			}
			else
			{
				ExistingDocumentation = doc.GetStructure() as DocumentationCommentTriviaSyntax;
				if (ExistingDocumentation != null)
				{
					xmlElements = ExistingDocumentation.ChildNodes().OfType<XmlElementSyntax>().ToList();
				}
			}
		}

		public DocumentationCommentTriviaSyntax ExistingDocumentation { get; private set; }

		public void AddException(string exceptionTypeName, string description)
		{
			var exceptionType = SyntaxFactory.ParseTypeName(exceptionTypeName);
			var cref = SyntaxFactory.TypeCref(exceptionType);
			var crefAttribute = SyntaxFactory.XmlCrefAttribute(cref);
			var attributesList = new SyntaxList<XmlAttributeSyntax>();
			attributesList = attributesList.Add(crefAttribute);
			var exceptionXmlName = SyntaxFactory.XmlName("exception");
			var exceptionStart = SyntaxFactory.XmlElementStartTag(exceptionXmlName, attributesList);
			var exceptionEnd = SyntaxFactory.XmlElementEndTag(exceptionXmlName);
			var xmlException = SyntaxFactory.XmlElement(exceptionStart, exceptionEnd);
			xmlElements.Add(xmlException);
		}

		public IEnumerable<string> GetExceptionCrefs()
		{
			return xmlElements.Where(IsExceptionElement).Select(GetCrefAttributeValue);
		}

		public DocumentationCommentTriviaSyntax CreateDocumentation()
		{
			ExistingDocumentation ??= SyntaxFactory.DocumentationComment();
			var startOfLine = SyntaxFactory.XmlText("/// ");
			var endOfLine = SyntaxFactory.XmlText("\r\n");
			var comment = ExistingDocumentation;
			var content = new List<XmlNodeSyntax>();
			foreach(var xmlElement in xmlElements)
			{
				content.Add(startOfLine);
				content.Add(xmlElement);
				content.Add(endOfLine);
			}
			var contentSyntax = new SyntaxList<XmlNodeSyntax>(content);
			return comment.WithContent(contentSyntax);
		}

		private static bool IsCommentTrivia(SyntaxTrivia trivia)
		{
			return trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia);
		}
		private static bool IsExceptionElement(XmlElementSyntax element)
		{
			return element.StartTag.Name.LocalName.Text == "exception";
		}

		private static string GetCrefAttributeValue(XmlElementSyntax element)
		{
			return element.StartTag.Attributes.OfType<XmlCrefAttributeSyntax>().Select(cref => cref.Cref.ToString()).FirstOrDefault();
		}
	}
}
