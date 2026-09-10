# Code Fixer Implementation Guide

This guide provides specific technical recommendations for implementing code fixers for the 20 analyzers currently lacking them in the Philips Roslyn Analyzers repository.

## Priority Implementation Order

Based on complexity analysis and implementation viability, here's the recommended implementation order:

### Phase 1: High Priority (8 analyzers, 1-3 weeks)

#### 1. NoProtectedFieldsAnalyzer (PH2070) - **IMMEDIATE PRIORITY**
- **Viability Score**: 9/10
- **Effort**: 1-2 hours
- **Implementation**: Simple modifier replacement
```csharp
// Replace 'protected' with 'private' in field declarations
var newModifiers = node.Modifiers.Replace(protectedToken, SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
return node.WithModifiers(newModifiers);
```

#### 2. NoRegionsInMethodAnalyzer (PH2081) - **IMMEDIATE PRIORITY**  
- **Viability Score**: 7/10
- **Effort**: 1-2 hours
- **Implementation**: Remove region directives within methods
```csharp
// Remove #region and #endregion directives
var newMethod = method.RemoveNodes(regionDirectives, SyntaxRemoveOptions.KeepNoTrivia);
```

#### 3. AvoidVoidReturnAnalyzer (PH2138) - **IMMEDIATE PRIORITY**
- **Viability Score**: 7/10  
- **Effort**: 1-2 hours
- **Implementation**: Change void return type to appropriate type
```csharp
// Change method return type from void to Task for async methods
var newReturnType = SyntaxFactory.IdentifierName("Task");
return method.WithReturnType(newReturnType);
```

#### 4. AvoidPublicMemberVariableAnalyzer (PH2047) - **IMMEDIATE PRIORITY**
- **Viability Score**: 7/10
- **Effort**: 1-2 hours
- **Implementation**: Convert public fields to properties
```csharp
// Transform public field to property with private backing field
var property = SyntaxFactory.PropertyDeclaration(field.Declaration.Type, field.Declaration.Variables[0].Identifier)
    .WithAccessorList(SyntaxFactory.AccessorList(/* get/set accessors */));
```

#### 5. AvoidThrowingUnexpectedExceptionsAnalyzer (PH2122)
- **Viability Score**: 8/10
- **Effort**: 2-3 hours
- **Implementation**: Add proper exception handling or documentation

#### 6. AvoidMagicNumbersAnalyzer (PH2118)
- **Viability Score**: 7/10
- **Effort**: 2-4 hours  
- **Implementation**: Extract magic numbers to named constants

#### 7. SetPropertiesInAnyOrderAnalyzer (PH2134)
- **Viability Score**: 7/10
- **Effort**: 2-3 hours
- **Implementation**: Reorder property initializers

#### 8. WinFormsInitializeComponentMustBeCalledOnceAnalyzer (PH2042)
- **Viability Score**: 6/10
- **Effort**: 3-4 hours
- **Implementation**: Add or reorganize InitializeComponent calls

### Phase 2: Medium Priority (5 analyzers, 1-2 weeks)

#### 9. VariableNamingConventionAnalyzer (PH2030)
- **Viability Score**: 5/10
- **Effort**: 4-6 hours
- **Implementation**: Rename variables to follow conventions
- **Challenge**: Must handle all references

#### 10. PreventUseOfGotoAnalyzer (PH2068)
- **Viability Score**: 5/10
- **Effort**: 4-8 hours
- **Implementation**: Replace goto with structured control flow
- **Challenge**: Complex control flow refactoring

#### 11. LimitConditionComplexityAnalyzer (PH2092)
- **Viability Score**: 5/10
- **Effort**: 6-8 hours
- **Implementation**: Extract complex conditions to variables

#### 12. CastCompleteObjectAnalyzer (PH2119)
- **Viability Score**: 5/10
- **Effort**: 3-5 hours
- **Implementation**: Replace partial casts with complete object casts

#### 13. LockObjectsMustBeReadonlyAnalyzer (PH2066)
- **Viability Score**: 5/10
- **Effort**: 2-4 hours
- **Implementation**: Add readonly modifier to lock objects

### Phase 3: Lower Priority (7 analyzers, 2-4 weeks)

These analyzers have moderate complexity but lower immediate impact:

- OrderPropertyAccessorsAnalyzer (PH2085)
- CopyrightPresentAnalyzer (PH2028)
- NamespaceMatchFilePathAnalyzer (PH2006)
- NamespaceMatchAssemblyNameAnalyzer (PH2135)
- NamespacePrefixAnalyzer (PH2079)
- NoSpaceInFilenameAnalyzer (PH2087)
- UnmanagedObjectsNeedDisposingAnalyzer (PH2133)

