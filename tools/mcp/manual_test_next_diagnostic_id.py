#!/usr/bin/env python3
"""
Manual integration test for the next_diagnosticId tool

This demonstrates the tool working end-to-end by manually calling
the core logic without the MCP wrapper.

© 2025 Koninklijke Philips N.V. See License.md in the project root for license information.
"""

import sys
from pathlib import Path

# Add the mcp directory to the path
sys.path.insert(0, str(Path(__file__).parent))

from mcp_server import (
    _parse_diagnostic_ids_from_csharp,
    _get_open_prs,
    _get_file_content_from_pr,
    BASE_DIR
)


def manual_next_diagnostic_id_test():
    """
    Manual test of the next_diagnosticId functionality.
    This replicates exactly what the MCP tool does.
    """
    print("🆔 Testing next_diagnosticId functionality")
    print("=" * 50)
    
    try:
        # Step 1: Get diagnostic IDs from main branch
        print("📁 Step 1: Analyzing main branch DiagnosticId.cs...")
        main_diagnostic_file = BASE_DIR / "Philips.CodeAnalysis.Common" / "DiagnosticId.cs"
        
        if not main_diagnostic_file.exists():
            print("❌ DiagnosticId.cs file not found in main branch")
            return False
        
        main_content = main_diagnostic_file.read_text(encoding='utf-8')
        main_ids = _parse_diagnostic_ids_from_csharp(main_content)
        
        if not main_ids:
            print("❌ No diagnostic IDs found in main branch")
            return False
        
        max_main_id = max(main_ids)
        print(f"✅ Found {len(main_ids)} diagnostic IDs in main branch")
        print(f"   Highest ID: {max_main_id}")
        
        # Step 2: Get open PRs and check for new diagnostic IDs
        print("\n🔍 Step 2: Checking open PRs for new diagnostic IDs...")
        open_prs = _get_open_prs()
        pr_ids = set()
        pr_details = []
        
        print(f"   Found {len(open_prs)} open PRs")
        
        for pr in open_prs:
            pr_number = pr.get('number', 'unknown')
            pr_title = pr.get('title', 'Unknown PR')
            pr_head_sha = pr.get('head', {}).get('sha')
            
            if not pr_head_sha:
                continue
            
            print(f"   Analyzing PR #{pr_number}: {pr_title[:50]}...")
            
            # Get DiagnosticId.cs from this PR
            pr_content = _get_file_content_from_pr(pr_head_sha, "Philips.CodeAnalysis.Common/DiagnosticId.cs")
            
            if pr_content:
                pr_diagnostic_ids = _parse_diagnostic_ids_from_csharp(pr_content)
                
                # Find IDs that are new in this PR (not in main)
                new_ids_in_pr = pr_diagnostic_ids - main_ids
                
                if new_ids_in_pr:
                    pr_ids.update(new_ids_in_pr)
                    pr_details.append({
                        "pr_number": pr_number,
                        "pr_title": pr_title,
                        "new_ids": sorted(list(new_ids_in_pr))
                    })
                    print(f"     🆕 Found new IDs: {sorted(list(new_ids_in_pr))}")
                else:
                    print(f"     ✅ No new diagnostic IDs")
            else:
                print(f"     ⚠️  Could not access DiagnosticId.cs")
        
        # Step 3: Calculate the next available ID
        print(f"\n🧮 Step 3: Calculating next available ID...")
        all_used_ids = main_ids.union(pr_ids)
        max_used_id = max(all_used_ids)
        next_id = max_used_id + 1
        
        print(f"   All used IDs count: {len(all_used_ids)}")
        print(f"   Highest used ID: {max_used_id}")
        print(f"   Next available ID: {next_id}")
        print(f"   Diagnostic string: PH{next_id}")
        
        # Final result
        result = {
            "status": "success",
            "next_id": next_id,
            "max_main_id": max_main_id,
            "max_used_id": max_used_id,
            "total_open_prs": len(open_prs),
            "prs_with_new_ids": len(pr_details),
            "pr_details": pr_details,
            "diagnostic_id_string": f"PH{next_id}",
            "all_used_ids_count": len(all_used_ids)
        }
        
        print(f"\n🎉 Success! Tool would return:")
        print(f"   Status: {result['status']}")
        print(f"   Next ID: {result['next_id']}")
        print(f"   Diagnostic String: {result['diagnostic_id_string']}")
        print(f"   Open PRs: {result['total_open_prs']}")
        print(f"   PRs with new IDs: {result['prs_with_new_ids']}")
        
        if pr_details:
            print(f"   PR Details:")
            for pr in pr_details:
                print(f"     - PR #{pr['pr_number']}: {pr['new_ids']}")
        
        return True
        
    except Exception as e:
        print(f"❌ Error: {e}")
        return False


if __name__ == "__main__":
    success = manual_next_diagnostic_id_test()
    
    if success:
        print("\n✅ Manual integration test passed!")
        print("The next_diagnosticId tool is ready for use.")
    else:
        print("\n❌ Manual integration test failed!")
    
    sys.exit(0 if success else 1)