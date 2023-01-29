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
		public string D { get; set; } = string.Empty;
	
		public static SyntaxNode FindAncestorThatCanHaveDocumentation(SyntaxNode node)
		{
			return node.AncestorsAndSelf().FirstOrDefault(ancestor => ancestor is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or TypeDeclarationSyntax);
		}

		public DocumentationHelper(SyntaxNode node)
		{
			SyntaxTrivia doc = node.GetLeadingTrivia().FirstOrDefault(IsCommentTrivia);
			if(doc == default)
			{
				D += "L26 ";
				if (node is MethodDeclarationSyntax method)
				{
					D += "L29 ";
					doc = method.Modifiers[0].LeadingTrivia.FirstOrDefault(IsCommentTrivia);
				}
				else if (node is PropertyDeclarationSyntax prop)
				{
					D += "L34 ";
					doc = prop.Modifiers[0].LeadingTrivia.FirstOrDefault(IsCommentTrivia);
				}
				else if(node is TypeDeclarationSyntax type)
				{
					D += "L39 ";
					doc = type.Modifiers[0].LeadingTrivia.FirstOrDefault(IsCommentTrivia);
				}
			}
			if (doc == default)
			{
				D += "L45 ";
				D += node.GetType().ToString();
				var stl = node.GetLeadingTrivia();
				D += stl.Count;

				xmlElements = new List<XmlElementSyntax>();
				ExistingDocumentation = null;
			}
			else
			{
				ExistingDocumentation = doc.GetStructure() as DocumentationCommentTriviaSyntax;
				xmlElements = ExistingDocumentation.ChildNodes().OfType<XmlElementSyntax>().ToList();
			}
		}

		public string GetNodeTypes()
		{
			var s = ExistingDocumentation == null ? "null" : string.Join(", ", ExistingDocumentation.ChildNodes().Select(sn => sn.GetType().ToString()));
			return s;
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

		public int GetXmlCount()
		{
			return xmlElements.Count;
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
			return trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia);
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
