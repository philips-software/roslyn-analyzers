// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.SomeHelp;
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
			: base(DiagnosticId.UnmanagedObjectsNeedDisposing, Title, MessageFormat, Description, Categories.RuntimeFailure, isEnabled: false)
		{ }
	}

	public class UnmanagedObjectsNeedDisposingSyntaxNodeAction : SyntaxNodeAction<FieldDeclarationSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			BaseTypeDeclarationSyntax typeDeclaration = Node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
			if (typeDeclaration is StructDeclarationSyntax)
			{
				return Option<Diagnostic>.None;
			}

			if (IsUnmanaged(Node.Declaration.Type) && !TypeImplementsIDisposable())
			{
				var variableName = Node.Declaration.Variables[0].Identifier.Text;
				Location loc = Node.Declaration.Variables[0].Identifier.GetLocation();
				return PrepareDiagnostic(loc, variableName).ToSome();
			}
			return Option<Diagnostic>.None;
		}

		private bool IsUnmanaged(TypeSyntax type)
		{
			var typeStr = type.ToString();
			var isIntPtr = typeStr.Contains("IntPtr");
			var isPointer = typeStr.Contains('*');
			var isHandle = typeStr.ToLowerInvariant().Contains("handle");
			return isIntPtr || isPointer || isHandle;
		}

		private bool TypeImplementsIDisposable()
		{
			const string IDisposableLiteral = "IDisposable";
			BaseTypeDeclarationSyntax type = Node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
			if (type == null)
			{
				return false;
			}
			INamedTypeSymbol typeSymbol = Context.SemanticModel.GetDeclaredSymbol(type);
			if (typeSymbol == null)
			{
				return false;
			}
			return typeSymbol.BaseType.Name == IDisposableLiteral || typeSymbol.AllInterfaces.Any(face => face.Name == IDisposableLiteral);
		}
	}
}
