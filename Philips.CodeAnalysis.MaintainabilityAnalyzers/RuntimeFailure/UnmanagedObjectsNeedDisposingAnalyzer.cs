// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.RuntimeFailure
{
	/// <summary>
	/// Report on type with unmanaged fields that is not <see cref="IDisposable"/>.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnmanagedObjectsNeedDisposingAnalyzer : SingleDiagnosticAnalyzer<FieldDeclarationSyntax, UnmanagedObjectsNeedDisposingSyntaxNodeAction>
	{
		private const string Title = "Unmanaged object need disposing";
		private const string MessageFormat = "The field {0} is refering to an unmanaged object and needs to be declared in a class that implements IDisposable.";
		private const string Description = "Every field which holds an unmanaged object needs to be declared in a class that implements IDisposable.";

		public UnmanagedObjectsNeedDisposingAnalyzer()
			: base(DiagnosticId.UnmanagedObjectsNeedDisposing, Title, MessageFormat, Description, Categories.RuntimeFailure)
		{ }
	}

	public class UnmanagedObjectsNeedDisposingSyntaxNodeAction : SyntaxNodeAction<FieldDeclarationSyntax>
	{
		public override void Analyze()
		{
			var typeDeclaration = Node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
			if (typeDeclaration is StructDeclarationSyntax)
			{
				return;
			}

			if (IsUnmanaged(Node.Declaration.Type) && !TypeImplementsIDisposable())
			{
				var variableName = Node.Declaration.Variables[0].Identifier.Text;
				var loc = Node.Declaration.Variables[0].Identifier.GetLocation();
				ReportDiagnostic(loc, variableName);
			}
		}

		private bool IsUnmanaged(TypeSyntax type)
		{
			var typeStr = type.ToString();
			bool isIntPtr = typeStr.Contains("IntPtr");
			bool isPointer = typeStr.Contains('*');
			bool isHandle = typeStr.ToLowerInvariant().Contains("handle");
			return isIntPtr || isPointer || isHandle;
		}

		private bool TypeImplementsIDisposable()
		{
			const string IDisposableLiteral = "IDisposable";
			var type = Node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
			if (type == null)
			{
				return false;
			}
			var typeSymbol = Context.SemanticModel.GetDeclaredSymbol(type);
			if (typeSymbol == null)
			{
				return false;
			}
			return typeSymbol.BaseType.Name == IDisposableLiteral || typeSymbol.AllInterfaces.Any(face => face.Name == IDisposableLiteral);
		}
	}
}
