// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReduceCognitiveLoadAnalyzer : SingleDiagnosticAnalyzer<MethodDeclarationSyntax, ReduceCognitiveLoadSyntaxNodeAction>
	{
		private const string Title = @"Reduce Cognitive Load";
		public const string MessageFormat = @"Reduce Cognitive Load of {0} to threshold of {1}";
		private const string Description = @"This method has too many nested block statements and logical cases to consider in this method";

		public AdditionalFilesHelper AdditionalFilesHelper { get; }

		public ReduceCognitiveLoadAnalyzer()
			: this(null)
		{ }

		public ReduceCognitiveLoadAnalyzer(AdditionalFilesHelper additionalFilesHelper)
			: base(DiagnosticId.ReduceCognitiveLoad, Title, MessageFormat, Description, Categories.Maintainability)
		{
			AdditionalFilesHelper = additionalFilesHelper;
		}
	}

	public class ReduceCognitiveLoadSyntaxNodeAction : SyntaxNodeAction<MethodDeclarationSyntax>
	{

		private int MaxCognitiveLoad { get; set; }
		private const int DefaultMaxCognitiveLoad = 25;

		private int CalcCognitiveLoad(BlockSyntax blockSyntax)
		{
			int cognitiveLoad = 1;
			foreach (BlockSyntax descBlockSyntax in blockSyntax.DescendantNodes().OfType<BlockSyntax>())
			{
				cognitiveLoad += CalcCognitiveLoad(descBlockSyntax);
			}
			return cognitiveLoad;
		}

		public override void Analyze()
		{
			BlockSyntax blockSyntax = Node.DescendantNodes().OfType<BlockSyntax>().FirstOrDefault();
			if (blockSyntax == null)
			{
				return;
			}
			int cognitiveLoad = CalcCognitiveLoad(blockSyntax);

			cognitiveLoad += blockSyntax.DescendantTokens().Count((token) =>
			{
				return
					token.IsKind(SyntaxKind.BarBarToken) ||
					token.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
					token.IsKind(SyntaxKind.ExclamationToken) ||
					token.IsKind(SyntaxKind.ExclamationEqualsToken) ||
					token.IsKind(SyntaxKind.BreakKeyword) ||
					token.IsKind(SyntaxKind.ContinueKeyword)
					;
			});

			InitializeMaxCognitiveLoad();
			if (cognitiveLoad > MaxCognitiveLoad)
			{
				Location location = Node.Identifier.GetLocation();
				ReportDiagnostic(location, cognitiveLoad, MaxCognitiveLoad);
			}
		}

		private void InitializeMaxCognitiveLoad()
		{
			if (MaxCognitiveLoad == 0)
			{
				AdditionalFilesHelper additionalFilesHelper = (Analyzer as ReduceCognitiveLoadAnalyzer).AdditionalFilesHelper;
				additionalFilesHelper ??= new AdditionalFilesHelper(Context.Options, Context.Compilation);
				string configuredMaxCognitiveLoad = additionalFilesHelper.GetValueFromEditorConfig(Rule.Id, @"max_cognitive_load");
				if (int.TryParse(configuredMaxCognitiveLoad, NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxAllowedCognitiveLoad) && maxAllowedCognitiveLoad is >= 1 and <= 100)
				{
					MaxCognitiveLoad = maxAllowedCognitiveLoad;
					return;
				}
				MaxCognitiveLoad = DefaultMaxCognitiveLoad;
			}
		}
	}
}
