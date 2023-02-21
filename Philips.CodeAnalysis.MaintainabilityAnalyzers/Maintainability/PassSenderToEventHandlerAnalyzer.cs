// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PassSenderToEventHandlerAnalyzer : SingleDiagnosticAnalyzer<EventFieldDeclarationSyntax, PassSenderToEventHandlerSyntaxNodeAction>
	{
		private const string Title = @"Pass sender to event handler";
		private const string MessageFormat = @"Pass the sender to the EventHandler for {0}";
		private const string Description = @"Prevent passing null values for sender/object to event handler (for instance-based events).";

		public PassSenderToEventHandlerAnalyzer()
			: base(DiagnosticId.PassSenderToEventHandler, Title, MessageFormat, Description, Categories.Maintainability)
		{ }
	}

	public class PassSenderToEventHandlerSyntaxNodeAction : SyntaxNodeAction<EventFieldDeclarationSyntax>
	{
		private readonly Helper _helper = new();

		public override void Analyze()
		{
			VariableDeclaratorSyntax variable = Node.Declaration.Variables.FirstOrDefault();
			if (variable == null)
			{
				return;
			}

			string eventName = variable.Identifier.Text;

			TypeDeclarationSyntax parent = Node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
			if (parent == null)
			{
				// Should never happen, field must be declared inside a type declaration.
				return;
			}

			AnalyzeArguments(parent, eventName);
		}

		private void AnalyzeArguments(TypeDeclarationSyntax parent, string eventName)
		{
			// EventHandlers must have 2 arguments as checked by CA1003, assume this rule is obeyed here.
			System.Collections.Generic.IEnumerable<InvocationExpressionSyntax> invocations = parent.DescendantNodes()
				.OfType<InvocationExpressionSyntax>()
				.Where(invocation => IsOurEvent(invocation, eventName))
				.Where(i => i.ArgumentList.Arguments.Count == 2);

			foreach (InvocationExpressionSyntax invocation in invocations)
			{
				SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
				if (_helper.IsLiteralNull(arguments[0].Expression))
				{
					Location loc = arguments[0].GetLocation();
					ReportDiagnostic(loc, eventName);
				}

				if (_helper.IsLiteralNull(arguments[1].Expression))
				{
					Location loc = arguments[1].GetLocation();
					ReportDiagnostic(loc, eventName);
				}
			}
		}

		private bool IsOurEvent(InvocationExpressionSyntax invocation, string eventName)
		{
			return invocation.Expression is IdentifierNameSyntax name && name.Identifier.Text == eventName;
		}
	}
}
