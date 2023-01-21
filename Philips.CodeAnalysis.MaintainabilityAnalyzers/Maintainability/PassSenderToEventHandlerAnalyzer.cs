// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PassSenderToEventHandlerAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Pass sender to event handler";
		private const string MessageFormat = @"Pass the sender to the EventHandler for {0}";
		private const string Description = @"Prevent passing null values for sender/object to event handler (for instance-based events).";
		private const string Category = Categories.Maintainability;

		private static readonly DiagnosticDescriptor Rule = new(Helper.ToDiagnosticId(DiagnosticIds.PassSenderToEventHandler),
			Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.EventFieldDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var eventField = (EventFieldDeclarationSyntax)context.Node;

			var variable = eventField.Declaration.Variables.FirstOrDefault();
			if (variable == null)
			{
				return;
			}

			var eventName = variable.Identifier.Text;

			var parent = eventField.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
			if (parent == null)
			{
				// Should never happen, field must be declared inside a type declaration.
				return;
			}

			AnalyzeArguments(context, parent, eventName);
		}

		private static void AnalyzeArguments(SyntaxNodeAnalysisContext context, TypeDeclarationSyntax parent, string eventName)
		{
			// EventHandlers must have 2 arguments as checked by CA1003, assume this rule is obeyed here.
			var invocations = parent.DescendantNodes()
				.OfType<InvocationExpressionSyntax>()
				.Where(invocation => IsOurEvent(invocation, eventName))
				.Where(i => i.ArgumentList.Arguments.Count == 2);

			foreach (var invocation in invocations)
			{
				var arguments = invocation.ArgumentList.Arguments;
				if (IsLiteralNull(arguments[0]))
				{
					var loc = arguments[0].GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, loc, eventName));
				}

				if (IsLiteralNull(arguments[1]))
				{
					var loc = arguments[1].GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(Rule, loc, eventName));
				}
			}
		}

		private static bool IsOurEvent(InvocationExpressionSyntax invocation, string eventName)
		{
			return invocation.Expression is IdentifierNameSyntax name && name.Identifier.Text == eventName;
		}

		private static bool IsLiteralNull(ArgumentSyntax argument)
		{
			return argument.Expression is LiteralExpressionSyntax { Token.Text: "null" };
		}
	}
}
