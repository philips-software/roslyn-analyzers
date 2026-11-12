// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	public class MemberRegionClassifier
	{
		public const string NonPublicDataMembers = "Non-Public Data Members";
		public const string NonPublicPropertiesAndMethods = "Non-Public Properties/Methods";
		public const string PublicInterface = "Public Interface";
		public const string TestMethods = "Test Methods";
		public const string TestMocks = "Test Mocks";
		public const string TestClasses = "Test Classes";

		private static readonly string[] Regions =
		[
			TestMocks,
			NonPublicDataMembers,
			NonPublicPropertiesAndMethods,
			PublicInterface,
			TestMethods,
			TestClasses,
		];

		private static readonly Dictionary<string, string> SettingKeys = new()
		{
			{ TestMocks, "test_mocks_region" },
			{ NonPublicDataMembers, "non_public_data_members_region" },
			{ NonPublicPropertiesAndMethods, "non_public_properties_methods_region" },
			{ PublicInterface, "public_interface_region" },
			{ TestMethods, "test_methods_region" },
			{ TestClasses, "test_classes_region" },
		};

		/// <summary>
		/// Maps each region's setting key to its configured name, falling back to the default name.
		/// </summary>
		public ImmutableDictionary<string, string> LoadRegionNames(Func<string, string> readSetting)
		{
			ImmutableDictionary<string, string>.Builder names = ImmutableDictionary.CreateBuilder<string, string>();
			foreach (var region in Regions)
			{
				var configured = readSetting(SettingKeys[region]);
				names.Add(SettingKeys[region], string.IsNullOrWhiteSpace(configured) ? region : configured.Trim());
			}
			return names.ToImmutable();
		}

		public const string AllowedRegionsKey = "allowed_regions";
		public const string RegionOrderKey = "region_order";
		public const string AllowedRegionsSection = "<allowed regions>";

		private static readonly string[] DefaultSectionOrder =
		[
			TestMocks,
			NonPublicDataMembers,
			NonPublicPropertiesAndMethods,
			PublicInterface,
			AllowedRegionsSection,
			TestMethods,
			TestClasses,
		];

		/// <summary>
		/// Parses a digit string (e.g. "12435") where each digit is a 1-based index into the default section order.
		/// Omitted sections follow in default order; invalid input yields the default order.
		/// </summary>
		public IReadOnlyList<string> ParseSectionOrder(string digits)
		{
			var order = new List<string>();
			foreach (var c in (digits ?? string.Empty).Trim())
			{
				var index = c - '1';
				if (index < 0 || index >= DefaultSectionOrder.Length || order.Contains(DefaultSectionOrder[index]))
				{
					return DefaultSectionOrder;
				}
				order.Add(DefaultSectionOrder[index]);
			}

			order.AddRange(DefaultSectionOrder.Where(section => !order.Contains(section)));
			return order;
		}

		public HashSet<string> ParseAllowedRegions(string commaSeparated)
		{
			return new HashSet<string>(
				(commaSeparated ?? string.Empty).Split(',').Select(name => name.Trim()).Where(name => name.Length > 0),
				StringComparer.Ordinal);
		}

		public string GetRegionName(string region, ImmutableDictionary<string, string> names)
		{
			return names != null && names.TryGetValue(SettingKeys[region], out var name) ? name : region;
		}

		public bool IsTestClass(TypeDeclarationSyntax type)
		{
			return type is ClassDeclarationSyntax && HasAttributeEndingWith(type.AttributeLists, "TestClass");
		}

		public string GetExpectedRegion(MemberDeclarationSyntax member, bool isTestClass)
		{
			var isPublic = member.Modifiers.Any(SyntaxKind.PublicKeyword);
			if (isTestClass)
			{
				if (member is MethodDeclarationSyntax method && HasAttributeEndingWith(method.AttributeLists, "TestMethod"))
				{
					return TestMethods;
				}

				if (member is TypeDeclarationSyntax nestedType)
				{
					var isMock = !isPublic && nestedType.Identifier.Text.Contains("Mock");
					return isMock ? TestMocks : TestClasses;
				}
			}

			if (isPublic)
			{
				return PublicInterface;
			}

			return member is (BaseFieldDeclarationSyntax and not EventFieldDeclarationSyntax) or EnumDeclarationSyntax or DelegateDeclarationSyntax
				? NonPublicDataMembers
				: NonPublicPropertiesAndMethods;
		}

		/// <summary>
		/// Determines the region each member currently sits in. Returns false if the member-level directives
		/// cannot be safely reorganized (unbalanced regions, or conditional/pragma directives between members).
		/// </summary>
		public bool TryGetCurrentRegions(TypeDeclarationSyntax type, out List<IReadOnlyCollection<string>> regionsPerMember)
		{
			regionsPerMember = [];
			var openRegions = new Stack<string>();
			foreach (MemberDeclarationSyntax member in type.Members)
			{
				if (!TrackDirectives(member.GetLeadingTrivia(), openRegions) || !HasBalancedInnerDirectives(member))
				{
					return false;
				}
				regionsPerMember.Add(openRegions.ToArray());
			}

			return TrackDirectives(type.CloseBraceToken.LeadingTrivia, openRegions) && openRegions.Count == 0;
		}

		public string GetMemberName(MemberDeclarationSyntax member)
		{
			return member switch
			{
				MethodDeclarationSyntax method => method.Identifier.Text,
				PropertyDeclarationSyntax property => property.Identifier.Text,
				EventDeclarationSyntax eventDeclaration => eventDeclaration.Identifier.Text,
				ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
				BaseTypeDeclarationSyntax nestedType => nestedType.Identifier.Text,
				DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.Identifier.Text,
				BaseFieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.Text,
				_ => member.Kind().ToString(),
			};
		}

		private bool TrackDirectives(SyntaxTriviaList trivia, Stack<string> openRegions)
		{
			foreach (SyntaxTrivia t in trivia)
			{
				if (t.IsKind(SyntaxKind.RegionDirectiveTrivia))
				{
					openRegions.Push(GetDirectiveName((RegionDirectiveTriviaSyntax)t.GetStructure()));
				}
				else if (t.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
				{
					if (openRegions.Count == 0)
					{
						return false;
					}
					_ = openRegions.Pop();
				}
				else if (t.IsDirective || t.IsKind(SyntaxKind.DisabledTextTrivia))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Directives inside a member (after its leading trivia) must open and close within that member, otherwise moving it breaks pairing.
		/// </summary>
		private static bool HasBalancedInnerDirectives(MemberDeclarationSyntax member)
		{
			var ifDepth = 0;
			var regionDepth = 0;
			foreach (SyntaxTrivia t in member.DescendantTrivia().Where(t => t.IsDirective && t.SpanStart >= member.SpanStart))
			{
				switch (t.Kind())
				{
					case SyntaxKind.IfDirectiveTrivia:
						ifDepth++;
						break;
					case SyntaxKind.EndIfDirectiveTrivia:
						ifDepth--;
						break;
					case SyntaxKind.ElifDirectiveTrivia:
					case SyntaxKind.ElseDirectiveTrivia:
						if (ifDepth == 0)
						{
							return false;
						}
						break;
					case SyntaxKind.RegionDirectiveTrivia:
						regionDepth++;
						break;
					case SyntaxKind.EndRegionDirectiveTrivia:
						regionDepth--;
						break;
				}

				if (ifDepth < 0 || regionDepth < 0)
				{
					return false;
				}
			}
			return ifDepth == 0 && regionDepth == 0;
		}

		/// <summary>
		/// Declarations whose relative order affects behavior: initializers run in textual order,
		/// and field order defines the layout of structs and types with an explicit StructLayout.
		/// </summary>
		public bool IsOrderSensitive(MemberDeclarationSyntax member, TypeDeclarationSyntax type)
		{
			if (member.Modifiers.Any(SyntaxKind.ConstKeyword))
			{
				return false;
			}

			switch (member)
			{
				case BaseFieldDeclarationSyntax field:
					var hasInitializer = field.Declaration.Variables.Any(v => v.Initializer != null);
					var isLayoutField = !member.Modifiers.Any(SyntaxKind.StaticKeyword) && HasSequentialLayout(type);
					return hasInitializer || isLayoutField;
				case PropertyDeclarationSyntax property:
					return property.Initializer != null;
				default:
					return false;
			}
		}

		private static bool HasSequentialLayout(TypeDeclarationSyntax type)
		{
			return type is StructDeclarationSyntax || type.IsKind(SyntaxKind.RecordStructDeclaration) || HasAttributeEndingWith(type.AttributeLists, "StructLayout");
		}

		public string GetDirectiveName(RegionDirectiveTriviaSyntax region)
		{
			SyntaxTrivia message = region.EndOfDirectiveToken.LeadingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.PreprocessingMessageTrivia));
			return message.ToString().Trim();
		}

		private static bool HasAttributeEndingWith(SyntaxList<AttributeListSyntax> attributeLists, string suffix)
		{
			foreach (AttributeSyntax attribute in attributeLists.SelectMany(list => list.Attributes))
			{
				var name = attribute.Name switch
				{
					QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
					AliasQualifiedNameSyntax aliased => aliased.Name.Identifier.Text,
					SimpleNameSyntax simple => simple.Identifier.Text,
					_ => string.Empty,
				};

				if (name.EndsWith(suffix, StringComparison.Ordinal) || name.EndsWith(suffix + "Attribute", StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}
	}
}
