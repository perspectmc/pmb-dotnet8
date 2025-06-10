#!/usr/bin/env python3
"""
PMB Incremental Update Script
Updates vector database with only changed files since last update
"""

import os
import subprocess
from pathlib import Path
from datetime import datetime
from typing import List, Set

import chromadb

# Import our config
import sys
sys.path.append(str(Path(__file__).parent.parent))
from config import *

# Import functions from ingest script
sys.path.append(str(Path(__file__).parent))
from ingest_pmb import process_file, find_source_files

def get_last_update_time() -> datetime:
    """Get timestamp of last update"""
    try:
        if LAST_UPDATE_FILE.exists():
            with open(LAST_UPDATE_FILE, 'r') as f:
                return datetime.fromisoformat(f.read().strip())
    except:
        pass
    
    # Default to very old date if no record
    return datetime(2020, 1, 1)

def get_changed_files_since(since_time: datetime) -> Set[Path]:
    """Get files changed since given timestamp using git"""
    changed_files = set()
    
    try:
        # Get git log since timestamp
        since_str = since_time.strftime('%Y-%m-%d %H:%M:%S')
        cmd = [
            'git', 'log', 
            f'--since={since_str}',
            '--name-only',
            '--pretty=format:',
            '--',
            'src/', 'aidocs/'  # Include both directories
        ]
        
        result = subprocess.run(
            cmd, 
            cwd=PROJECT_ROOT, 
            capture_output=True, 
            text=True
        )
        
        if result.returncode == 0:
            for line in result.stdout.split('\n'):
                line = line.strip()
                if line and (line.startswith('src/') or line.startswith('aidocs/')):
                    file_path = PROJECT_ROOT / line
                    if file_path.exists() and not is_file_ignored(file_path):
                        # Check if it's a supported file type
                        if file_path.suffix in SUPPORTED_EXTENSIONS:
                            changed_files.add(file_path)
        
        # --- ALSO INCLUDE UNTRACKED FILES ---------------------------------
        try:
            untracked_cmd = [
                'git', 'ls-files',
                '--others',
                '--exclude-standard',
                'src/', 'aidocs/'
            ]
            unres = subprocess.run(
                untracked_cmd,
                cwd=PROJECT_ROOT,
                capture_output=True,
                text=True
            )
            if unres.returncode == 0:
                for ln in unres.stdout.split('\n'):
                    ln = ln.strip()
                    if ln and (ln.startswith('src/') or ln.startswith('aidocs/')):
                        fp = PROJECT_ROOT / ln
                        if fp.exists() and not is_file_ignored(fp):
                            if fp.suffix in SUPPORTED_EXTENSIONS:
                                changed_files.add(fp)
        except Exception:
            # Ignore errors (e.g., non‑git folder); mtime fallback covers it
            pass
        # -------------------------------------------------------------------
        
    except Exception as e:
        print(f"Error getting git changes: {e}")
        print("Falling back to filesystem timestamp check...")
        
        # Fallback: check file modification times in both directories
        for directory in [SRC_DIR, DOCS_DIR]:
            if directory.exists():
                for ext in SUPPORTED_EXTENSIONS:
                    pattern = f"**/*{ext}"
                    for file_path in directory.glob(pattern):
                        if not is_file_ignored(file_path):
                            try:
                                file_mtime = datetime.fromtimestamp(file_path.stat().st_mtime)
                                if file_mtime > since_time:
                                    changed_files.add(file_path)
                            except:
                                continue
    
    return changed_files

def remove_file_vectors(collection, file_path: Path):
    """Remove all vectors for a specific file"""
    rel_path = str(file_path.relative_to(PROJECT_ROOT))
    
    try:
        # Get all vectors for this file
        results = collection.get(
            where={"file_path": rel_path}
        )
        
        if results['ids']:
            collection.delete(ids=results['ids'])
            print(f"  Removed {len(results['ids'])} existing vectors")
        
    except Exception as e:
        print(f"  Error removing vectors: {e}")

