// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NuGet.Protocol.Core.Types;
using NuGet.Configuration;
using System.Threading;
using System.Linq;
using System.Net.Http;
using NuGet.Common;
using System.Collections.Generic;
using System;

namespace Philips.CodeAnalysis.LicenseAnalyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NugetLicenseAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "PH3077";
		private const string Title = "Commercial License Detected";
		private const string MessageFormat = "Package '{0}' uses a commercial license";
		private const string Description = "Commercial licenses are not allowed.";
		private const string Category = "Licensing";

		private static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId,
			Title,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction(Analyze);
		}

		private void Analyze(CompilationStartAnalysisContext context)
		{
			ArgumentNullException.ThrowIfNull(context);

			IEnumerable<AssemblyIdentity> packageReferences = context.Compilation.ReferencedAssemblyNames;

			using var httpClient = new HttpClient();
			IEnumerable<Lazy<INuGetResourceProvider>> providers = Repository.Provider.GetCoreV3();
			var repository = new SourceRepository(new PackageSource("https://api.nuget.org/v3/index.json"), providers);

			var cache = new SourceCacheContext();
			ILogger logger = NullLogger.Instance;

			foreach (AssemblyIdentity reference in packageReferences)
			{
				try
				{
					PackageMetadataResource resource = repository.GetResourceAsync<PackageMetadataResource>().GetAwaiter().GetResult();
					IPackageSearchMetadata package = resource.GetMetadataAsync(reference.Name, includePrerelease: true, includeUnlisted: false, cache, logger, CancellationToken.None).GetAwaiter().GetResult().FirstOrDefault();

					if (package != null)
					{
						var licenses = package.LicenseMetadata?.License ?? "Unknown";
						if (IsCommercialLicense(licenses))
						{
							var diagnostic = Diagnostic.Create(Rule, Location.None, reference.Name);
							context.RegisterCompilationEndAction(endContext => endContext.ReportDiagnostic(diagnostic));
						}
					}
				}
				catch
				{
					// Handle package lookup errors gracefully
				}
			}
		}

		private bool IsCommercialLicense(string license)
		{
			// Open-source licenses allowed
			var openSourceLicenses = new[]
			{
				"MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause",
				"GPL-2.0", "GPL-3.0", "LGPL-3.0", "ISC", "MPL-2.0",
				"CC0-1.0", "Unlicense", "EPL-1.0", "EPL-2.0"
			};

			var commercialLicenses = new[]
			{
				"Proprietary", "Commercial", "EULA", "SSPL",
				"Elastic License", "Confluent Community License",
				"BSL-1.1", "Custom"
			};

			if (string.IsNullOrWhiteSpace(license))
			{
				return true;  // Treat unknown licenses as commercial
			}

			// Block commercial licenses
			return !openSourceLicenses.Contains(license) || commercialLicenses.Contains(license);
		}

	}

}
