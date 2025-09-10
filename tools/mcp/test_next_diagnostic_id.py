#!/usr/bin/env python3
"""
Test script specifically for the next_diagnosticId functionality

© 2025 Koninklijke Philips N.V. See License.md in the project root for license information.
"""

import sys
import unittest
from pathlib import Path

# Add the mcp directory to the path
sys.path.insert(0, str(Path(__file__).parent))

from mcp_server import _parse_diagnostic_ids_from_csharp, BASE_DIR


class TestNextDiagnosticId(unittest.TestCase):
    
    def test_parse_simple_enum(self):
        """Test parsing a simple C# enum."""
        code = """
        public enum DiagnosticId
        {
            None = 0,
            TestMethodName = 2000,
            EmptyXmlComments = 2001,
            AssertAreEqual = 2003,
        }
        """
        
        ids = _parse_diagnostic_ids_from_csharp(code)
        expected = {2000, 2001, 2003}  # 0 is filtered out as < 2000
        self.assertEqual(ids, expected)
    
    def test_parse_complex_enum(self):
        """Test parsing enum with various formatting."""
        code = """
        public enum DiagnosticId
        {
            None = 0,
            TestMethodName = 2000,
            EmptyXmlComments = 2001,
            AssertAreEqual = 2003,
            ExpectedExceptionAttribute = 2004,
            TestContext = 2005,
            NamespaceMatchFilePath = 2006,
            // Comment line
            AssertAreEqualTypesMatch = 2008,
            AssertIsEqual = 2009,
            AssertIsTrueParenthesis = 2010,
        }
        """
        
        ids = _parse_diagnostic_ids_from_csharp(code)
        expected = {2000, 2001, 2003, 2004, 2005, 2006, 2008, 2009, 2010}
        self.assertEqual(ids, expected)
    
    def test_parse_actual_file(self):
        """Test parsing the actual DiagnosticId.cs file."""
        main_diagnostic_file = BASE_DIR / "Philips.CodeAnalysis.Common" / "DiagnosticId.cs"
        self.assertTrue(main_diagnostic_file.exists(), "DiagnosticId.cs should exist")
        
        content = main_diagnostic_file.read_text(encoding='utf-8')
        ids = _parse_diagnostic_ids_from_csharp(content)
        
        # Basic sanity checks
        self.assertGreater(len(ids), 100, "Should have many diagnostic IDs")
        self.assertIn(2000, ids, "Should include TestMethodName = 2000")
        self.assertIn(2159, ids, "Should include AvoidUnnecessaryAttributeParentheses = 2159")
        
        # Check range
        max_id = max(ids)
        min_id = min(ids)
        self.assertGreaterEqual(min_id, 2000, "All IDs should be >= 2000")
        self.assertEqual(max_id, 2159, "Current max ID should be 2159")
    
    def test_next_id_calculation(self):
        """Test next ID calculation logic."""
        # Test with known IDs
        test_ids = {2000, 2001, 2003, 2005, 2159}
        max_id = max(test_ids)
        next_id = max_id + 1
        
        self.assertEqual(next_id, 2160)
    
    def test_parse_empty_content(self):
        """Test parsing empty or invalid content."""
        empty_ids = _parse_diagnostic_ids_from_csharp("")
        self.assertEqual(len(empty_ids), 0)
        
        invalid_ids = _parse_diagnostic_ids_from_csharp("not valid C# code")
        self.assertEqual(len(invalid_ids), 0)
    
    def test_parse_with_comments_and_whitespace(self):
        """Test parsing with various comment styles and whitespace."""
        code = """
        public enum DiagnosticId
        {
            // This is a comment
            None = 0,
            
            /* Multi-line
               comment */
            TestMethodName = 2000,
            
            EmptyXmlComments = 2001,  // Inline comment
            
            /* Another comment */ AssertAreEqual = 2003,
        }
        """
        
        ids = _parse_diagnostic_ids_from_csharp(code)
        expected = {2000, 2001, 2003}  # 0 is filtered out
        self.assertEqual(ids, expected)


def run_tests():
    """Run all tests and return True if they pass."""
    print("🧪 Testing next_diagnosticId functionality")
    print("=" * 50)
    
    # Create a test suite
    suite = unittest.TestLoader().loadTestsFromTestCase(TestNextDiagnosticId)
    
    # Run tests with detailed output
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    # Print summary
    if result.wasSuccessful():
        print("\n🎉 All tests passed!")
        return True
    else:
        print(f"\n❌ {len(result.failures)} test(s) failed, {len(result.errors)} error(s)")
        return False


if __name__ == "__main__":
    success = run_tests()
    sys.exit(0 if success else 1)