#!/usr/bin/env python3
"""
PMB Code Ingestion Script
Processes all PMB source code files and creates vector embeddings in ChromaDB
"""

import os
import re
import hashlib
from pathlib import Path
from datetime import datetime
from typing import List, Dict, Any

import chromadb
from chromadb.config import Settings

# Import our config
import sys
sys.path.append(str(Path(__file__).parent.parent))
from config import *

def extract_csharp_metadata(content: str, file_path: Path) -> Dict[str, Any]:
    """Extract C# specific metadata from file content"""
    metadata = {}
    
    # Extract namespace
    namespace_match = re.search(r'namespace\s+([A-Za-z0-9_.]+)', content)
    metadata['namespace'] = namespace_match.group(1) if namespace_match else ""
    
    # Extract class names
    class_matches = re.findall(r'(?:public|internal|private|protected)?\s*(?:static|abstract|sealed)?\s*class\s+([A-Za-z0-9_]+)', content)
    metadata['class_names'] = ', '.join(class_matches) if class_matches else ""
    
    # Extract interface names
    interface_matches = re.findall(r'(?:public|internal|private|protected)?\s*interface\s+([A-Za-z0-9_]+)', content)
    if interface_matches:
        interfaces = ', '.join(interface_matches)
        if metadata['class_names']:
            metadata['class_names'] += f", {interfaces}"
        else:
            metadata['class_names'] = interfaces
    
    return metadata