def update_file_vectors(collection, file_path: Path):
    """Update vectors for a single file"""
    print(f"Updating: {file_path.relative_to(PROJECT_ROOT)}")
    
    # Remove existing vectors for this file
    remove_file_vectors(collection, file_path)
    
    # Process the file
    documents = process_file(file_path)

    # ✅ June 10, 2025 @ 2:55 PM — normalize file_path and add keywords for consistency with ingest_pmb
    import re
    from collections import Counter

    for doc in documents:
        meta = doc["metadata"]
        meta["file_path"] = meta["file_path"].lower()
        tokens = re.findall(r'\b[a-zA-Z]{3,}\b', doc["content"].lower())
        stopwords = {"the", "and", "for", "are", "with", "that", "this", "from", "have", "not", "you", "but", "your"}
        keywords = [word for word, count in Counter(tokens).items() if word not in stopwords and count > 1]
        meta["keywords"] = keywords
    
    if documents:
        # Add new vectors
        ids = [doc['id'] for doc in documents]
        contents = [doc['content'] for doc in documents]
        metadatas = [doc['metadata'] for doc in documents]
        
        collection.add(
            ids=ids,
            documents=contents,
            metadatas=metadatas
        )
        
        print(f"  Added {len(documents)} new vectors")
    else:
        print(f"  No content to vectorize")

def get_collection_stats(collection):
    """Get statistics about the collection"""
    try:
        # Get all items (just count)
        results = collection.get()
        total_vectors = len(results['ids']) if results['ids'] else 0
        
        # Get unique files
        unique_files = set()
        if results['metadatas']:
            for metadata in results['metadatas']:
                if 'file_path' in metadata:
                    unique_files.add(metadata['file_path'])
        
        return {
            'total_vectors': total_vectors,
            'unique_files': len(unique_files)
        }
    except:
        return {'total_vectors': 0, 'unique_files': 0}

def main():
    """Main update process"""
    print("🔄 Starting PMB incremental update...")
    
    # Check if vector database exists
    if not CHROMA_DB_DIR.exists():
        print("❌ Vector database not found!")
        print("Run ingest_pmb.py first to create initial vectors")
        return
    
    # Initialize ChromaDB
    print("📊 Connecting to ChromaDB...")
    client = chromadb.PersistentClient(path=str(CHROMA_DB_DIR))
    
    try:
        collection = client.get_collection(name=COLLECTION_NAME)
    except:
        print("❌ Collection not found!")
        print("Run ingest_pmb.py first to create initial vectors")
        return
    
    # Get collection stats
    stats = get_collection_stats(collection)
    print(f"Current database: {stats['total_vectors']} vectors, {stats['unique_files']} files")
    
    # Get last update time
    last_update = get_last_update_time()
    print(f"Last update: {last_update.strftime('%Y-%m-%d %H:%M:%S')}")
    
    # Find changed files
    print("\n🔍 Finding changed files...")
    changed_files = get_changed_files_since(last_update)
    
    if not changed_files:
        print("✅ No files changed since last update")
        return
    
    print(f"Found {len(changed_files)} changed files:")
    for file_path in sorted(changed_files):
        print(f"  {file_path.relative_to(PROJECT_ROOT)}")
    
    # Confirm update
    response = input(f"\nUpdate vectors for {len(changed_files)} files? (Y/n): ").strip().lower()
    if response == 'n':
        print("Update cancelled")
        return
    
    # Update vectors
    print(f"\n📝 Updating vectors...")
    updated_count = 0
    
    for file_path in changed_files:
        try:
            update_file_vectors(collection, file_path)
            updated_count += 1
        except Exception as e:
            print(f"  Error updating {file_path}: {e}")
    
    # Update timestamp
    with open(LAST_UPDATE_FILE, 'w') as f:
        f.write(datetime.now().isoformat())
    
    print(f"\n✅ Update complete!")
    print(f"Updated: {updated_count}/{len(changed_files)} files")
    
    # Show new stats
    new_stats = get_collection_stats(collection)
    print(f"New database: {new_stats['total_vectors']} vectors, {new_stats['unique_files']} files")
    
    # Quick test
    print("\n🔍 Testing search...")
    results = collection.query(
        query_texts=["recent changes"],
        n_results=3
    )
    
    if results['documents'] and results['documents'][0]:
        print("✅ Search test successful!")
        recent_file = results['metadatas'][0][0]['file_path']
        print(f"Most relevant result: {recent_file}")
    else:
        print("❌ Search test failed")

if __name__ == "__main__":
    main()