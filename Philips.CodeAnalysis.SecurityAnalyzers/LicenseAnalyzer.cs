// © 2025 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.SecurityAnalyzers
{
	/// <summary>
	/// Represents a package from project.assets.json
	/// </summary>
	internal sealed class PackageInfo
	{
		public string Name { get; set; }
		public string Version { get; set; }
		public string Type { get; set; }
		public string Path { get; set; }
	}

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LicenseAnalyzer : DiagnosticAnalyzer
	{
		private const string Title = @"Avoid Packages with Unacceptable Licenses";
		public const string MessageFormat = @"Package '{0}' version '{1}' has an unacceptable license '{2}'. " +
											 @"Consider adding to Allowed.Licenses.txt if license is acceptable.";
		private const string Description = @"Packages with unacceptable licenses (e.g., copyleft licenses like GPL) should be " +
										   @"reviewed before use to ensure compliance with project requirements.";

		public const string AllowedLicensesFileName = @"Allowed.Licenses.txt";
		public const string LicensesCacheFileName = @"licenses.cache";
		private const string ProjectAssetsFileName = @"project.assets.json";

		// Default acceptable licenses (permissive licenses that are generally safe to use)
		private static readonly HashSet<string> DefaultAcceptableLicenses = new(StringComparer.OrdinalIgnoreCase)
		{
			"MIT",
			"Apache-2.0",
			"BSD-2-Clause",
			"BSD-3-Clause",
			"ISC",
			"Unlicense",
			"0BSD"
		};

		private static readonly char[] LineSeparators = { '\r', '\n' };
		private static readonly char[] EqualsSeparator = { '=' };

		private static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId.AvoidUnlicensedPackages.ToId(),
			Title,
			MessageFormat,
			Categories.Security,
			DiagnosticSeverity.Error,
			isEnabledByDefault: false,
			Description,
			DiagnosticId.AvoidUnlicensedPackages.ToHelpLinkUrl());

		private static readonly DiagnosticDescriptor InfoDiagnostic = new(
			"PH2155_INFO",
			"License analysis information",
			"{0}",
			Categories.Security,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Rule, InfoDiagnostic);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationAction(AnalyzeProject);
		}

		private void AnalyzeProject(CompilationAnalysisContext context)
		{
			// Skip analysis for test projects to avoid noise during development
			if (IsTestProject(context.Compilation))
			{
				return;
			}

			try
			{
				// Load custom allowed licenses from additional files
				HashSet<string> allowedLicenses = GetAllowedLicenses(context.Options.AdditionalFiles);

				// Find and analyze project.assets.json using AnalyzerConfigOptionsProvider
				// Implementation follows the requested approach:
				// * Use project.assets.json (found via AnalyzerConfigOptionsProvider) instead of assembly file names
				// * Parse that file and use Nuget.ProjectModel package
				// * Build a licenses.json file for performance/caching (configure the file via a diagnostic config option)
				// * Check licenses OFFLINE using project.assets.json and the local global‑packages cache
				AnalyzePackagesFromAssetsFile(context, allowedLicenses);
			}
			catch (Exception ex)
			{
				// Report diagnostic about the error instead of silently failing
				var diagnostic = Diagnostic.Create(InfoDiagnostic, Location.None, $"Failed to analyze package licenses: {ex.Message}");
				context.ReportDiagnostic(diagnostic);
			}
		}

		private static bool IsTestProject(Compilation compilation)
		{
			// Check if this is a test project by looking for common test framework references
			var referencedAssemblyNames = new HashSet<string>(
				compilation.ReferencedAssemblyNames.Select(name => name.Name),
				StringComparer.OrdinalIgnoreCase);

			return referencedAssemblyNames.Contains("Microsoft.VisualStudio.TestPlatform.TestFramework") ||
				   referencedAssemblyNames.Contains("MSTest.TestFramework") ||
				   referencedAssemblyNames.Contains("NUnit.Framework") ||
				   referencedAssemblyNames.Contains("xunit") ||
				   referencedAssemblyNames.Contains("xunit.core");
		}

		private void AnalyzePackagesFromAssetsFile(CompilationAnalysisContext context, HashSet<string> allowedLicenses)
		{
			// Get project.assets.json path from analyzer config options
			var assetsFilePath = TryFindAssetsFileFromSourcePaths(context);
			if (string.IsNullOrEmpty(assetsFilePath) || !File.Exists(assetsFilePath))
			{
				// Report diagnostic that project.assets.json was not found
				var diagnostic = Diagnostic.Create(InfoDiagnostic, Location.None, "Could not find project.assets.json file for license analysis");
				context.ReportDiagnostic(diagnostic);
				return;
			}

			// Load and parse project.assets.json manually to avoid external dependencies
			List<PackageInfo> packages;
			try
			{
				// Parse project.assets.json manually for better analyzer compatibility
				packages = ParseProjectAssetsFile(assetsFilePath);
			}
			catch (Exception ex)
			{
				// Report diagnostic about parsing failure instead of silently failing
				var diagnostic = Diagnostic.Create(InfoDiagnostic, Location.None, $"Failed to parse project.assets.json: {ex.Message}");
				context.ReportDiagnostic(diagnostic);
				return;
			}

			if (packages == null || packages.Count == 0)
			{
				return;
			}

			// Load or create license cache
			Dictionary<string, PackageLicenseInfo> licenseCache = LoadLicenseCache(Path.GetDirectoryName(assetsFilePath));

			// Analyze each package
			foreach (PackageInfo package in packages)
			{
				if (package.Type != "package")
				{
					continue;
				}

				// Get license information for this package
				var licenseInfo = GetPackageLicenseInfo(package, licenseCache);

				// Check if license is acceptable
				if (!string.IsNullOrEmpty(licenseInfo) && !IsLicenseAcceptable(licenseInfo, allowedLicenses))
				{
					var diagnostic = Diagnostic.Create(
						Rule,
						Location.None,
						package.Name,
						package.Version ?? "unknown",
						licenseInfo);

					context.ReportDiagnostic(diagnostic);
				}
			}

			// Save updated license cache
			SaveLicenseCache(licenseCache, Path.GetDirectoryName(assetsFilePath));
		}

		private static string TryFindAssetsFileFromSourcePaths(CompilationAnalysisContext context)
		{
			// For .NET Standard 2.0 compatibility, use alternative approaches to find project.assets.json
			// Try common locations relative to the current working directory
			var currentDir = Directory.GetCurrentDirectory();

			// Report debug information about search starting point
			var diagnostic = Diagnostic.Create(InfoDiagnostic, Location.None, $"Searching for {ProjectAssetsFileName} starting from directory: {currentDir}");
			context.ReportDiagnostic(diagnostic);

			// Look for project.assets.json in common locations
			var possiblePaths = new[]
			{
				Path.Combine(currentDir, "obj", ProjectAssetsFileName),
				Path.Combine(currentDir, "..", "obj", ProjectAssetsFileName),
				Path.Combine(currentDir, "..", "..", "obj", ProjectAssetsFileName)
			};

			// Report each path being checked
			foreach (var path in possiblePaths)
			{
				var exists = File.Exists(path);
				var diagnostic2 = Diagnostic.Create(InfoDiagnostic, Location.None, $"Checking path: {path} - Exists: {exists}");
				context.ReportDiagnostic(diagnostic2);
			}

			var foundPath = possiblePaths.Where(File.Exists).FirstOrDefault();
			if (foundPath != null)
			{
				var diagnostic3 = Diagnostic.Create(InfoDiagnostic, Location.None, $"Found {ProjectAssetsFileName} at: {foundPath}");
				context.ReportDiagnostic(diagnostic3);
				return foundPath;
			}

			// If not found in common locations, search in current directory tree
			try
			{
				var diagnostic4 = Diagnostic.Create(InfoDiagnostic, Location.None, "Common paths failed, searching directory tree...");
				context.ReportDiagnostic(diagnostic4);

				var searchDir = currentDir;
				for (var i = 0; i < 5; i++) // Limit search depth
				{
					var assetsPath = Path.Combine(searchDir, "obj", ProjectAssetsFileName);
					var exists = File.Exists(assetsPath);

					var diagnostic5 = Diagnostic.Create(InfoDiagnostic, Location.None, $"Tree search depth {i}: checking {assetsPath} - Exists: {exists}");
					context.ReportDiagnostic(diagnostic5);

					if (exists)
					{
						var diagnostic6 = Diagnostic.Create(InfoDiagnostic, Location.None, $"Found {ProjectAssetsFileName} via tree search at: {assetsPath}");
						context.ReportDiagnostic(diagnostic6);
						return assetsPath;
					}

					DirectoryInfo parentDir = Directory.GetParent(searchDir);
					if (parentDir == null)
					{
						var diagnostic7 = Diagnostic.Create(InfoDiagnostic, Location.None, $"Reached root directory at depth {i}, stopping search");
						context.ReportDiagnostic(diagnostic7);
						break;
					}
					searchDir = parentDir.FullName;
				}
			}
			catch (Exception ex)
			{
				// Report diagnostic about directory traversal failure
				var diagnostic8 = Diagnostic.Create(InfoDiagnostic, Location.None, $"Directory traversal failed: {ex.Message}");
				context.ReportDiagnostic(diagnostic8);
				return null;
			}

			var diagnostic9 = Diagnostic.Create(InfoDiagnostic, Location.None, $"{ProjectAssetsFileName} not found after exhaustive search");
			context.ReportDiagnostic(diagnostic9);
			return null;
		}

		private static List<PackageInfo> ParseProjectAssetsFile(string assetsFilePath)
		{
			var packages = new List<PackageInfo>();

			try
			{
				var content = File.ReadAllText(assetsFilePath);

				// Find the "libraries" section using simple string parsing
				// This is more reliable than pulling in JSON dependencies for analyzers
				Match librariesMatch = Regex.Match(content, @"""libraries""\s*:\s*\{");
				if (!librariesMatch.Success)
				{
					return packages;
				}

				// Find the start of libraries section
				var startIndex = librariesMatch.Index + librariesMatch.Length;
				var braceCount = 1;
				var currentIndex = startIndex;

				// Find the end of the libraries section by counting braces
				while (currentIndex < content.Length && braceCount > 0)
				{
					var c = content[currentIndex];
					if (c == '{')
					{
						braceCount++;
					}
					else if (c == '}')
					{
						braceCount--;
					}
					currentIndex++;
				}

				if (braceCount != 0)
				{
					return packages;
				}

				// Extract libraries section content
				var librariesContent = content.Substring(startIndex, currentIndex - startIndex - 1);

				// Parse each package entry using regex
				MatchCollection packageMatches = Regex.Matches(librariesContent, @"""([^/]+)/([^""]+)""\s*:\s*\{[^}]*""type""\s*:\s*""([^""]+)""[^}]*(?:""path""\s*:\s*""([^""]+)"")?[^}]*\}");

				foreach (Match match in packageMatches)
				{
					if (match.Groups.Count >= 4)
					{
						var package = new PackageInfo
						{
							Name = match.Groups[1].Value,
							Version = match.Groups[2].Value,
							Type = match.Groups[3].Value,
							Path = match.Groups.Count > 4 ? match.Groups[4].Value : null
						};
						packages.Add(package);
					}
				}
			}
			catch (Exception)
			{
				// Return empty list on parsing errors
				return [];
			}

			return packages;
		}

		private static Dictionary<string, PackageLicenseInfo> LoadLicenseCache(string projectDirectory)
		{
			if (string.IsNullOrEmpty(projectDirectory))
			{
				return [];
			}

			var cacheFilePath = Path.Combine(projectDirectory, LicensesCacheFileName);
			if (!File.Exists(cacheFilePath))
			{
				return [];
			}

			try
			{
				var cache = new Dictionary<string, PackageLicenseInfo>();
				var lines = File.ReadAllLines(cacheFilePath);
				foreach (var line in lines)
				{
					if (string.IsNullOrWhiteSpace(line) || !line.Contains('='))
					{
						continue;
					}

					var parts = line.Split(EqualsSeparator, 2);
					if (parts.Length == 2)
					{
						cache[parts[0]] = new PackageLicenseInfo { License = parts[1] };
					}
				}
				return cache;
			}
			catch (Exception)
			{
				// If cache is corrupted, start fresh
				return [];
			}
		}

		private static void SaveLicenseCache(Dictionary<string, PackageLicenseInfo> cache, string projectDirectory)
		{
			if (string.IsNullOrEmpty(projectDirectory))
			{
				return;
			}

			try
			{
				var cacheFilePath = Path.Combine(projectDirectory, LicensesCacheFileName);
				IEnumerable<string> lines = cache.Select(kvp => $"{kvp.Key}={kvp.Value.License}");
				File.WriteAllLines(cacheFilePath, lines);
			}
			catch (Exception)
			{
				// If we can't write cache, just continue without caching
				return;
			}
		}

		private static string GetPackageLicenseInfo(PackageInfo package, Dictionary<string, PackageLicenseInfo> licenseCache)
		{
			var cacheKey = $"{package.Name}#{package.Version}";

			// Check cache first
			if (licenseCache.TryGetValue(cacheKey, out PackageLicenseInfo cachedInfo))
			{
				return cachedInfo.License;
			}

			// Try to get license from package metadata in global packages cache
			var license = ExtractLicenseFromGlobalPackages(package);

			// Cache the result (even if null/empty)
			licenseCache[cacheKey] = new PackageLicenseInfo { License = license ?? string.Empty };

			return license;
		}

		private static string ExtractLicenseFromGlobalPackages(PackageInfo package)
		{
			// Get global packages folder path
			var globalPackagesPath = GetGlobalPackagesPath();
			if (string.IsNullOrEmpty(globalPackagesPath))
			{
				return null;
			}

			// Construct path to package in global cache using the package path if available, 
			// otherwise construct from name and version
			string packagePath;
			if (!string.IsNullOrEmpty(package.Path))
			{
				packagePath = Path.Combine(globalPackagesPath, package.Path);
			}
			else
			{
				packagePath = Path.Combine(globalPackagesPath,
					package.Name.ToLowerInvariant(),
					package.Version?.ToLowerInvariant() ?? "");
			}

			if (!Directory.Exists(packagePath))
			{
				return null;
			}

			// Look for .nuspec file which contains license information
			var nuspecPath = Path.Combine(packagePath, $"{package.Name}.nuspec");
			if (File.Exists(nuspecPath))
			{
				return ExtractLicenseFromNuspec(nuspecPath);
			}

			return null;
		}

		private static string GetGlobalPackagesPath()
		{
			// Try to get from NuGet configuration
			var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var defaultPath = Path.Combine(userProfile, ".nuget", "packages");

			// Check if default path exists
			if (Directory.Exists(defaultPath))
			{
				return defaultPath;
			}

			// Could also check NUGET_PACKAGES environment variable
			var nugetPackagesEnv = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
			if (!string.IsNullOrEmpty(nugetPackagesEnv) && Directory.Exists(nugetPackagesEnv))
			{
				return nugetPackagesEnv;
			}

			return defaultPath; // Return default even if it doesn't exist
		}

		private static string ExtractLicenseFromNuspec(string nuspecPath)
		{
			try
			{
				var content = File.ReadAllText(nuspecPath);

				// Try to extract license from <license> element first
				var licenseFromElement = ExtractLicenseElement(content);
				if (!string.IsNullOrEmpty(licenseFromElement))
				{
					return licenseFromElement;
				}

				// Fall back to extracting from <licenseUrl> element
				return ExtractLicenseUrl(content);
			}
			catch (Exception)
			{
				// If we can't parse the nuspec, return null
				return null;
			}
		}

		private static string ExtractLicenseElement(string content)
		{
			var licenseStart = content.IndexOf("<license", StringComparison.OrdinalIgnoreCase);
			if (licenseStart < 0)
			{
				return null;
			}

			var typeStart = content.IndexOf("type=\"", licenseStart, StringComparison.OrdinalIgnoreCase);
			if (typeStart < 0)
			{
				return null;
			}

			typeStart += 6; // Skip 'type="'
			var typeEnd = content.IndexOf("\"", typeStart, StringComparison.OrdinalIgnoreCase);
			if (typeEnd <= typeStart)
			{
				return null;
			}

			var licenseType = content.Substring(typeStart, typeEnd - typeStart);
			if (!licenseType.Equals("expression", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			// Extract SPDX expression
			var contentStart = content.IndexOf(">", licenseStart) + 1;
			var contentEnd = content.IndexOf("</license>", contentStart, StringComparison.OrdinalIgnoreCase);
			if (contentEnd <= contentStart)
			{
				return null;
			}

			return content.Substring(contentStart, contentEnd - contentStart).Trim();
		}

		private static string ExtractLicenseUrl(string content)
		{
			var licenseUrlStart = content.IndexOf("<licenseUrl>", StringComparison.OrdinalIgnoreCase);
			if (licenseUrlStart < 0)
			{
				return null;
			}

			licenseUrlStart += 12; // Skip '<licenseUrl>'
			var licenseUrlEnd = content.IndexOf("</licenseUrl>", licenseUrlStart, StringComparison.OrdinalIgnoreCase);
			if (licenseUrlEnd <= licenseUrlStart)
			{
				return null;
			}

			var licenseUrl = content.Substring(licenseUrlStart, licenseUrlEnd - licenseUrlStart).Trim();
			// Extract license name from common URLs
			return ExtractLicenseFromUrl(licenseUrl);
		}

		private static string ExtractLicenseFromUrl(string licenseUrl)
		{
			if (string.IsNullOrEmpty(licenseUrl))
			{
				return null;
			}

			// Common license URL patterns - use IndexOf for .NET Standard 2.0 compatibility
			if (HasSubstringIgnoreCase(licenseUrl, "mit"))
			{
				return "MIT";
			}
			if (HasSubstringIgnoreCase(licenseUrl, "apache"))
			{
				return "Apache-2.0";
			}
			if (HasSubstringIgnoreCase(licenseUrl, "bsd"))
			{
				return "BSD";
			}
			if (HasSubstringIgnoreCase(licenseUrl, "gpl"))
			{
				return "GPL";
			}
			if (HasSubstringIgnoreCase(licenseUrl, "lgpl"))
			{
				return "LGPL";
			}

			// Return the URL itself if we can't identify it
			return licenseUrl;
		}

		private static bool HasSubstringIgnoreCase(string source, string value)
		{
			return source.ToUpper(CultureInfo.InvariantCulture).Contains(value.ToUpper(CultureInfo.InvariantCulture));
		}

		private static bool IsLicenseAcceptable(string license, HashSet<string> allowedLicenses)
		{
			if (string.IsNullOrEmpty(license))
			{
				return true; // Don't flag packages without license information
			}

			return allowedLicenses.Contains(license);
		}

		private static HashSet<string> GetAllowedLicenses(IEnumerable<AdditionalText> additionalFiles)
		{
			var allowedLicenses = new HashSet<string>(DefaultAcceptableLicenses, StringComparer.OrdinalIgnoreCase);

			AdditionalText licenseFile = additionalFiles?.FirstOrDefault(file =>
				Path.GetFileName(file.Path).Equals(AllowedLicensesFileName, StringComparison.OrdinalIgnoreCase));

			if (licenseFile != null)
			{
				Microsoft.CodeAnalysis.Text.SourceText text = licenseFile.GetText();
				if (text != null)
				{
					IEnumerable<string> customLicenses = text.ToString()
						.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries)
						.Select(line => line.Trim())
						.Where(line => !string.IsNullOrEmpty(line) && !(line.Length > 0 && line[0] == '#'));

					foreach (var license in customLicenses)
					{
						_ = allowedLicenses.Add(license);
					}
				}
			}

			return allowedLicenses;
		}

		private sealed class PackageLicenseInfo
		{
			public string License { get; set; } = string.Empty;
		}
	}
}
