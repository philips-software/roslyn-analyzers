﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AssertFailAnalyzer : DiagnosticAnalyzerBase
	{
		private const string Title = @"Assert.Fail should not be used if an alternative is more appropriate";
		private const string MessageFormat = @"Assert.Fail should not be used in this scenario.  Consider using Assert.AreEqual or Assert.IsTrue or Assert.IsNull";
		private const string Description = @"";
		private const string Category = Categories.MsTest;

		private static readonly DiagnosticDescriptor Rule = new(DiagnosticId.AssertFail.ToId(), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		private sealed class AssertMetadata
		{
			public ImmutableArray<IMethodSymbol> FailMethods { get; set; }
		}

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			INamedTypeSymbol assertClass = context.Compilation.GetTypeByMetadataName(StringConstants.AssertFullyQualifiedName);

			if (assertClass == null)
			{
				return;
			}

			AssertMetadata metadata = new()
			{
				FailMethods = assertClass.GetMembers("Fail").OfType<IMethodSymbol>().ToImmutableArray(),
			};

			context.RegisterOperationAction((x) => OnMethodCall(metadata, x), OperationKind.Invocation);
		}

		private void OnMethodCall(AssertMetadata metadata, OperationAnalysisContext obj)
		{
			var invocation = (IInvocationOperation)obj.Operation;

			if (!metadata.FailMethods.Contains(invocation.TargetMethod))
			{
				//not a call to Assert.Fail
				return;
			}

			if (invocation.Parent is not IExpressionStatementOperation expressionOperation)
			{
				return;
			}

			// check if they did this:
			// if/else/foreach/using ( something )
			// { Assert.Fail() }
			if (expressionOperation.Parent is IBlockOperation blockOperation && CheckBlock(blockOperation, expressionOperation))
			{
				Location location = invocation.Syntax.GetLocation();
				obj.ReportDiagnostic(Diagnostic.Create(Rule, location));
				return;
			}

			// bare if/else (IE, no block)
			if (expressionOperation.Parent is IConditionalOperation)
			{
				Location location = invocation.Syntax.GetLocation();
				obj.ReportDiagnostic(Diagnostic.Create(Rule, location));
			}

			// bare foreach loop
			if (expressionOperation.Parent is IForEachLoopOperation)
			{
				Location location = invocation.Syntax.GetLocation();
				obj.ReportDiagnostic(Diagnostic.Create(Rule, location));
			}
		}

		private bool CheckBlock(IBlockOperation blockOperation, IExpressionStatementOperation expressionOperation)
		{
			if (blockOperation.Parent is IUsingOperation or ICatchClauseOperation)
			{
				return false;
			}

			if (blockOperation.Operations.Length == 1)
			{
				return true;
			}

			var index = blockOperation.Operations.IndexOf(expressionOperation);

			if (index != blockOperation.Operations.Length - 1)
			{
				return false;
			}

			// the assert.fail is the last operation.  Check if they are ending the loop with an if(blah) continue; fail.

			IOperation previous = blockOperation.Operations[index - 1];

			if (previous is IConditionalOperation conditional && conditional.WhenFalse is null)
			{
				static bool IsContinue(IOperation operation)
				{
					return operation switch
					{
						IBranchOperation branch => branch.Target.Name == "continue",
						IBlockOperation block => block.Operations.Length == 1 && IsContinue(block.Operations[0]),
						_ => false,
					};
				}

				if (IsContinue(conditional.WhenTrue))
				{
					return true;
				}
			}

			return false;
		}
	}
}
