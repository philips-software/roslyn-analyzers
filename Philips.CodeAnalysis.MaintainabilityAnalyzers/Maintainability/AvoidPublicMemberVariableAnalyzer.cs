// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using LanguageExt.SomeHelp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidPublicMemberVariableAnalyzer : SingleDiagnosticAnalyzer<FieldDeclarationSyntax, AvoidPublicMemberVariableSyntaxNodeAction>
	{
		private const string Title = @"Avoid public fields declaration";
		public const string MessageFormat = @"Avoid public instance fields in a class. Use property instead";
		private const string Description = @"Avoid public  fields in a class. Declare public property if needed for static fields";

		public AvoidPublicMemberVariableAnalyzer()
			: base(DiagnosticId.AvoidPublicMemberVariables, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}
	public class AvoidPublicMemberVariableSyntaxNodeAction : SyntaxNodeAction<FieldDeclarationSyntax>
	{
		public override IEnumerable<Diagnostic> Analyze()
		{
			if (Node.Parent.Kind() == SyntaxKind.StructDeclaration)
			{
				return Option<Diagnostic>.None;
			}

			if (Node.Modifiers.Any(SyntaxKind.PublicKeyword))
			{
				if (Node.Modifiers.Any(SyntaxKind.ConstKeyword))
				{
					return Option<Diagnostic>.None;
				}

				if (Node.Modifiers.Any(SyntaxKind.StaticKeyword))
				{
					return Option<Diagnostic>.None;
				}

				Location location = Node.GetLocation();
				return PrepareDiagnostic(location).ToSome();
			}
			return Option<Diagnostic>.None;
		}
	}
}
