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

		// Default acceptable licenses (permissive licenses)
		private static readonly HashSet<string> DefaultAcceptableLicenses =
			new(StringComparer.OrdinalIgnoreCase)
		{
			"MIT",
			"Apache-2.0",
			"BSD-2-Clause",
			"BSD-3-Clause",
			"ISC",
			"Unlicense",
			"0BSD",
			"PostgreSQL",
			"github.com/dotnet/corefx/blob/master/LICENSE.TXT",
			"github.com/dotnet/standard/blob/master/LICENSE.TXT",
			"go.microsoft.com/fwlink/?LinkId=329770",
			"www.bouncycastle.org/csharp/licence.html"
		};

		private static readonly char[] LineSeparators = { '\r', '\n' };
		private static readonly char[] EqualsSeparator = { '=' };

		private static readonly DiagnosticDescriptor Rule = new(
			DiagnosticId.AvoidUnlicensedPackages.ToId(),
			Title,
			MessageFormat,
			Categories.Security,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			Description,
			DiagnosticId.AvoidUnlicensedPackages.ToHelpLinkUrl());

		private static readonly DiagnosticDescriptor DebugDiagnostic = new(
			DiagnosticId.AvoidUnlicensedPackages.ToId() + "_DEBUG",
			"License analysis debug information",
			"{0}",
			Categories.Security,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Rule, DebugDiagnostic);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterCompilationAction(AnalyzeProject);
		}

		private void ReportDebugDiagnostic(CompilationAnalysisContext context, string message)
		{
			var helper = new Helper(context.Options, context.Compilation);
			if (helper.ForAdditionalFiles.IsDebugLoggingEnabled("PH2155"))
			{
				var diagnostic = Diagnostic.Create(DebugDiagnostic, Location.None, message);
				context.ReportDiagnostic(diagnostic);
			}
			else
			{
				// Always show critical debugging information for troubleshooting license issues
				if (message.Contains("License = ") || message.Contains("not acceptable") || message.Contains("acceptable"))
				{
					var diagnostic = Diagnostic.Create(DebugDiagnostic, Location.None, message);
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		private void AnalyzeProject(CompilationAnalysisContext context)
		{
			if (TestHelper.IsTestProject(context.Compilation))
			{
				return;
			}

			var helper = new Helper(context.Options, context.Compilation);

			try
			{
				var loggingEnabled = helper.ForAdditionalFiles.IsDebugLoggingEnabled("PH2155");
				ReportDebugDiagnostic(context, $"LicenseAnalyzer debug logging: {(loggingEnabled ? "enabled" : "disabled")} (configure via dotnet_code_quality.PH2155.enable_debug_logging=false)");

				HashSet<string> allowedLicenses = GetAllowedLicenses(context.Options.AdditionalFiles);

				ReportDebugDiagnostic(context, $"Allowed licenses: {string.Join(", ", allowedLicenses.OrderBy(x => x))}");

				AnalyzePackagesFromAssetsFile(context, allowedLicenses);
			}
			catch (Exception ex)
			{
				ReportDebugDiagnostic(context, $"Failed to analyze package licenses: {ex.Message}");
			}
		}

		private void AnalyzePackagesFromAssetsFile(CompilationAnalysisContext context, HashSet<string> allowedLicenses)
		{
			var assetsFilePath = TryFindAssetsFileFromSourcePaths(context);
			if (string.IsNullOrEmpty(assetsFilePath) || !File.Exists(assetsFilePath))
			{
				ReportDebugDiagnostic(context, "Could not find project.assets.json file for license analysis");
				return;
			}

			ReportDebugDiagnostic(context, $"Found {ProjectAssetsFileName} at: {assetsFilePath}");

			List<PackageInfo> packages;
			try
			{
				packages = ParseProjectAssetsFile(assetsFilePath);
			}
			catch (Exception ex)
			{
				ReportDebugDiagnostic(context, $"Failed to parse project.assets.json: {ex.Message}");
				return;
			}

			if (packages == null || packages.Count == 0)
			{
				ReportDebugDiagnostic(context, "No packages found in project.assets.json");
				return;
			}

			Dictionary<string, PackageLicenseInfo> licenseCache = LoadLicenseCache(Path.GetDirectoryName(assetsFilePath));

			foreach (PackageInfo package in packages)
			{
				if (package.Type != "package")
				{
					continue;
				}

				PackageLicenseInfo licenseInfo = GetPackageLicenseInfo(package, licenseCache);

				var license = licenseInfo?.License;
				var projectUrl = licenseInfo?.ProjectUrl;

				var displayLicense = string.IsNullOrEmpty(license) ? "unknown" : license;
				var displayProjectUrl = string.IsNullOrEmpty(projectUrl) ? "none" : projectUrl;

				ReportDebugDiagnostic(context, $"Package {package.Name} {package.Version ?? "unknown"}: License = {displayLicense}, ProjectUrl = {displayProjectUrl}");

				if (!string.IsNullOrEmpty(license) && !IsLicenseAcceptable(context, license, projectUrl, allowedLicenses))
				{
					ReportDebugDiagnostic(context, $"Package {package.Name} {package.Version ?? "unknown"}: License '{license}' is NOT acceptable - triggering finding");
					var diagnostic = Diagnostic.Create(
						Rule,
						Location.None,
						package.Name,
						package.Version ?? "unknown",
						license);

					context.ReportDiagnostic(diagnostic);
				}
				else if (!string.IsNullOrEmpty(license))
				{
					ReportDebugDiagnostic(context, $"Package {package.Name} {package.Version ?? "unknown"}: License '{license}' is acceptable - no finding");
				}
			}

			SaveLicenseCache(context, licenseCache, Path.GetDirectoryName(assetsFilePath));
		}

		private string TryFindAssetsFileFromSourcePaths(CompilationAnalysisContext context)
		{
			List<string> sourceDirectories = GetSourceDirectories(context);

			foreach (var sourceDir in sourceDirectories)
			{
				var foundPath = SearchForAssetsFile(context, sourceDir, enableDetailedLogging: false);
				if (foundPath != null)
				{
					return foundPath;
				}
			}

			var currentDir = Directory.GetCurrentDirectory();
			var fallbackPath = SearchForAssetsFile(context, currentDir, enableDetailedLogging: false);
			if (fallbackPath != null)
			{
				return fallbackPath;
			}

			ReportDetailedSearchDiagnostics(context, sourceDirectories, currentDir);
			return null;
		}

		private static List<string> GetSourceDirectories(CompilationAnalysisContext context)
		{
			return context.Compilation.SyntaxTrees
				.Where(tree => !string.IsNullOrEmpty(tree.FilePath))
				.Select(tree => Path.GetDirectoryName(tree.FilePath))
				.Where(dir => !string.IsNullOrEmpty(dir))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private void ReportDetailedSearchDiagnostics(CompilationAnalysisContext context, List<string> sourceDirectories, string currentDir)
		{
			ReportDebugDiagnostic(context, $"Could not find {ProjectAssetsFileName}. Starting detailed search diagnostics...");
			ReportDebugDiagnostic(context, $"Found {sourceDirectories.Count} unique source directories from compilation");

			foreach (var sourceDir in sourceDirectories)
			{
				ReportDebugDiagnostic(context, $"Searching from source directory: {sourceDir}");
				_ = SearchForAssetsFile(context, sourceDir, enableDetailedLogging: true);
			}

			ReportDebugDiagnostic(context, $"Source directory search failed, falling back to current directory: {currentDir}");
			_ = SearchForAssetsFile(context, currentDir, enableDetailedLogging: true);

			ReportDebugDiagnostic(context, $"Exhaustive search completed, {ProjectAssetsFileName} not found");
		}

		private string SearchForAssetsFile(CompilationAnalysisContext context, string startDirectory, bool enableDetailedLogging)
		{
			// First, check common locations
			var foundPath = CheckCommonLocations(context, startDirectory, enableDetailedLogging);
			if (foundPath != null)
			{
				return foundPath;
			}

			// If not found, search up the directory tree
			return SearchDirectoryTree(context, startDirectory, enableDetailedLogging);
		}

		private string CheckCommonLocations(CompilationAnalysisContext context, string startDirectory, bool enableDetailedLogging)
		{
			var possiblePaths = new[]
			{
				Path.Combine(startDirectory, "obj", ProjectAssetsFileName),
				Path.Combine(startDirectory, "..", "obj", ProjectAssetsFileName),
				Path.Combine(startDirectory, "..", "..", "obj", ProjectAssetsFileName)
			};

			foreach (var path in possiblePaths)
			{
				try
				{
					var exists = File.Exists(path);
					if (enableDetailedLogging)
					{
						ReportDebugDiagnostic(context, $"Checking path: {path} - Exists: {exists}");
					}

					if (exists)
					{
						return path;
					}
				}
				catch (Exception ex)
				{
					if (enableDetailedLogging)
					{
						ReportDebugDiagnostic(context, $"Exception checking path {path}: {ex.Message}");
					}
				}
			}

			return null;
		}

		private string SearchDirectoryTree(CompilationAnalysisContext context, string startDirectory, bool enableDetailedLogging)
		{
			try
			{
				return SearchUpDirectoryHierarchy(context, startDirectory, enableDetailedLogging);
			}
			catch (Exception ex)
			{
				if (enableDetailedLogging)
				{
					ReportDebugDiagnostic(context, $"Exception during directory tree search: {ex.Message}");
				}
			}

			return null;
		}

		private string SearchUpDirectoryHierarchy(CompilationAnalysisContext context, string startDirectory, bool enableDetailedLogging)
		{
			var searchDir = startDirectory;
			for (var i = 0; i < 5; i++) // Limit search depth
			{
				var foundPath = CheckDirectoryForAssetsFile(context, searchDir, i, enableDetailedLogging);
				if (foundPath != null)
				{
					return foundPath;
				}

				DirectoryInfo parentDir = Directory.GetParent(searchDir);
				if (parentDir == null)
				{
					if (enableDetailedLogging)
					{
						ReportDebugDiagnostic(context, $"Reached root directory at depth {i}, stopping search");
					}
					break;
				}

				searchDir = parentDir.FullName;
			}
			return null;
		}

		private string CheckDirectoryForAssetsFile(CompilationAnalysisContext context, string searchDir, int depth, bool enableDetailedLogging)
		{
			var assetsPath = Path.Combine(searchDir, "obj", ProjectAssetsFileName);

			try
			{
				var exists = File.Exists(assetsPath);
				if (enableDetailedLogging)
				{
					ReportDebugDiagnostic(context, $"Tree search depth {depth}: checking {assetsPath} - Exists: {exists}");
				}

				return exists ? assetsPath : null;
			}
			catch (Exception ex)
			{
				if (enableDetailedLogging)
				{
					ReportDebugDiagnostic(context, $"Exception during tree search at {assetsPath}: {ex.Message}");
				}
				return null;
			}
		}

		private static List<PackageInfo> ParseProjectAssetsFile(string assetsFilePath)
		{
			var packages = new List<PackageInfo>();

			try
			{
				var content = File.ReadAllText(assetsFilePath);

				Match librariesMatch = Regex.Match(content, @"""libraries""\s*:\s*\{", RegexOptions.None, TimeSpan.FromSeconds(30));
				if (!librariesMatch.Success)
				{
					return packages;
				}

				var startIndex = librariesMatch.Index + librariesMatch.Length;
				var braceCount = 1;
				var currentIndex = startIndex;

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

				var librariesContent = content.Substring(startIndex, currentIndex - startIndex - 1);

				MatchCollection packageMatches = Regex.Matches(librariesContent, @"""([^/]+)/([^""]+)""\s*:\s*\{[^}]*""type""\s*:\s*""([^""]+)""[^}]*(?:""path""\s*:\s*""([^""]+)"")?[^}]*\}", RegexOptions.None, TimeSpan.FromSeconds(30));

				foreach (Match match in packageMatches)
				{
					if (match.Groups.Count >= 4)
					{
						packages.Add(new PackageInfo
						{
							Name = match.Groups[1].Value,
							Version = match.Groups[2].Value,
							Type = match.Groups[3].Value,
							Path = match.Groups.Count > 4 ? match.Groups[4].Value : null
						});
					}
				}
			}
			catch (Exception)
			{
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
						// Check if the value contains project URL (separated by |)
						var value = parts[1];
						var valueParts = value.Split('|');

						var licenseInfo = new PackageLicenseInfo
						{
							License = valueParts[0] ?? string.Empty,
							ProjectUrl = valueParts.Length > 1 ? valueParts[1] : string.Empty
						};

						cache[parts[0]] = licenseInfo;
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

		private void SaveLicenseCache(CompilationAnalysisContext context, Dictionary<string, PackageLicenseInfo> cache, string projectDirectory)
		{
			if (string.IsNullOrEmpty(projectDirectory))
			{
				return;
			}

			try
			{
				var cacheFilePath = Path.Combine(projectDirectory, LicensesCacheFileName);
				IEnumerable<string> lines = cache.Select(kvp =>
				{
					var license = kvp.Value.License ?? string.Empty;
					var projectUrl = kvp.Value.ProjectUrl ?? string.Empty;
					// Use | as delimiter to separate license from project URL
					return string.IsNullOrEmpty(projectUrl)
						? $"{kvp.Key}={license}"
						: $"{kvp.Key}={license}|{projectUrl}";
				});
				File.WriteAllLines(cacheFilePath, lines);
			}
			catch (Exception ex)
			{
				ReportDebugDiagnostic(context, $"Failed to write license cache: {ex.Message}");
			}
		}

		private static PackageLicenseInfo GetPackageLicenseInfo(PackageInfo package, Dictionary<string, PackageLicenseInfo> licenseCache)
		{
			var cacheKey = $"{package.Name}#{package.Version}";

			// Check cache first
			if (licenseCache.TryGetValue(cacheKey, out PackageLicenseInfo cachedInfo))
			{
				// Ensure cached URLs are normalized to fix legacy cache entries
				// that may contain non-normalized URLs from before the fix
				var normalizedCachedLicense = NormalizeCachedLicenseUrl(cachedInfo.License);
				if (normalizedCachedLicense != cachedInfo.License)
				{
					// Update cache with normalized value
					cachedInfo.License = normalizedCachedLicense;
				}

				// Also normalize project URL if present
				if (!string.IsNullOrEmpty(cachedInfo.ProjectUrl))
				{
					var normalizedProjectUrl = NormalizeCachedLicenseUrl(cachedInfo.ProjectUrl);
					if (normalizedProjectUrl != cachedInfo.ProjectUrl)
					{
						cachedInfo.ProjectUrl = normalizedProjectUrl;
					}
				}

				return cachedInfo;
			}

			// Try to get license info from package metadata in global packages cache
			PackageLicenseInfo licenseInfo = ExtractLicenseFromGlobalPackages(package);

			// Cache the result (even if empty)
			licenseCache[cacheKey] = licenseInfo;

			return licenseInfo;
		}

		private static PackageLicenseInfo ExtractLicenseFromGlobalPackages(PackageInfo package)
		{
			var globalPackagesPath = GetGlobalPackagesPath();
			if (string.IsNullOrEmpty(globalPackagesPath))
			{
				return new PackageLicenseInfo();
			}

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
				return new PackageLicenseInfo();
			}

			var nuspecPath = Path.Combine(packagePath, $"{package.Name}.nuspec");
			if (File.Exists(nuspecPath))
			{
				return ExtractLicenseInfoFromNuspec(nuspecPath);
			}

			return new PackageLicenseInfo();
		}

		private static string GetGlobalPackagesPath()
		{
			var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var defaultPath = Path.Combine(userProfile, ".nuget", "packages");

			if (Directory.Exists(defaultPath))
			{
				return defaultPath;
			}

			var nugetPackagesEnv = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
			if (!string.IsNullOrEmpty(nugetPackagesEnv) && Directory.Exists(nugetPackagesEnv))
			{
				return nugetPackagesEnv;
			}

			return defaultPath;
		}

		private static PackageLicenseInfo ExtractLicenseInfoFromNuspec(string nuspecPath)
		{
			try
			{
				var content = File.ReadAllText(nuspecPath);
				return ExtractLicenseInfoFromNuspecContent(content);
			}
			catch (Exception)
			{
				return new PackageLicenseInfo();
			}
		}

		// Public method for testing complete license info extraction logic
		public static PackageLicenseInfo ExtractLicenseInfoFromNuspecContent(string content)
		{
			var licenseInfo = new PackageLicenseInfo
			{
				License = ExtractLicenseFromNuspecContent(content),
				ProjectUrl = ExtractProjectUrlFromNuspecContent(content)
			};
			return licenseInfo;
		}

		// Public method for testing license extraction logic
		public static string ExtractLicenseFromNuspecContent(string content)
		{
			var licenseFromElement = ExtractLicenseElement(content);
			if (!string.IsNullOrEmpty(licenseFromElement))
			{
				return licenseFromElement;
			}

			return ExtractLicenseUrl(content);
		}

		// Public method for testing project URL extraction logic
		public static string ExtractProjectUrlFromNuspecContent(string content)
		{
			return ExtractProjectUrl(content);
		}

		private static string ExtractProjectUrl(string content)
		{
			var projectUrlStart = content.IndexOf("<projectUrl>", StringComparison.OrdinalIgnoreCase);
			if (projectUrlStart < 0)
			{
				return null;
			}

			projectUrlStart += 12; // Skip '<projectUrl>'
			var projectUrlEnd = content.IndexOf("</projectUrl>", projectUrlStart, StringComparison.OrdinalIgnoreCase);
			if (projectUrlEnd <= projectUrlStart)
			{
				return null;
			}

			var projectUrl = content.Substring(projectUrlStart, projectUrlEnd - projectUrlStart).Trim();
			// Normalize project URL using the same logic as license URLs
			return NormalizeLicenseUrl(projectUrl);
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
			if (licenseType.Equals("expression", StringComparison.OrdinalIgnoreCase))
			{
				// Extract SPDX expression
				var contentStart = content.IndexOf(">", licenseStart) + 1;
				var contentEnd = content.IndexOf("</license>", contentStart, StringComparison.OrdinalIgnoreCase);
				if (contentEnd <= contentStart)
				{
					return null;
				}

				return content.Substring(contentStart, contentEnd - contentStart).Trim();
			}
			else if (licenseType.Equals("file", StringComparison.OrdinalIgnoreCase))
			{
				// For type="file", we can't determine the actual license content from just the file path
				// Return a special value to indicate this is an unknown license that should trigger a finding
				return "UNKNOWN_FILE_LICENSE";
			}

			// Unknown license type
			return null;
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

			// Return the URL itself but try to normalize it to match the acceptable licenses format
			// Remove common prefixes to match the format used in DefaultAcceptableLicenses
			var normalizedUrl = NormalizeLicenseUrl(licenseUrl);
			return normalizedUrl;
		}

		private static string NormalizeLicenseUrl(string licenseUrl)
		{
			if (string.IsNullOrEmpty(licenseUrl))
			{
				return licenseUrl;
			}

			// Remove common URL prefixes to match the format in DefaultAcceptableLicenses
			var normalized = licenseUrl;

			// Remove https:// prefix
			if (normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				normalized = normalized.Substring(8);
			}
			// Remove http:// prefix
			else if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
			{
				normalized = normalized.Substring(7);
			}

			return normalized;
		}

		private static string NormalizeCachedLicenseUrl(string cachedLicense)
		{
			if (string.IsNullOrEmpty(cachedLicense))
			{
				return cachedLicense;
			}

			// Handle cached URLs that may have been stored before URL normalization was implemented
			// Only normalize if it looks like a URL (contains known URL patterns)
			if (cachedLicense.Contains("://"))
			{
				// Apply the same normalization logic used for fresh extractions
				return NormalizeLicenseUrl(cachedLicense);
			}

			// Also handle special license identifiers that are already normalized
			// but might need consistency checks
			return cachedLicense;
		}

		private static bool HasSubstringIgnoreCase(string source, string value)
		{
			return source.ToUpper(CultureInfo.InvariantCulture).Contains(value.ToUpper(CultureInfo.InvariantCulture));
		}


		private bool IsLicenseAcceptable(CompilationAnalysisContext context, string license, string projectUrl, HashSet<string> allowedLicenses)
		{
			if (string.IsNullOrEmpty(license))
			{
				ReportDebugDiagnostic(context, $"License is null or empty - not acceptable");
				return false;
			}

			// First check if the license itself is acceptable
			var isLicenseAcceptable = allowedLicenses.Contains(license);
			ReportDebugDiagnostic(context, $"Checking license '{license}' against {allowedLicenses.Count} allowed licenses: {(isLicenseAcceptable ? "FOUND" : "NOT FOUND")}");

			// If license is already acceptable, return true
			if (isLicenseAcceptable)
			{
				return true;
			}

			// If license is not acceptable by itself, also check if projectUrl is in the allowed list
			// This handles cases where two packages have the same license file but different project URLs
			if (!string.IsNullOrEmpty(projectUrl))
			{
				var isProjectUrlAcceptable = allowedLicenses.Contains(projectUrl);
				ReportDebugDiagnostic(context, $"License not found, checking project URL '{projectUrl}': {(isProjectUrlAcceptable ? "FOUND" : "NOT FOUND")}");

				if (isProjectUrlAcceptable)
				{
					return true;
				}
			}

			// For debugging, show first few allowed licenses to help troubleshoot
			var first5Licenses = string.Join(", ", allowedLicenses.Take(5));
			ReportDebugDiagnostic(context, $"Sample allowed licenses: {first5Licenses}...");

			return false;
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
						// Normalize the license from the file to handle both prefixed and non-prefixed URLs
						// This allows users to include https:// and http:// prefixes in their Allowed.Licenses.txt files
						var normalizedLicense = NormalizeLicenseUrl(license);
						_ = allowedLicenses.Add(normalizedLicense);

						// Also add the original license in case it's not a URL but a license identifier
						if (normalizedLicense != license)
						{
							_ = allowedLicenses.Add(license);
						}
					}
				}
			}

			return allowedLicenses;
		}

		public sealed class PackageLicenseInfo
		{
			public string License { get; set; } = string.Empty;
			public string ProjectUrl { get; set; } = string.Empty;
		}
	}
}
