﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

namespace Philips.CodeAnalysis.Common
{

	public enum DiagnosticIds
	{
		TestMethodName = 2000,
		EmptyXmlComments = 2001,
		AssertAreEqual = 2003,
		ExpectedExceptionAttribute = 2004,
		TestContext = 2005,
		NamespaceMatchAssembly = 2006,
		AssertAreEqualTypesMatch = 2008,
		AssertIsEqual = 2009,
		AssertIsTrueParenthesis = 2010,
		AvoidDescriptionAttribute = 2011,
		TestHasTimeoutAttribute = 2012,
		AvoidIgnoreAttribute = 2013,
		AvoidOwnerAttribute = 2014,
		TestHasCategoryAttribute = 2015,
		AvoidTestInitializeMethod = 2016,
		AvoidClassInitializeMethod = 2017,
		AvoidClassCleanupMethod = 2018,
		AvoidTestCleanupMethod = 2019,
		AvoidThreadSleep = 2020,
		AvoidInlineNew = 2021,
		AvoidSuppressMessage = 2026,
		AvoidStaticMethods = 2027,
		CopyrightPresent = 2028,
		AvoidPragma = 2029,
		VariableNamingConventions = 2030,
		AvoidTryParseWithoutCulture = 2031,
		AvoidEmptyTypeInitializer = 2032,
		DataTestMethodsHaveDataRows = 2033,
		TestMethodsMustBeInTestClass = 2034,
		TestMethodsMustHaveTheCorrectNumberOfArguments = 2035,
		TestMethodsMustBePublic = 2036,
		TestMethodsMustHaveUniqueNames = 2037,
		TestClassesMustBePublic = 2038,
		ServiceContractsMustHaveOperationContractAttributes = 2040,
		AvoidMsFakes = 2041,
		InitializeComponentMustBeCalledOnce = 2042,
		DynamicKeywordProhibited = 2044,
		AvoidStaticClasses = 2045,
		AvoidPublicMemberVariables = 2047,
		MockArgumentsMustMatchConstructor = 2048,
		TestMethodsMustNotBeEmpty = 2050,
		PreventUncessaryRangeChecks = 2051,
		MockRaiseArgumentsMustMatchEvent = 2053,
		MockRaiseArgumentCountMismatch = 2054,
		AssertIsTrueLiteral = 2055,
		AssertAreEqualLiteral = 2056,
		AvoidNonConstStrings = 2057,
		AvoidAssertConditionalAccess = 2058,
		TestClassPublicMethodShouldBeTestMethod = 2059,
		EnforceBoolNamingConvention = 2060,
		EnforceRegions = 2061,
		EnforceNonDuplicateRegion = 2064,
		NonCheckedRegionMember = 2065,
		LocksShouldBeReadonly = 2066,
		NoNestedStringFormats = 2067,
		GotoNotAllowed = 2068,
		NoUnnecessaryStringFormats = 2069,
		NoProtectedFields = 2070,
		AvoidDuplicateCode = 2071,
		EnforceEditorConfig = 2072,
		ExtensionMethodsCalledLikeInstanceMethods = 2073,
		DisallowDisposeRegistration = 2074,
		AvoidAssemblyVersionChange = 2075,
		AssertFail = 2076,
		AvoidSwitchStatementsWithNoCases = 2077,
		AvoidPrivateKeyProperty = 2078,
		NamespacePrefix = 2079,
		NoHardcodedPaths = 2080,
		NoRegionsInMethods = 2081,
		PositiveNaming = 2082,
		AvoidPassByReference = 2083,
		DontLockNewObject = 2084,
		OrderPropertyAccessors = 2085,
		AvoidTaskResult = 2086,
		NoSpaceInFilename = 2087,
		LimitPathLength = 2088,
		AvoidAssignmentInCondition = 2089,
		LogException = 2090,
		ThrowInnerException = 2091,
		LimitConditionComplexity = 2092,
		PreferTuplesWithNamedFields = 2093,
		PreferUsingNamedTupleField = 2094,
		TestMethodsMustHaveValidReturnType = 2095,
		AvoidAsyncVoid = 2096,
		AvoidEmptyStatementBlock = 2097,
		AvoidEmptyCatchBlock = 2098,
		EnforceFileVersionIsSameAsPackageVersion = 2099,
		AvoidPasswordField = 2100,
		DereferenceNull = 2101,
		XmlDocumentationShouldAddValue = 2102,
		AvoidInvocationAsArgument = 2103,
		EveryLinqStatementOnSeparateLine = 2104,
		ReduceCognitiveLoad = 2105,
	}
}
