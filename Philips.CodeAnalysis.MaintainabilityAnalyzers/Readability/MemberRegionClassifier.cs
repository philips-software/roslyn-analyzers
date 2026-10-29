// © 2026 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Readability
{
	public static class MemberRegionClassifier
	{
		public const string NonPublicDataMembers = "Non-Public Data Members";
		public const string NonPublicPropertiesAndMethods = "Non-Public Properties/Methods";
		public const string PublicInterface = "Public Interface";
		public const string TestMethods = "Test Methods";
		public const string TestMocks = "Test Mocks";
		public const string TestClasses = "Test Classes";

		public static IReadOnlyList<string> RegionOrder { get; } =
		[
			TestMocks,
			NonPublicDataMembers,
			NonPublicPropertiesAndMethods,
			PublicInterface,
			TestMethods,
			TestClasses,
		];

		public static bool IsTestClass(TypeDeclarationSyntax type)
		{
			return type is ClassDeclarationSyntax && HasAttributeEndingWith(type.AttributeLists, "TestClass");
		}

		public static string GetExpectedRegion(MemberDeclarationSyntax member, bool isTestClass)
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
		public static bool TryGetCurrentRegions(TypeDeclarationSyntax type, out List<IReadOnlyCollection<string>> regionsPerMember)
		{
			regionsPerMember = [];
			var openRegions = new Stack<string>();
			foreach (MemberDeclarationSyntax member in type.Members)
			{
				if (!TrackDirectives(member.GetLeadingTrivia(), openRegions))
				{
					return false;
				}
				regionsPerMember.Add(openRegions.ToArray());
			}

			return TrackDirectives(type.CloseBraceToken.LeadingTrivia, openRegions) && openRegions.Count == 0;
		}

		public static string GetMemberName(MemberDeclarationSyntax member)
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

		private static bool TrackDirectives(SyntaxTriviaList trivia, Stack<string> openRegions)
		{
			foreach (SyntaxTrivia t in trivia)
			{
				if (t.IsKind(SyntaxKind.RegionDirectiveTrivia))
				{
					openRegions.Push(GetRegionName((RegionDirectiveTriviaSyntax)t.GetStructure()));
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

		private static string GetRegionName(RegionDirectiveTriviaSyntax region)
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
