// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.Common
{
	public class DocumentationHelper
	{
		private const string ExceptionElementName = "exception";
		private readonly List<XmlElementSyntax> _xmlElements = new();

		public static SyntaxNode FindAncestorThatCanHaveDocumentation(SyntaxNode node)
		{
			return node.AncestorsAndSelf().FirstOrDefault(ancestor => ancestor is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or TypeDeclarationSyntax);
		}

		public DocumentationHelper(SyntaxNode node)
		{
			SyntaxTrivia doc = node.GetLeadingTrivia().FirstOrDefault(IsCommentTrivia);
			if (doc == default)
			{
				if (node is MethodDeclarationSyntax method)
				{
					doc = method.Modifiers[0].LeadingTrivia.FirstOrDefault(IsCommentTrivia);
				}
				else if (node is PropertyDeclarationSyntax prop)
				{
					doc = prop.Modifiers[0].LeadingTrivia.FirstOrDefault(IsCommentTrivia);
				}
				else if (node is TypeDeclarationSyntax type)
				{
					doc = type.Modifiers[0].LeadingTrivia.FirstOrDefault(IsCommentTrivia);
				}
			}
			if (doc != default)
			{
				ExistingDocumentation = doc.GetStructure() as DocumentationCommentTriviaSyntax;
				if (ExistingDocumentation != null)
				{
					_xmlElements = ExistingDocumentation.ChildNodes().OfType<XmlElementSyntax>().ToList();
				}
			}
		}

		public DocumentationCommentTriviaSyntax ExistingDocumentation { get; private set; }

		public void AddException(string exceptionTypeName)
		{
			TypeSyntax exceptionType = SyntaxFactory.ParseTypeName(exceptionTypeName);
			TypeCrefSyntax cref = SyntaxFactory.TypeCref(exceptionType);
			XmlCrefAttributeSyntax crefAttribute = SyntaxFactory.XmlCrefAttribute(cref);
			var attributesList = new SyntaxList<XmlAttributeSyntax>();
			attributesList = attributesList.Add(crefAttribute);
			XmlNameSyntax exceptionXmlName = SyntaxFactory.XmlName(ExceptionElementName);
			XmlElementStartTagSyntax exceptionStart = SyntaxFactory.XmlElementStartTag(exceptionXmlName, attributesList);
			XmlElementEndTagSyntax exceptionEnd = SyntaxFactory.XmlElementEndTag(exceptionXmlName);
			XmlElementSyntax xmlException = SyntaxFactory.XmlElement(exceptionStart, exceptionEnd);
			_xmlElements.Add(xmlException);
		}

		public IEnumerable<string> GetExceptionCodeReferences()
		{
			return _xmlElements.Where(IsExceptionElement).Select(GetCrefAttributeValue);
		}

		public DocumentationCommentTriviaSyntax CreateDocumentation()
		{
			ExistingDocumentation ??= SyntaxFactory.DocumentationComment();
			XmlTextSyntax startOfLine = SyntaxFactory.XmlText("/// ");
			XmlTextSyntax endOfLine = SyntaxFactory.XmlText("\r\n");
			DocumentationCommentTriviaSyntax comment = ExistingDocumentation;
			var content = new List<XmlNodeSyntax>();
			foreach (XmlElementSyntax xmlElement in _xmlElements)
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
			return element.StartTag.Name.LocalName.Text == ExceptionElementName;
		}

		private static string GetCrefAttributeValue(XmlElementSyntax element)
		{
			return element.StartTag.Attributes.OfType<XmlCrefAttributeSyntax>().Select(cref => cref.Cref.ToString()).FirstOrDefault();
		}
	}
}
