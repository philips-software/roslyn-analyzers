// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidAttributeAnalyzer : DiagnosticAnalyzer
	{
		public const string AttributesWhitelist = @"AvoidAttributesWhitelist.txt";

		private static readonly ImmutableDictionary<string, ImmutableArray<AttributeModel>> attributes = GetAttributeModels();

		public static readonly ImmutableArray<DiagnosticDescriptor> Rules = GetRules(attributes);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return Rules; } }

		private readonly AttributeHelper _attributeHelper;

		public AvoidAttributeAnalyzer()
			: this(new AttributeHelper())
		{ }

		public AvoidAttributeAnalyzer(AttributeHelper attributeHelper)
		{
			_attributeHelper = attributeHelper;
		}

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(startContext =>
			{
				ImmutableHashSet<string> whitelist = null;

				foreach (var kvp in attributes)
				{
					if (startContext.Compilation.GetTypeByMetadataName(kvp.Key) == null)
					{
						continue;
					}

					whitelist ??= PopulateWhitelist(startContext.Options);

					startContext.RegisterSyntaxNodeAction((c) => Analyze(kvp.Value, c, whitelist), SyntaxKind.AttributeList);
				}
			});
		}

		private ImmutableHashSet<string> PopulateWhitelist(AnalyzerOptions options)
		{
			foreach (var file in options.AdditionalFiles)
			{
				if (Path.GetFileName(file.Path) != AttributesWhitelist)
				{
					continue;
				}

				var text = file.GetText();

				var builder = ImmutableHashSet.CreateBuilder<string>();
				if (text != null)
				{
					foreach (var textLine in text.Lines)
					{
						string line = textLine.ToString();
						builder.Add(line);
					}
				}

				return builder.ToImmutable();
			}

			return ImmutableHashSet<string>.Empty;
		}

		private void Analyze(ImmutableArray<AttributeModel> attributes, SyntaxNodeAnalysisContext context, ImmutableHashSet<string> whitelist)
		{
			GeneratedCodeDetector generatedCodeDetector = new();
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			AttributeListSyntax attributesNode = (AttributeListSyntax)context.Node;

			foreach (AttributeModel attribute in attributes)
			{
				if (!_attributeHelper.HasAttribute(attributesNode, context, attribute.Name, attribute.FullName, out var descriptionLocation))
				{
					continue;
				}

				string id = null;
				if (attribute.IsSuppressible && IsWhitelisted(whitelist, context.SemanticModel, attributesNode.Parent, out id))
				{
					continue;
				}

				Diagnostic diagnostic = Diagnostic.Create(attribute.Rule, descriptionLocation, id);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private bool IsWhitelisted(ImmutableHashSet<string> whitelist, SemanticModel semanticModel, SyntaxNode node, out string id)
		{
			var symbol = semanticModel.GetDeclaredSymbol(node);

			if (symbol == null)
			{
				id = null;
				return false;
			}

			id = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

			return whitelist.Contains(id);
		}

		private static ImmutableArray<DiagnosticDescriptor> GetRules(ImmutableDictionary<string, ImmutableArray<AttributeModel>> attributes)
		{
			var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>();

			var items = attributes.SelectMany(x => x.Value)
									.Select(x => x.Rule);
			builder.AddRange(items);

			return builder.ToImmutable();
		}

		private static ImmutableDictionary<string, ImmutableArray<AttributeModel>> GetAttributeModels()
		{
			var ownerAttribute = new AttributeModel(@"Owner",
				@"Microsoft.VisualStudio.TestTools.UnitTesting.OwnerAttribute",
				@"Owner attribute not allowed",
				@"Tests may not use the Owner attribute.",
				@"The Owner attribute is no more relevant.",
				DiagnosticId.AvoidOwnerAttribute,
				isSuppressible: false,
				isEnabledByDefault: true);

			var removedAttribute = new AttributeModel(@"Ignore",
				@"Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute",
				@"Ignore attribute not allowed",
				@"Tests may not use the Ignore attribute.",
				@"The Ignore attribute creates dead code and build warnings.  Rather than ignoring a test, fix it or remove it.  (Rely on Version Control to save it.)",
				DiagnosticId.AvoidIgnoreAttribute,
				isSuppressible: false,
				isEnabledByDefault: true);

			var testInitializeAttribute = new AttributeModel(MsTestFrameworkDefinitions.TestInitializeAttribute,
				@"TestInitialize methods not allowed",
				@"Tests may not have any TestInitialize methods. ({0})",
				@"TestInitialize methods are not deterministic and can create unexpected test results.",
				DiagnosticId.AvoidTestInitializeMethod,
				isSuppressible: true,
				isEnabledByDefault: true);

			var classInitializeAttribute = new AttributeModel(MsTestFrameworkDefinitions.ClassInitializeAttribute,
				@"ClassInitialize methods not allowed",
				@"Tests may not have any ClassInitialize methods. ({0})",
				@"ClassInitialize methods are not deterministic and can create unexpected test results.",
				DiagnosticId.AvoidClassInitializeMethod,
				isSuppressible: true,
				isEnabledByDefault: true);

			var classCleanupAttribute = new AttributeModel(MsTestFrameworkDefinitions.ClassCleanupAttribute,
				@"ClassCleanup methods not allowed",
				@"Tests may not have any ClassCleanup methods. ({0})",
				@"ClassCleanup methods are not deterministic and can create unexpected test results.",
				DiagnosticId.AvoidClassCleanupMethod,
				isSuppressible: true,
				isEnabledByDefault: true);

			var testCleanupAttribute = new AttributeModel(MsTestFrameworkDefinitions.TestCleanupAttribute,
				@"TestCleanup methods not allowed",
				@"Tests may not have any TestCleanup methods. ({0})",
				@"TestCleanup methods are not deterministic and can create unexpected test results.",
				DiagnosticId.AvoidTestCleanupMethod,
				isSuppressible: true,
				isEnabledByDefault: true);

			var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<AttributeModel>>();

			builder["Microsoft.VisualStudio.TestTools.UnitTesting.Assert"] = ImmutableArray.Create(ownerAttribute, removedAttribute, testInitializeAttribute, testCleanupAttribute, classCleanupAttribute, classInitializeAttribute);

			return builder.ToImmutable();
		}
	}
}
