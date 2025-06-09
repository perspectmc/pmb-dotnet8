#!/usr/bin/env python3
"""
Vector Query Script for Cline Integration
Quick semantic search across PMB codebase and documentation
"""

import sys
import argparse
from pathlib import Path

import chromadb
from openai import OpenAI
import os

# Import our config
sys.path.append(str(Path(__file__).parent.parent))
from config import *

def format_results(query: str, results: dict, max_results: int = 10, simple: bool = False) -> str:
    """Format search results for Cline consumption"""
    if not results or not results.get('documents') or not results['documents'][0]:
        return f"No results found for: '{query}'"
    
    output = f"Search Results for: '{query}'\n"
    output += "=" * (len(output) - 1) + "\n\n"
    
    documents = results['documents'][0][:max_results]
    metadatas = results['metadatas'][0][:max_results]
    distances = results.get('distances', [[None] * len(documents)])[0][:max_results]
    
    for i, (doc, metadata, distance) in enumerate(zip(documents, metadatas, distances)):
        relevance = f"{(1 - distance) * 100:.1f}%" if distance is not None else "N/A"
        
        if simple:
            output += f"🔍 File: {metadata.get('file_path', 'unknown')}\n"
            if metadata.get('project_name'):
                output += f"📦 Project: {metadata['project_name']}\n"
            if metadata.get('class_names'):
                output += f"📄 Class: {', '.join(metadata['class_names'])}\n"
            output += f"📈 Relevance: {relevance}\n"
            content = doc[:400] + "..." if len(doc) > 400 else doc
            output += f"---\n{content}\n\n"
        else:
            output += f"Result {i+1} (Relevance: {relevance})\n"
            output += f"File: {metadata.get('file_path', 'unknown')}\n"
            output += f"Type: {metadata.get('content_type', 'unknown')}"
            
            if metadata.get('project_name'):
                output += f" | Project: {metadata['project_name']}"
            if metadata.get('doc_category'):
                output += f" | Category: {metadata['doc_category']}"
            if metadata.get('namespace'):
                output += f" | Namespace: {metadata['namespace']}"
            
            output += "\n"
            content = doc[:400] + "..." if len(doc) > 400 else doc
            output += f"Content:\n{content}\n"
            output += "-" * 60 + "\n\n"
    
    return output

def search_by_category(collection, content_type: str = None, project: str = None, category: str = None, limit: int = 20):
    """Search by metadata categories"""
    where_clause = {}
    
    if content_type:
        where_clause['content_type'] = content_type
    if project:
        where_clause['project_name'] = project
    if category:
        where_clause['doc_category'] = category
    
    try:
        results = collection.get(
            where=where_clause,
            limit=limit
        )
        
        output = f"Category Search Results\n"
        output += f"Filters: "
        if content_type:
            output += f"Type={content_type} "
        if project:
            output += f"Project={project} "
        if category:
            output += f"Category={category} "
        output += f"\n"
        output += "=" * 40 + "\n\n"
        
        if results['documents']:
            for i, (doc, metadata) in enumerate(zip(results['documents'], results['metadatas'])):
                output += f"{i+1}. {metadata.get('file_path', 'unknown')}\n"
                if metadata.get('class_names'):
                    output += f"   Classes: {metadata['class_names']}\n"
                content_preview = doc[:100] + "..." if len(doc) > 100 else doc
                output += f"   Preview: {content_preview}\n\n"
        else:
            output += "No documents found matching criteria.\n"
        
        return output
        
    except Exception as e:
        return f"Error in category search: {e}"

