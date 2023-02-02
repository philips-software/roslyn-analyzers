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
	public class UnmanagedObjectsNeedDisposingAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = "Unmanaged object need disposing";
		private const string Message = "The field {0} is refering to an unmanaged object and needs to be declared in a class that implements IDisposable.";
		private const string Description = "Every field which holds an unmanaged object needs to be declared in a class that implements IDisposable.";
		private const string Category = Categories.RuntimeFailure;

		private static readonly DiagnosticDescriptor Rule =
			new(
				Helper.ToDiagnosticId(DiagnosticId.UnmanagedObjectsNeedDisposing),
				Title,
				Message,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: Description
			);

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.FieldDeclaration);
		}

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var field = (FieldDeclarationSyntax)context.Node;

			if (IsUnmanaged(field.Declaration.Type) && !TypeImplementsIDisposable(context, field))
			{
				var variableName = field.Declaration.Variables[0].Identifier.Text;
				var loc = field.Declaration.Variables[0].Identifier.GetLocation();
				context.ReportDiagnostic(Diagnostic.Create(Rule, loc, variableName));
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

		private bool TypeImplementsIDisposable(SyntaxNodeAnalysisContext context, FieldDeclarationSyntax field)
		{
			var type = field.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
			if (type == null)
			{
				return false;
			}
			var typeSymbol = context.SemanticModel.GetDeclaredSymbol(type);
			if (typeSymbol == null)
			{
				return false;
			}
			return typeSymbol.BaseType.Name == "IDisposable" || typeSymbol.AllInterfaces.Any(face => face.Name == "IDisposable");
		}
	}
}
