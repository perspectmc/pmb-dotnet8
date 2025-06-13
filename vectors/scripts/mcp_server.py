#!/usr/bin/env python3
"""
MCP Server for PMB Vector Database
Provides tools and resources for accessing the PMB vector database directly
"""

import asyncio
import json
import sys
from pathlib import Path
from typing import Any, Dict, List, Optional

# Add the vectors directory to the path
sys.path.append(str(Path(__file__).parent.parent))

import chromadb
from config import *

# MCP imports
from mcp.server import Server, NotificationOptions
from mcp.server.models import InitializationOptions
from mcp.server.stdio import stdio_server
from mcp.types import (
    Resource,
    Tool,
    TextContent,
    ImageContent,
    EmbeddedResource,
    LoggingLevel
)

# Initialize ChromaDB
try:
    client = chromadb.PersistentClient(path=str(CHROMA_DB_DIR))
    collection = client.get_collection(COLLECTION_NAME)
    print(f"✅ MCP Server: Connected to {COLLECTION_NAME} with {collection.count()} documents", file=sys.stderr)
except Exception as e:
    print(f"❌ MCP Server: Failed to connect to ChromaDB: {e}", file=sys.stderr)
    collection = None

# Create the MCP server
server = Server("pmb-vector-db")

@server.list_resources()
async def handle_list_resources() -> List[Resource]:
    """List available resources"""
    return [
        Resource(
            uri="pmb://database/stats",
            name="Database Statistics",
            description="Statistics about the PMB vector database",
            mimeType="application/json"
        ),
        Resource(
            uri="pmb://database/files",
            name="Indexed Files",
            description="List of all files indexed in the vector database",
            mimeType="application/json"
        )
    ]

@server.read_resource()
async def handle_read_resource(uri: str) -> str:
    """Read a specific resource"""
    if not collection:
        raise RuntimeError("Vector database not available")
    
    if uri == "pmb://database/stats":
        # Get database statistics
        total_docs = collection.count()
        sample = collection.get(limit=min(1000, total_docs))
        
        projects = set()
        content_types = set()
        file_types = {}
        
        if sample['metadatas']:
            for metadata in sample['metadatas']:
                if metadata.get('project_name'):
                    projects.add(metadata['project_name'])
                if metadata.get('content_type'):
                    content_types.add(metadata['content_type'])
                
                file_type = metadata.get('file_type', 'unknown')
                file_types[file_type] = file_types.get(file_type, 0) + 1
        
        stats = {
            "total_documents": total_docs,
            "projects": sorted(list(projects)),
            "content_types": sorted(list(content_types)),
            "file_types": file_types,
            "collection_name": COLLECTION_NAME
        }
        
        return json.dumps(stats, indent=2)
    
    elif uri == "pmb://database/files":
        # Get all indexed file paths
        all_docs = collection.get()
        file_paths = set()
        
        if all_docs['metadatas']:
            for metadata in all_docs['metadatas']:
                if 'file_path' in metadata:
                    file_paths.add(metadata['file_path'])
        
        files = sorted(list(file_paths))
        return json.dumps(files, indent=2)
    
    else:
        raise ValueError(f"Unknown resource URI: {uri}")

@server.list_tools()
async def handle_list_tools() -> List[Tool]:
    """List available tools"""
    return [
        Tool(
            name="query_vector_db",
            description="Query the PMB vector database with natural language",
            inputSchema={
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "Natural language query to search for"
                    },
                    "n_results": {
                        "type": "integer",
                        "description": "Number of results to return (default: 5)",
                        "default": 5
                    },
                    "content_type": {
                        "type": "string",
                        "description": "Filter by content type: 'source_code' or 'documentation'",
                        "enum": ["source_code", "documentation"]
                    },
                    "project": {
                        "type": "string",
                        "description": "Filter by project name (e.g., 'MBS.Web.Portal')"
                    }
                },
                "required": ["query"]
            }
        ),
        Tool(
            name="get_file_content",
            description="Get the content of a specific file from the vector database",
            inputSchema={
                "type": "object",
                "properties": {
                    "path": {
                        "type": "string",
                        "description": "File path to retrieve content for"
                    },
                    "lines": {
                        "type": "integer",
                        "description": "Number of lines to return (default: 50)",
                        "default": 50
                    }
                },
                "required": ["path"]
            }
        ),
        Tool(
            name="list_files",
            description="List all files indexed in the vector database",
            inputSchema={
                "type": "object",
                "properties": {
                    "type": {
                        "type": "string",
                        "description": "Filter by file extension (e.g., '.cs', '.md')"
                    }
                }
            }
        )
    ]

