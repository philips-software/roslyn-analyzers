// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidSuppressMessageAttributeAnalyzer : DiagnosticAnalyzer
	{
		public const string AvoidSuppressMessageAttributeWhitelist = @"AvoidSuppressMessageAttributeWhitelist.txt";

		private static AttributeModel attribute = GetAttributeModel();

		public static ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(attribute.Rule);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return Rules; } }

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(startContext =>
			{
				ImmutableHashSet<string> whitelist = null;

				if (startContext.Compilation.GetTypeByMetadataName(attribute.FullName) != null)
				{

					if (whitelist == null)
					{
						whitelist = PopulateWhitelist(startContext.Options);
					}

					startContext.RegisterSyntaxNodeAction(
						(c) => Analyze(attribute, c, whitelist),
						SyntaxKind.AttributeList);
				}
			});
		}

		private ImmutableHashSet<string> PopulateWhitelist(AnalyzerOptions options)
		{
			foreach (var file in options.AdditionalFiles)
			{
				if (Path.GetFileName(file.Path) != AvoidSuppressMessageAttributeWhitelist)
				{
					continue;
				}

				var text = file.GetText();

				var builder = ImmutableHashSet.CreateBuilder<string>();
				if (text != null)
					foreach (var textLine in text.Lines)
					{
						string line = textLine.ToString();
						builder.Add(line);
					}

				return builder.ToImmutable();
			}

			return ImmutableHashSet<string>.Empty;
		}

		private static void Analyze(AttributeModel attribute, SyntaxNodeAnalysisContext context, ImmutableHashSet<string> whitelist)
		{
			AttributeListSyntax attributesNode = (AttributeListSyntax)context.Node;

			if (Helper.HasAttribute(attributesNode, context, attribute.Name, attribute.FullName, out var descriptionLocation) && !Helper.IsGeneratedCode(context))
			{

				string id = null;
				if (!IsWhitelisted(whitelist, context.SemanticModel, attributesNode.Parent, out id))
				{
					Diagnostic diagnostic = Diagnostic.Create(attribute.Rule, descriptionLocation, id);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		private static bool IsWhitelisted(ImmutableHashSet<string> whitelist, SemanticModel semanticModel, SyntaxNode node, out string id)
		{
			var symbol = semanticModel.GetDeclaredSymbol(node);

			if (symbol == null)
			{
				id = null;
				return false;
			}

			id = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

			if (whitelist.Contains(id))
			{
				return true;
			}

			return false;
		}

		private static AttributeModel GetAttributeModel()
		{
			return new AttributeModel(@"SuppressMessage",
				@"System.Diagnostics.CodeAnalysis.SuppressMessageAttribute",
				@"SuppressMessage not allowed",
				@"SuppressMessage is not allowed.",
				@"SuppressMessage results in violations of codified coding guidelines.",
				DiagnosticIds.AvoidSuppressMessage,
				canBeSuppressed: false,
				isEnabledByDefault: true);
		}
	}
}