## Implementation Templates

### Basic Code Fixer Template
```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SampleCodeFixProvider))]
public sealed class SampleCodeFixProvider : SingleDiagnosticCodeFixProvider<SyntaxNode>
{
    protected override string Title => "Fix description";
    protected override DiagnosticId DiagnosticId => DiagnosticId.SampleRule;
    
    protected override SyntaxNode ApplyFix(SyntaxNode node, ITypeSymbol typeSymbol)
    {
        // Apply the fix transformation
        return node; // Return transformed node
    }
    
    protected override bool IsValidToFix(SyntaxNode node, ITypeSymbol typeSymbol)
    {
        // Validate if the fix should be applied
        return true;
    }
}
```

### Modifier Replacement Template
```csharp
protected override SyntaxNode ApplyFix(FieldDeclarationSyntax field, ITypeSymbol typeSymbol)
{
    var modifiers = field.Modifiers;
    var publicToken = modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PublicKeyword));
    
    if (!publicToken.IsKind(SyntaxKind.None))
    {
        var privateToken = SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
            .WithTriviaFrom(publicToken);
        var newModifiers = modifiers.Replace(publicToken, privateToken);
        return field.WithModifiers(newModifiers);
    }
    
    return field;
}
```

### Region Removal Template
```csharp
protected override SyntaxNode ApplyFix(MethodDeclarationSyntax method, ITypeSymbol typeSymbol)
{
    var regionDirectives = method.DescendantTrivia()
        .Where(t => t.IsKind(SyntaxKind.RegionDirectiveTrivia) || 
                   t.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
        .ToList();
        
    if (regionDirectives.Any())
    {
        var newMethod = method.ReplaceTrivia(regionDirectives, (original, updated) => 
            SyntaxFactory.Whitespace(""));
        return newMethod;
    }
    
    return method;
}
```

## Testing Strategy

### Test Structure
```csharp
[TestMethod]
public void SampleCodeFix_FixesIssue()
{
    const string testCode = @"
public class Test
{
    protected int _field; // Should be private
}";

    const string expectedCode = @"
public class Test
{
    private int _field; // Fixed
}";

    VerifyCodeFix(testCode, expectedCode);
}

[TestMethod]
public void SampleCodeFix_DoesNotAffectValidCode()
{
    const string validCode = @"
public class Test
{
    private int _field; // Already correct
}";

    VerifyNoCodeFix(validCode);
}
```

### Edge Cases to Test
1. **Multiple violations in same file**
2. **Partial classes**
3. **Nested types**
4. **Generic types**
5. **Async methods**
6. **Properties vs fields**
7. **Static vs instance members**

## Implementation Guidelines

### Do's
- ✅ Start with simplest cases (modifier changes)
- ✅ Use existing base classes (`SingleDiagnosticCodeFixProvider`)
- ✅ Test extensively with edge cases
- ✅ Follow existing patterns in the codebase
- ✅ Use `WithTriviaFrom()` to preserve formatting
- ✅ Handle partial classes correctly

### Don'ts
- ❌ Don't implement complex semantic transformations initially
- ❌ Don't break existing functionality
- ❌ Don't ignore accessibility rules
- ❌ Don't modify unrelated code
- ❌ Don't assume single occurrence per file

### Common Pitfalls
1. **Trivia handling**: Always preserve comments and formatting
2. **Multiple declarations**: Handle fields with multiple variables
3. **Scope issues**: Ensure renamed variables don't conflict
4. **Async contexts**: Be careful with async/await patterns
5. **Performance**: Avoid expensive operations in IsValidToFix

## Estimated ROI

### High-Priority Implementations (Phase 1)
- **Development time**: 20-30 hours
- **Potential issues fixed**: ~500-1000 per large codebase
- **Developer time saved**: 10-20 hours per project
- **Payback period**: 1-2 sprints

### Medium-Priority Implementations (Phase 2)
- **Development time**: 40-60 hours  
- **Additional coverage**: ~15% improvement
- **Long-term maintenance**: Reduced manual review time

## Conclusion

Implementing code fixers for these 20 analyzers will:
1. Increase code fixer coverage from 77.8% to 100%
2. Significantly improve developer experience
3. Reduce manual code review overhead
4. Accelerate adoption of coding standards

The phased approach ensures quick wins while building towards comprehensive coverage.