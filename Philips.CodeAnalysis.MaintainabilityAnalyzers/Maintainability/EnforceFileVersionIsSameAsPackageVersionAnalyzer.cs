// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnforceFileVersionIsSameAsPackageVersionAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Ensure FileVersion is the same as PackageVersion";
		public const string MessageFormat = @"The FileVersion ({0}) must be the same as the PackageVersion ({1}).";
		private const string Category = Categories.Maintainability;
		private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(Helper.ToDiagnosticId(DiagnosticIds.EnforceFileVersionIsSameAsPackageVersion), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationAction(Analyze);
		}

		private void Analyze(CompilationAnalysisContext context)
		{
			var attributes = context.Compilation.Assembly.GetAttributes();

			Version fileVersion = null;
			Version informationalVersion = null;

			foreach (var attr in attributes)
			{
				if (attr.AttributeClass.Name == typeof(AssemblyFileVersionAttribute).Name)
				{
					if (!attr.ConstructorArguments.IsEmpty)
					{
						fileVersion = new Version((string)attr.ConstructorArguments[0].Value);
						fileVersion = SetRevisionToZeroIfMissing(fileVersion);
					}
				}

				if (attr.AttributeClass.Name == typeof(AssemblyInformationalVersionAttribute).Name)
				{
					if (!attr.ConstructorArguments.IsEmpty)
					{
						string strippedVersion = StripPreReleaseSuffix((string)attr.ConstructorArguments[0].Value);
						informationalVersion = new Version(strippedVersion);
						informationalVersion = SetRevisionToZeroIfMissing(informationalVersion);
					}
				}
			}

			if (fileVersion is null || informationalVersion is null)
			{
				return;
			}

			if (!fileVersion.Equals(informationalVersion))
			{
				Diagnostic diagnostic = Diagnostic.Create(Rule, null, fileVersion, informationalVersion);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private Version SetRevisionToZeroIfMissing(Version version)
		{
			if (version.Revision < 0)
			{
				version = new Version(version.Major, version.Minor, version.Build, 0);
			}
			return version;
		}

		private string StripPreReleaseSuffix(string semanticVersion)
		{
			string version = semanticVersion;
			int index = semanticVersion.IndexOf("-");
			if (index >= 0)
			{
				version = semanticVersion.Substring(0, index);
			}
			return version;
		}
	}
}