@server.call_tool()
async def handle_call_tool(name: str, arguments: Dict[str, Any]) -> List[TextContent]:
    """Handle tool calls"""
    if not collection:
        return [TextContent(type="text", text="Error: Vector database not available")]
    
    try:
        if name == "query_vector_db":
            query = arguments["query"]
            n_results = arguments.get("n_results", 5)
            content_type = arguments.get("content_type")
            project = arguments.get("project")
            
            # Build where clause for filtering
            where_clause = {}
            if content_type:
                where_clause['content_type'] = content_type
            if project:
                where_clause['project_name'] = project
            
            # Perform the search
            results = collection.query(
                query_texts=[query],
                n_results=n_results,
                where=where_clause if where_clause else None
            )
            
            # Format results
            search_results = []
            if results['documents'] and results['documents'][0]:
                documents = results['documents'][0]
                metadatas = results['metadatas'][0]
                distances = results.get('distances', [[1.0] * len(documents)])[0]
                
                for doc, metadata, distance in zip(documents, metadatas, distances):
                    relevance = max(0, 1 - distance)
                    search_results.append({
                        "file": metadata.get('file_path', 'unknown'),
                        "project": metadata.get('project_name', 'n/a'),
                        "content_type": metadata.get('content_type', 'unknown'),
                        "doc_category": metadata.get('doc_category'),
                        "class_names": metadata.get('class_names'),
                        "namespace": metadata.get('namespace'),
                        "relevance": f"{relevance:.3f}",
                        "content": doc[:800] + "..." if len(doc) > 800 else doc
                    })
            
            response = {
                "query": query,
                "total_results": len(search_results),
                "results": search_results
            }
            
            return [TextContent(type="text", text=json.dumps(response, indent=2))]
        
        elif name == "get_file_content":
            path = arguments["path"]
            lines = arguments.get("lines", 50)
            
            # Get all chunks for this file path
            result = collection.get(
                where={"file_path": path},
                include=['documents', 'metadatas']
            )
            
            if not result.get('documents'):
                return [TextContent(type="text", text=f"No content found for path: {path}")]
            
            # Extract and sort chunks
            chunks_with_metadata = []
            documents = result['documents']
            metadatas = result['metadatas']
            
            for doc, metadata in zip(documents, metadatas):
                chunk_index = metadata.get('chunk_index', 0)
                chunks_with_metadata.append((chunk_index, doc, metadata))
            
            # Sort by chunk_index to maintain original file order
            chunks_with_metadata.sort(key=lambda x: x[0])
            
            # Reconstruct full content
            full_content_parts = []
            for chunk_index, doc, metadata in chunks_with_metadata:
                content = doc
                # Remove context prefix if present
                if '\n\n' in content:
                    parts = content.split('\n\n', 1)
                    if len(parts) > 1:
                        content = parts[1]
                full_content_parts.append(content)
            
            # Join all chunks and split into lines
            full_text = ''.join(full_content_parts)
            text_lines = full_text.splitlines()
            
            response = {
                "path": path,
                "total_lines": len(text_lines),
                "requested_lines": lines,
                "total_chunks": len(chunks_with_metadata),
                "lines": text_lines[:lines]
            }
            
            return [TextContent(type="text", text=json.dumps(response, indent=2))]
        
        elif name == "list_files":
            file_type = arguments.get("type")
            
            # Get all documents and extract unique file paths
            all_docs = collection.get()
            file_paths = set()
            
            if all_docs['metadatas']:
                for metadata in all_docs['metadatas']:
                    if 'file_path' in metadata:
                        file_paths.add(metadata['file_path'])
            
            files = sorted(list(file_paths))
            
            if file_type:
                files = [f for f in files if f.endswith(file_type)]
            
            return [TextContent(type="text", text=json.dumps(files, indent=2))]
        
        else:
            return [TextContent(type="text", text=f"Unknown tool: {name}")]
    
    except Exception as e:
        return [TextContent(type="text", text=f"Error: {str(e)}")]

async def main():
    """Main entry point for the MCP server"""
    async with stdio_server() as (read_stream, write_stream):
        await server.run(
            read_stream,
            write_stream,
            InitializationOptions(
                server_name="pmb-vector-db",
                server_version="1.0.0",
                capabilities=server.get_capabilities(
                    notification_options=NotificationOptions(),
                    experimental_capabilities={}
                )
            )
        )

if __name__ == "__main__":
    asyncio.run(main())
