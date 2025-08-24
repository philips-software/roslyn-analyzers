// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnforceRegionsAnalyzer : DiagnosticAnalyzerBase
	{
		private const string RegionTag = "#region";

		private const string EnforceRegionTitle = @"Enforce Regions";
		public const string EnforceRegionMessageFormat = @"Member doesn't belong to region {0}";
		private const string EnforceRegionDescription = @"Given member doesn't belong to region {0}. Change the method's location to its correct region";
		private const string EnforceRegionCategory = Categories.Readability;

		private const string EnforceNonDuplicateRegionTitle = @"Enforce Non Duplicate Regions";
		public const string EnforceNonDuplicateRegionMessageFormat = @"Multiple instances of {0}";
		private const string EnforceNonDuplicateRegionDescription = @"A Class cannot have regions of the same name";
		private const string EnforceNonDuplicateRegionCategory = Categories.Readability;

		private const string NonCheckedRegionMemberTitle = @"Members location relative to regions (Enforce Region Analyzer).";
		public const string NonCheckedRegionMemberMessageFormat = @"Member's location relative to region {0} should be verified in Enforce Regions Analyzer";
		private const string NonCheckedRegionMemberDescription = @"Member's location relative to region {0} should be verified. Implement the check in enforce region analyer";
		private const string NonCheckedRegionMemberCategory = Categories.Readability;

		private const string AvoidEmptyRegionTitle = @"Avoid empty regions";
		public const string AvoidEmptyRegionMessageFormat = @"Remove the empty region";
		private const string AvoidEmptyRegionDescription = @"Remove the empty region";
		private const string AvoidEmptyRegionCategory = Categories.Readability;

		private const string NonPublicDataMembersRegion = "Non-Public Data Members";
		private const string NonPublicPropertiesAndMethodsRegion = "Non-Public Properties/Methods";
		private const string PublicInterfaceRegion = "Public Interface";
		private const string EmptyRegion = "Empty region";

		private static readonly Dictionary<string, Func<IReadOnlyList<MemberDeclarationSyntax>, SyntaxNodeAnalysisContext, bool>> RegionChecks = new()
		{
			{ NonPublicDataMembersRegion, CheckMembersOfNonPublicDataMembersRegion },
			{ NonPublicPropertiesAndMethodsRegion, CheckMembersOfNonPublicPropertiesAndMethodsRegion },
			{ PublicInterfaceRegion, CheckMembersOfPublicInterfaceRegion }
		};

		private static readonly DiagnosticDescriptor EnforceMemberLocation = new(DiagnosticId.EnforceRegions.ToId(), EnforceRegionTitle,
			EnforceRegionMessageFormat, EnforceRegionCategory, DiagnosticSeverity.Error, isEnabledByDefault: true, description: EnforceRegionDescription);
		private static readonly DiagnosticDescriptor EnforceNonDuplicateRegion = new(DiagnosticId.EnforceNonDuplicateRegion.ToId(),
			EnforceNonDuplicateRegionTitle, EnforceNonDuplicateRegionMessageFormat, EnforceNonDuplicateRegionCategory,
			DiagnosticSeverity.Error, isEnabledByDefault: true, description: EnforceNonDuplicateRegionDescription);
		private static readonly DiagnosticDescriptor NonCheckedMember = new(DiagnosticId.NonCheckedRegionMember.ToId(),
			NonCheckedRegionMemberTitle, NonCheckedRegionMemberMessageFormat, NonCheckedRegionMemberCategory,
			DiagnosticSeverity.Info, isEnabledByDefault: true, description: NonCheckedRegionMemberDescription);
		private static readonly DiagnosticDescriptor AvoidEmpty = new(DiagnosticId.AvoidEmptyRegions.ToId(), AvoidEmptyRegionTitle,
			AvoidEmptyRegionMessageFormat, AvoidEmptyRegionCategory, DiagnosticSeverity.Error, isEnabledByDefault: true, description: AvoidEmptyRegionDescription);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(EnforceMemberLocation, EnforceNonDuplicateRegion, NonCheckedMember, AvoidEmpty);

		protected override void InitializeCompilation(CompilationStartAnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
		}

		private static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var typeDeclaration = (TypeDeclarationSyntax)context.Node;

			RegionVisitor visitor = new();

			visitor.Visit(typeDeclaration);

			IReadOnlyList<DirectiveTriviaSyntax> regions = visitor.Regions;
			if (regions.Count == 0)
			{
				return;
			}
			// Region directives should come in pairs
			if (regions.Count % 2 == 1)
			{
				return;
			}

			// If pair is malformed (eg. #region followed by #region), bail out
			for (var i = 0; i < regions.Count; i += 2)
			{
				DirectiveTriviaSyntax start = regions[i];
				DirectiveTriviaSyntax end = regions[i + 1];

				if (start.IsKind(SyntaxKind.EndRegionDirectiveTrivia) || end.IsKind(SyntaxKind.RegionDirectiveTrivia))
				{
					return;
				}
			}

			Dictionary<string, LocationRangeModel> regionLocations = PopulateRegionLocations(regions, context);

			if (regionLocations.Count == 0)
			{
				return;
			}

			SyntaxList<MemberDeclarationSyntax> members = typeDeclaration.Members;

			// Check if nested classes are present - if so, skip empty region analysis to avoid false positives
			var hasNestedClasses = ContainsNestedClasses(members);

			foreach (KeyValuePair<string, LocationRangeModel> pair in regionLocations)
			{
				// Skip empty region checking when nested classes are present to avoid false positives
				if (!hasNestedClasses && !MemberPresentInRegion(typeDeclaration, pair.Value))
				{
					CheckEmptyRegion(pair.Value, members, context);
				}

				if (RegionChecks.TryGetValue(pair.Key, out Func<IReadOnlyList<MemberDeclarationSyntax>, SyntaxNodeAnalysisContext, bool> functionToCall))
				{
					IReadOnlyList<MemberDeclarationSyntax> membersOfRegion = GetMembersOfRegion(members, pair.Value);
					_ = functionToCall(membersOfRegion, context);
				}
			}

		}

		/// <summary>
		/// Checks if the given list of members contains any nested type declarations (classes, structs, etc.)
		/// </summary>
		/// <param name="members">The list of members to check</param>
		/// <returns>True if nested classes/types are present, false otherwise</returns>
		private static bool ContainsNestedClasses(SyntaxList<MemberDeclarationSyntax> members)
		{
			return members.Any(member =>
				member.IsKind(SyntaxKind.ClassDeclaration) ||
				member.IsKind(SyntaxKind.StructDeclaration) ||
				member.IsKind(SyntaxKind.InterfaceDeclaration));
		}

		private static string GetRegionName(DirectiveTriviaSyntax region)
		{
			var regionName = string.Empty;

			TextLineCollection lines = region.GetText().Lines;

			if (lines.Count > 0)
			{
				regionName = lines[0].ToString();
			}

			if (regionName.StartsWith(RegionTag))
			{
				regionName = regionName.Replace(RegionTag + " ", string.Empty);
			}
			return regionName;
		}

		private static void PopulateRegionLocation(ref string regionStartName, Dictionary<string, LocationRangeModel> regionLocations,
													DirectiveTriviaSyntax region, int i, SyntaxNodeAnalysisContext context)
		{
			if (i % 2 == 0)
			{
				var regionName = GetRegionName(region);
				if (regionName.Length <= 0)
				{
					return;
				}

				if (regionLocations.Remove(regionName))
				{
					Location memberLocation = region.DirectiveNameToken.GetLocation();
					CreateDiagnostic(memberLocation, context, regionName, EnforceNonDuplicateRegion);
				}
				else
				{
					Location location = region.GetLocation();
					Location memberLocation = region.DirectiveNameToken.GetLocation();
					var lineNumber = GetMemberLineNumber(location);

					regionLocations.Add(regionName, new LocationRangeModel(memberLocation, lineNumber, lineNumber));
					regionStartName = regionName;
				}
			}
			else
			{
				if (regionLocations.TryGetValue(regionStartName, out LocationRangeModel value))
				{
					Location location = region.GetLocation();
					value.EndLine = GetMemberLineNumber(location);
					regionStartName = string.Empty;
				}
			}
		}

		/// <summary>
		/// Populate the dictionary with the region name(if we are performing checks on the concerned region) along with the LocationRangeModel
		/// DirectiveTriviaSyntax will have both start region and end region
		/// Start of the region will always come RIGHT before end region in the regions list.
		/// Index position of start of the region will always be 0 or even. While index position of end region will always be odd and RIGHT after corresponding start
		/// region object.
		/// If the region is a start region(at index position 0 or even), add it to the dictionary.
		/// If the region is an end region(at index position odd), look for corresponding start region added in the previous loop and update the endLocation in LocationRangeModel object
		/// </summary>
		/// <param name="regions">Regions found in the file</param>
		/// <param name="context">Tha Analysis context</param>
		/// <returns>Dictionary of region name and LocationRangeModel object</returns>
		private static Dictionary<string, LocationRangeModel> PopulateRegionLocations(IReadOnlyList<DirectiveTriviaSyntax> regions, SyntaxNodeAnalysisContext context)
		{
			Dictionary<string, LocationRangeModel> regionLocations = [];
			var regionStartName = string.Empty;

			for (var i = 0; i < regions.Count; i++)
			{
				DirectiveTriviaSyntax region = regions[i];
				PopulateRegionLocation(ref regionStartName, regionLocations, region, i, context);
			}
			return regionLocations;
		}

		/// <summary>
		/// Filters the list of members based on their location with respect to the given region
		/// </summary>
		/// <param name="members">List of members</param>
		/// <param name="locationRange">LocationRangeModel object</param>
		/// <returns>List of members belonging to the given region</returns>
		private static List<MemberDeclarationSyntax> GetMembersOfRegion(SyntaxList<MemberDeclarationSyntax> members, LocationRangeModel locationRange)
		{
			return members.Where(member => MemberPresentInRegion(member, locationRange)).ToList();
		}

		/// <summary>
		/// Check is member is present in the given region
		/// </summary>
		/// <param name="member"></param>
		/// <param name="locationRange"></param>
		/// <returns>true if member is inside the given region, else false</returns>
		private static bool MemberPresentInRegion(SyntaxNode member, LocationRangeModel locationRange)
		{
			Location location = member.GetLocation();
			var memberLine = GetMemberLineNumber(location);
			// Fixed logic: use >= and < for proper boundary handling
			// This ensures members on the line after the #region are included
			return memberLine >= locationRange.StartLine && memberLine < locationRange.EndLine;
		}

		/// <summary>
		/// Gets the line number with respect to input file based on given location
		/// </summary>
		/// <param name="location"></param>
		/// <returns>Integer, which is the line number</returns>
		private static int GetMemberLineNumber(Location location)
		{
			return location.GetLineSpan().StartLinePosition.Line;
		}

		/// <summary>
		/// Checks whether the members inside the Public Interface region belong there.
		/// </summary>
		private static bool CheckMembersOfPublicInterfaceRegion(IReadOnlyList<MemberDeclarationSyntax> members, SyntaxNodeAnalysisContext context)
		{
			foreach (MemberDeclarationSyntax member in members)
			{
				VerifyMemberForPublicInterfaceRegion(member, context);
			}
			return true;
		}

		/// <summary>
		/// Checks whether the regions are empty, contain no statements.
		/// </summary>
		private static void CheckEmptyRegion(LocationRangeModel locationRange, IReadOnlyList<MemberDeclarationSyntax> members, SyntaxNodeAnalysisContext context)
		{
			var foundMemberInside = members.Any(m => MemberPresentInRegion(m, locationRange));

			if (!foundMemberInside)
			{
				// Empty region
				CreateDiagnostic(locationRange.Location, context, EmptyRegion, AvoidEmpty);
			}
		}

		/// <summary>
		/// Verify whether the given member belongs to the public interface region
		/// If the member is of type field, then throw an error
		/// If the member is of type method, then check if its non-public
		/// </summary>
		private static void VerifyMemberForPublicInterfaceRegion(MemberDeclarationSyntax member, SyntaxNodeAnalysisContext context)
		{
			if (TryGetModifiers(member, true, out SyntaxTokenList modifiers))
			{
				Location memberLocation = member.GetLocation();
				if (!HasAccessModifier(modifiers) || !MemberIsPublic(modifiers))
				{
					CreateDiagnostic(memberLocation, context, PublicInterfaceRegion, EnforceMemberLocation);
				}
				return;
			}

			if (member.Kind() != SyntaxKind.StructDeclaration)
			{
				Location memberLocation = member.GetLocation();
				CreateDiagnostic(memberLocation, context, PublicInterfaceRegion, NonCheckedMember);
			}
		}

		private static bool TryGetModifiers(MemberDeclarationSyntax member, bool isDetailed, out SyntaxTokenList modifiers)
		{
			// Field
			// Delegate
			// Enum
			modifiers = default;
			var shouldCheck = false;
			switch (member.Kind())
			{
				case SyntaxKind.ConstructorDeclaration:
					modifiers = ((ConstructorDeclarationSyntax)member).Modifiers;
					shouldCheck = true;
					break;
				case SyntaxKind.ClassDeclaration:
					modifiers = ((ClassDeclarationSyntax)member).Modifiers;
					shouldCheck = true;
					break;
				case SyntaxKind.FieldDeclaration:
					modifiers = ((FieldDeclarationSyntax)member).Modifiers;
					shouldCheck = isDetailed;
					break;
				case SyntaxKind.MethodDeclaration:
					modifiers = ((MethodDeclarationSyntax)member).Modifiers;
					shouldCheck = true;
					break;
				case SyntaxKind.DelegateDeclaration:
					modifiers = ((DelegateDeclarationSyntax)member).Modifiers;
					shouldCheck = isDetailed;
					break;
				case SyntaxKind.PropertyDeclaration:
					modifiers = ((PropertyDeclarationSyntax)member).Modifiers;
					shouldCheck = true;
					break;
				case SyntaxKind.EventDeclaration:
					modifiers = ((EventDeclarationSyntax)member).Modifiers;
					shouldCheck = true;
					break;
				case SyntaxKind.EventFieldDeclaration:
					modifiers = ((EventFieldDeclarationSyntax)member).Modifiers;
					shouldCheck = true;
					break;
				case SyntaxKind.OperatorDeclaration:
					modifiers = ((OperatorDeclarationSyntax)member).Modifiers;
					shouldCheck = true;
					break;
				case SyntaxKind.DestructorDeclaration:
					modifiers = ((DestructorDeclarationSyntax)member).Modifiers;
					shouldCheck = true;
					break;
				case SyntaxKind.IndexerDeclaration:
					modifiers = ((IndexerDeclarationSyntax)member).Modifiers;
					shouldCheck = true;
					break;
				case SyntaxKind.EnumDeclaration:
					modifiers = ((EnumDeclarationSyntax)member).Modifiers;
					shouldCheck = isDetailed;
					break;
			}

			return shouldCheck;
		}

		/// <summary>
		/// Create a diagnostic rule for the analyzer
		/// </summary>
		private static void CreateDiagnostic(Location memberLocation, SyntaxNodeAnalysisContext context, string regionName, DiagnosticDescriptor rule)
		{
			var diagnostic = Diagnostic.Create(rule, memberLocation, regionName);
			context.ReportDiagnostic(diagnostic);
		}

		/// <summary>
		/// Check whether the members inside Non-Public Properties/Methods region belong there
		/// </summary>
		/// <returns>Dummy return</returns>
		private static bool CheckMembersOfNonPublicPropertiesAndMethodsRegion(IReadOnlyList<MemberDeclarationSyntax> members, SyntaxNodeAnalysisContext context)
		{
			foreach (MemberDeclarationSyntax member in members)
			{
				VerifyMemberForNonPublicPropertiesAndMethods(member, context);
			}
			return true;
		}

		/// <summary>
		/// Verify whether member belogns to Non-public Properties/Methods region
		/// if memeber is of type field, then verify if the field is non-public
		/// if member is of type method, then verify if the method is non-public
		/// </summary>
		/// <param name="member"></param>
		/// <param name="context"></param>
		private static void VerifyMemberForNonPublicPropertiesAndMethods(MemberDeclarationSyntax member, SyntaxNodeAnalysisContext context)
		{
			Location memberLocation = member.GetLocation();
			if (TryGetModifiers(member, false, out SyntaxTokenList modifiers))
			{
				if (!HasAccessModifier(modifiers))
				{
					return;
				}
				else if (MemberIsPublic(modifiers))
				{
					CreateDiagnostic(memberLocation, context, NonPublicPropertiesAndMethodsRegion, EnforceMemberLocation);
				}
				return;
			}

			if (member.Kind() == SyntaxKind.StructDeclaration)
			{
				return;
			}

			switch (member.Kind())
			{
				case SyntaxKind.FieldDeclaration:
				case SyntaxKind.EnumDeclaration:
				case SyntaxKind.DelegateDeclaration:
					CreateDiagnostic(memberLocation, context, NonPublicPropertiesAndMethodsRegion, EnforceMemberLocation);
					break;
				default:
					CreateDiagnostic(memberLocation, context, NonPublicPropertiesAndMethodsRegion, NonCheckedMember);
					break;
			}
		}

		/// <summary>
		/// Check whether the members of Non-Public Data Members Region belong there
		/// </summary>
		/// <returns>Dummy return</returns>
		private static bool CheckMembersOfNonPublicDataMembersRegion(IReadOnlyList<MemberDeclarationSyntax> members, SyntaxNodeAnalysisContext context)
		{
			foreach (MemberDeclarationSyntax member in members)
			{
				VerifyMemberForNonPublicDataMemberRegion(member, context);
			}
			return true;
		}

		/// <summary>
		/// Verify member belongs to Non-Public Data Member Region
		/// if member is of type method, throw an error
		/// if member is of type field, check if it is non-public
		/// </summary>
		/// <param name="member"></param>
		/// <param name="context"></param>
		private static void VerifyMemberForNonPublicDataMemberRegion(MemberDeclarationSyntax member, SyntaxNodeAnalysisContext context)
		{
			SyntaxTokenList modifiers = default;
			var shouldProcess = false;
			switch (member.Kind())
			{
				case SyntaxKind.FieldDeclaration:
					modifiers = ((FieldDeclarationSyntax)member).Modifiers;
					shouldProcess = true;
					break;
				case SyntaxKind.EnumDeclaration:
					modifiers = ((EnumDeclarationSyntax)member).Modifiers;
					shouldProcess = true;
					break;
				case SyntaxKind.DelegateDeclaration:
					modifiers = ((DelegateDeclarationSyntax)member).Modifiers;
					shouldProcess = true;
					break;
			}

			Location memberLocation = member.GetLocation();
			if (shouldProcess)
			{
				if (!HasAccessModifier(modifiers))
				{
					return;
				}
				else if (MemberIsPublic(modifiers))
				{
					CreateDiagnostic(memberLocation, context, NonPublicDataMembersRegion, EnforceMemberLocation);
				}
				return;
			}

			if (member.Kind() == SyntaxKind.StructDeclaration)
			{
				return;
			}

			switch (member.Kind())
			{
				case SyntaxKind.ConstructorDeclaration:
				case SyntaxKind.MethodDeclaration:
				case SyntaxKind.PropertyDeclaration:
				case SyntaxKind.EventDeclaration:
				case SyntaxKind.EventFieldDeclaration:
				case SyntaxKind.OperatorDeclaration:
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.IndexerDeclaration:
				case SyntaxKind.DestructorDeclaration:
					CreateDiagnostic(memberLocation, context, NonPublicDataMembersRegion, EnforceMemberLocation);
					break;
				default:
					CreateDiagnostic(memberLocation, context, NonPublicDataMembersRegion, NonCheckedMember);
					break;
			}
		}

		public static bool HasAccessModifier(SyntaxTokenList memberTokens)
		{
			return
				memberTokens.Any(SyntaxKind.PublicKeyword) ||
				memberTokens.Any(SyntaxKind.PrivateKeyword) ||
				memberTokens.Any(SyntaxKind.ProtectedKeyword) ||
				memberTokens.Any(SyntaxKind.InternalKeyword);
		}

		private static bool MemberIsPublic(SyntaxTokenList memberTokens)
		{
			return memberTokens.Any(SyntaxKind.PublicKeyword);
		}

	}
}
