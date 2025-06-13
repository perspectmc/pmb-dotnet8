#!/usr/bin/env python3
"""
Simple test for the PMB MCP server - just verify it can start
"""

import sys
from pathlib import Path

# Add the vectors directory to the path
sys.path.append(str(Path(__file__).parent))

try:
    from mcp_server import server, collection
    print("✅ MCP Server imports successful")
    
    if collection:
        count = collection.count()
        print(f"✅ ChromaDB connected with {count} documents")
    else:
        print("❌ ChromaDB not available")
        sys.exit(1)
    
    # Test that the server has the expected handlers
    handlers = []
    if hasattr(server, '_list_resources_handler'):
        handlers.append("list_resources")
    if hasattr(server, '_call_tool_handler'):
        handlers.append("call_tool")
    if hasattr(server, '_read_resource_handler'):
        handlers.append("read_resource")
    if hasattr(server, '_list_tools_handler'):
        handlers.append("list_tools")
    
    print(f"✅ MCP Server handlers: {', '.join(handlers)}")
    
    print("\n🎉 MCP Server is ready!")
    print("\n📋 Setup Instructions:")
    print("1. Restart Claude Desktop to load the new MCP server configuration")
    print("2. In Claude Desktop, you should see 'pmb-vector-db' tools available")
    print("3. Available tools:")
    print("   - query_vector_db: Search your codebase with natural language")
    print("   - get_file_content: Get content of specific files")
    print("   - list_files: List all indexed files")
    print("4. Available resources:")
    print("   - pmb://database/stats: Database statistics")
    print("   - pmb://database/files: List of indexed files")
    
except Exception as e:
    print(f"❌ Error testing MCP server: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)
