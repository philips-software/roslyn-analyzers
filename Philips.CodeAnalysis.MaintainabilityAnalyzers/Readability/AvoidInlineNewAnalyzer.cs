﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidInlineNewAnalyzer : SingleDiagnosticAnalyzer<ObjectCreationExpressionSyntax, AvoidInlineNewSyntaxNodeAction>
	{
		private const string Title = @"Do not inline new T() calls";
		private const string MessageFormat = @"Do not inline the constructor call for class {0}";
		private const string Description = @"Create a local variable, or a field for the temporary instance of class '{0}'";

		public AvoidInlineNewAnalyzer()
			: base(DiagnosticId.AvoidInlineNew, Title, MessageFormat, Description, Categories.Readability)
		{ }
	}

	public class AvoidInlineNewSyntaxNodeAction : SyntaxNodeAction<ObjectCreationExpressionSyntax>
	{
		private static readonly HashSet<string> AllowedMethods = new() { StringConstants.ToStringMethodName, StringConstants.ToListMethodName, StringConstants.ToArrayMethodName, "AsSpan" };

		public override void Analyze()
		{
			SyntaxNode parent = Node.Parent;

			if (!IsInlineNew(parent))
			{
				return;
			}

			if (IsCallingAllowedMethod(parent))
			{
				return;
			}

			var location = Node.GetLocation();
			ReportDiagnostic(location, Node.Type.ToString());
		}

		private static bool IsInlineNew(SyntaxNode node)
		{
			return
				node is MemberAccessExpressionSyntax ||
				(node is ParenthesizedExpressionSyntax syntax && IsInlineNew(syntax.Parent));
		}

		private static bool IsCallingAllowedMethod(SyntaxNode node)
		{
			if (node is ParenthesizedExpressionSyntax syntax)
			{
				return IsCallingAllowedMethod(syntax.Parent);
			}
			return
				node is MemberAccessExpressionSyntax memberAccess &&
				AllowedMethods.Contains(memberAccess.Name.Identifier.Text);
		}
	}
}