def main():
    parser = argparse.ArgumentParser(description='Search PMB vector database')
    parser.add_argument('query', nargs='?', help='Search query text')
    parser.add_argument('-n', '--num-results', type=int, default=5, help='Number of results (default: 5)')
    parser.add_argument('-c', '--category', choices=['source_code', 'documentation'], help='Filter by content type')
    parser.add_argument('-p', '--project', help='Filter by project name (e.g., MBS.Common)')
    parser.add_argument('-d', '--doc-category', help='Filter by doc category (e.g., analysis, architecture)')
    parser.add_argument('--list-projects', action='store_true', help='List all projects')
    parser.add_argument('--list-categories', action='store_true', help='List all doc categories')
    parser.add_argument('--browse', help='Browse files by category (source_code/documentation)')
    parser.add_argument('--simple', action='store_true', help='Simplified output (clean summary view)')
    
    args = parser.parse_args()
    
    # Connect to ChromaDB
    try:
        client = chromadb.PersistentClient(path=str(CHROMA_DB_DIR))
        collection = client.get_collection(name=COLLECTION_NAME)
        print(f"Connected to collection: {COLLECTION_NAME} ({collection.count()} documents)\n")
    except Exception as e:
        print(f"Error connecting to vector database: {e}")
        return
    
    # Handle list operations
    if args.list_projects:
        try:
            all_docs = collection.get()
            projects = set()
            for metadata in all_docs['metadatas']:
                if metadata.get('project_name'):
                    projects.add(metadata['project_name'])
            print("Available Projects:")
            for project in sorted(projects):
                print(f"  - {project}")
        except Exception as e:
            print(f"Error listing projects: {e}")
        return
    
    if args.list_categories:
        try:
            all_docs = collection.get()
            categories = set()
            for metadata in all_docs['metadatas']:
                if metadata.get('doc_category'):
                    categories.add(metadata['doc_category'])
            print("Available Doc Categories:")
            for category in sorted(categories):
                print(f"  - {category}")
        except Exception as e:
            print(f"Error listing categories: {e}")
        return
    
    # Handle browse operation
    if args.browse:
        result = search_by_category(collection, content_type=args.browse, limit=50)
        print(result)
        return
    
    # Handle filtered browsing
    if not args.query and (args.category or args.project or args.doc_category):
        result = search_by_category(
            collection, 
            content_type=args.category,
            project=args.project,
            category=args.doc_category,
            limit=args.num_results * 2
        )
        print(result)
        return
    
    # Require query for semantic search
    if not args.query:
        print("Please provide a search query or use --help for options")
        return
    
    # Perform semantic search
    try:
        results = collection.query(
            query_texts=[args.query],
            n_results=args.num_results,
            where=({
                'content_type': args.category
            } if args.category else None)
        )
        
        # Synthesized summary using OpenAI
        api_key = os.getenv("OPENAI_API_KEY")
        if not api_key:
            raise ValueError("Missing OPENAI_API_KEY environment variable")

        openai = OpenAI(api_key=api_key)

        top_chunks = results['documents'][0]
        top_metadatas = results['metadatas'][0]

        # Prepare one large prompt with metadata
        combined_text = ""
        for i in range(min(len(top_chunks), args.num_results)):
            meta = top_metadatas[i]
            content = top_chunks[i][:1000]
            combined_text += f"\n---\nFile: {meta.get('file_path', 'unknown')}\n"
            combined_text += f"Project: {meta.get('project_name', 'N/A')} | Type: {meta.get('content_type', 'N/A')}\n"
            if meta.get('class_names'):
                combined_text += f"Classes: {', '.join(meta['class_names'])}\n"
            combined_text += f"Content:\n{content}\n"

        # Define the GPT prompt
        system_prompt = "You are a senior AI engineer helping modernize a .NET legacy system. Synthesize the following code/document excerpts into a clear summary. Include the file names, strategic business rationale, and dependencies. Keep it tight, clear, and useful."
        user_prompt = f"I searched for: '{args.query}'. Here are the top results:\n{combined_text}\n\nPlease return a single structured answer."

        response = openai.chat.completions.create(
            model="gpt-4",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ]
        )

        summary = response.choices[0].message.content
        print("\n🧠 Synthesized Summary\n======================")
        print(summary)
        
    except Exception as e:
        print(f"Error performing search: {e}")

if __name__ == "__main__":
    main()