def chunk_content(content: str, max_size: int = MAX_CHUNK_SIZE, overlap: int = CHUNK_OVERLAP) -> List[str]:
    """Split large content into overlapping chunks"""
    if len(content) <= max_size:
        return [content]
    
    chunks = []
    start = 0
    
    while start < len(content):
        end = start + max_size
        
        # Try to break at natural boundaries (newlines)
        if end < len(content):
            # Look for newline within last 100 chars
            last_newline = content.rfind('\n', start, end)
            if last_newline > start + (max_size // 2):
                end = last_newline
        
        chunk = content[start:end].strip()
        if chunk:
            chunks.append(chunk)
        
        start = end - overlap
        if start >= len(content):
            break
    
    return chunks

def process_file(file_path: Path) -> List[Dict[str, Any]]:
    """Process a single file and return list of documents to embed"""
    try:
        # Read file content
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        
        if not content.strip():
            return []
        
        # Get file stats
        stat = file_path.stat()
        rel_path = file_path.relative_to(PROJECT_ROOT)
        
        # Base metadata for all chunks
        base_metadata = {
            'file_path': str(rel_path),
            'content_type': get_content_type(rel_path),
            'project_name': get_project_name(rel_path) if get_content_type(rel_path) == "source_code" else "",
            'doc_category': get_doc_category(rel_path) if get_content_type(rel_path) == "documentation" else "",
            'file_type': file_path.suffix,
            'file_size': stat.st_size,
            'last_modified': datetime.fromtimestamp(stat.st_mtime).isoformat(),
            'git_hash': get_git_hash(),
            'namespace': '',
            'class_names': ''
        }
        
        # Extract C# specific metadata
        if file_path.suffix == '.cs':
            csharp_meta = extract_csharp_metadata(content, file_path)
            base_metadata.update(csharp_meta)
        
        # Chunk the content
        chunks = chunk_content(content)
        
        # Create documents for each chunk
        documents = []
        for i, chunk in enumerate(chunks):
            # Create unique ID for this chunk
            chunk_id = hashlib.md5(f"{rel_path}_{i}".encode()).hexdigest()

            metadata = base_metadata.copy()
            metadata['chunk_index'] = i
            metadata['total_chunks'] = len(chunks)

            # Add context information to chunk
            context_prefix = f"File: {rel_path}\n"
            if metadata['content_type'] == "source_code":
                if metadata['namespace']:
                    context_prefix += f"Namespace: {metadata['namespace']}\n"
                if metadata['class_names']:
                    context_prefix += f"Classes: {metadata['class_names']}\n"
                if metadata['project_name']:
                    context_prefix += f"Project: {metadata['project_name']}\n"
            elif metadata['content_type'] == "documentation":
                if metadata['doc_category']:
                    context_prefix += f"Category: {metadata['doc_category']}\n"
                context_prefix += f"Type: Documentation\n"
            context_prefix += "\n"

            # Add high-level summary to metadata only on first chunk
            if i == 0:
                summary_lines = []
                if metadata['content_type'] == "source_code":
                    summary_lines.append(f"This file supports the Perspect Medical Billing system by implementing or configuring: {metadata.get('class_names', 'unknown class')}.")
                    summary_lines.append("Its primary purpose is to serve application infrastructure, logic control, or external integration.")
                    summary_lines.append(f"Technical highlights: Namespace {metadata.get('namespace', 'N/A')}, part of project {metadata.get('project_name', 'unknown')}.")

                    # Optional inferred dependencies could be extracted with simple keyword matching
                    likely_dependencies = []
                    if "HttpClient" in chunk or "WebRequest" in chunk:
                        likely_dependencies.append("MSB API or external HTTP services")
                    if "SmtpClient" in chunk or "Mail" in chunk:
                        likely_dependencies.append("Email services")
                    if "Pdf" in chunk or "iText" in chunk:
                        likely_dependencies.append("PDF generation")
                    if likely_dependencies:
                        summary_lines.append(f"Likely dependencies: {', '.join(likely_dependencies)}")

                    summary_lines.append("This summary was autogenerated to support strategic modernization and may require review.")
                    metadata['summary'] = " ".join(summary_lines)
                else:
                    metadata['summary'] = "Documentation file. Autogenerated summary not implemented for non-source files."

            full_content = context_prefix + chunk

            documents.append({
                'id': chunk_id,
                'content': full_content,
                'metadata': metadata
            })

        return documents
        
    except Exception as e:
        print(f"Error processing {file_path}: {e}")
        return []

def find_source_files() -> List[Path]:
    """Find all source files to process"""
    source_files = []
    
    # Scan source code directory
    for ext in SUPPORTED_EXTENSIONS:
        pattern = f"**/*{ext}"
        for file_path in SRC_DIR.glob(pattern):
            if not is_file_ignored(file_path):
                source_files.append(file_path)
    
    # Scan AI documentation directory
    if DOCS_DIR.exists():
        for ext in SUPPORTED_EXTENSIONS:
            pattern = f"**/*{ext}"
            for file_path in DOCS_DIR.glob(pattern):
                if not is_file_ignored(file_path):
                    source_files.append(file_path)
    
    return sorted(source_files)

def main():
    """Main ingestion process"""
    print("🚀 Starting PMB code ingestion...")
    print(f"Source directory: {SRC_DIR}")
    print(f"Vector database: {CHROMA_DB_DIR}")
    
    # Initialize ChromaDB
    print("\n📊 Initializing ChromaDB...")
    client = chromadb.PersistentClient(path=str(CHROMA_DB_DIR))
    
    # Create or get collection
    try:
        collection = client.get_collection(name=COLLECTION_NAME)
        print(f"Found existing collection: {COLLECTION_NAME}")
        
        # Option to clear existing data
        response = input("Clear existing vectors? (y/N): ").strip().lower()
        if response == 'y':
            client.delete_collection(name=COLLECTION_NAME)
            collection = client.create_collection(name=COLLECTION_NAME)
            print("Cleared existing collection")
    except:
        collection = client.create_collection(name=COLLECTION_NAME)
        print(f"Created new collection: {COLLECTION_NAME}")
    
    # Find source files
    print("\n🔍 Finding source files...")
    source_files = find_source_files()
    print(f"Found {len(source_files)} files to process")

    # Create manifest log
    manifest_log = {
        "ingestion_timestamp": datetime.now().isoformat(),
        "file_count": len(source_files),
        "file_types": {},
        "files": []
    }

    for file_path in source_files:
        file_type = file_path.suffix
        manifest_log["file_types"][file_type] = manifest_log["file_types"].get(file_type, 0) + 1
        manifest_log["files"].append(str(file_path.relative_to(PROJECT_ROOT)))

    # Write manifest log to disk
    manifest_path = PROJECT_ROOT / "vectors" / "logs" / "vector_ingest_report.json"
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    with open(manifest_path, "w") as mf:
        import json
        json.dump(manifest_log, mf, indent=2)
    print(f"\n🧾 Manifest written to: {manifest_path}")
    # Print manifest summary
    print("\n📋 Manifest Summary:")
    print(f"  Total files: {manifest_log['file_count']}")
    print("  File types:")
    for ext, count in manifest_log["file_types"].items():
        print(f"    {ext}: {count}")
    print("  First 5 files:")
    for f in manifest_log["files"][:5]:
        print(f"    {f}")
    if len(manifest_log["files"]) > 5:
        print(f"    ...and {len(manifest_log['files']) - 5} more")
    print("Ingestion manifest includes file count and type summary.")
    
    # Process files
    print("\n📝 Processing files...")
    all_documents = []
    processed_count = 0
    
    for file_path in source_files:
        print(f"Processing: {file_path.relative_to(PROJECT_ROOT)}")
        documents = process_file(file_path)
        all_documents.extend(documents)
        processed_count += 1
        
        if processed_count % 10 == 0:
            print(f"  Processed {processed_count}/{len(source_files)} files")
    
    if not all_documents:
        print("❌ No documents to embed!")
        return
    
    print(f"\n🧮 Created {len(all_documents)} document chunks from {len(source_files)} files")
    
    # Add to ChromaDB in batches
    print("\n💾 Adding to vector database...")
    batch_size = 100
    
    for i in range(0, len(all_documents), batch_size):
        batch = all_documents[i:i + batch_size]
        
        ids = [doc['id'] for doc in batch]
        documents = [doc['content'] for doc in batch]
        metadatas = [doc['metadata'] for doc in batch]
        
        collection.add(
            ids=ids,
            documents=documents,
            metadatas=metadatas
        )
        
        print(f"  Added batch {i//batch_size + 1}/{(len(all_documents)-1)//batch_size + 1}")
    
    # Save last update timestamp
    with open(LAST_UPDATE_FILE, 'w') as f:
        f.write(datetime.now().isoformat())
    
    print(f"\n✅ Ingestion complete!")
    print(f"Total vectors: {len(all_documents)}")
    print(f"Database location: {CHROMA_DB_DIR}")
    
    # client.persist()  # <-- added line to persist the collection

    # Quick test query
    print("\n🔍 Testing search...")
    results = collection.query(
        query_texts=["ConfigHelper connection string"],
        n_results=3
    )
    
    if results['documents'] and results['documents'][0]:
        print("✅ Search test successful!")
        print(f"Sample result: {results['metadatas'][0][0]['file_path']}")
    else:
        print("❌ Search test failed")

if __name__ == "__main__":
    main()