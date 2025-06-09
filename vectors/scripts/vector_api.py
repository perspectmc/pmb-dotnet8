#!/usr/bin/env python3
"""
FastAPI Web Interface for PMB Vector Database
Simple REST API for querying PMB codebase and documentation
"""

from fastapi import FastAPI, Request, HTTPException
from fastapi.responses import HTMLResponse
from pydantic import BaseModel
from typing import Optional, List
import sys
from pathlib import Path


import chromadb

# --- Disable direct filesystem I/O at runtime ---
import builtins
builtins.open = lambda *args, **kwargs: (_ for _ in ()).throw(RuntimeError("Disk I/O disabled"))
import os
if hasattr(os, "walk"):
    del os.walk
from pathlib import Path
# Prevent using Path for filesystem scans
del Path


# Import our config
import os as _orig_os
sys.path.append(_orig_os.path.dirname(_orig_os.path.dirname(__file__)))
from config import *

# Initialize ChromaDB
try:
    client = chromadb.PersistentClient(path=str(CHROMA_DB_DIR))
    collection = client.get_collection(COLLECTION_NAME)
    print(f"✅ Connected to {COLLECTION_NAME} with {collection.count()} documents")
except Exception as e:
    print(f"❌ Failed to connect to ChromaDB: {e}")
    collection = None


app = FastAPI(title="PMB Vector Search API", version="1.0.0")

# --- /files endpoint ---
from typing import List, Optional  # Ensure List and Optional are imported


@app.get("/files", response_model=List[str])
def list_files(type: Optional[str] = None):
    """
    Return all indexed file paths from the vector store.
    Optional filter by file extension.
    """
    try:
        files = collection.list_paths()
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error listing files: {e}")
    if type:
        return [f for f in files if f.endswith(type)]
    return files


@app.get("/file-content")
def get_file_content(path: str, lines: int = 20):
    """
    Return the first `lines` lines of the full document stored in the vector index at `path`.
    """
    # Validate that the file is indexed
    indexed_paths = collection.list_paths()
    if path not in indexed_paths:
        raise HTTPException(status_code=404, detail=f"Path not found: {path}")

    # Query all chunks for this file via metadata filter
    result = collection.query(
        query_texts=[""],            # dummy query to trigger metadata filter
        where={"file_path": path},
        n_results=9999               # retrieve all chunks
    )
    # Extract and concatenate chunks in order
    chunks = result.get("documents", [[]])[0]
    full_text = "".join(chunks)

    # Split into lines and return the requested slice
    text_lines = full_text.splitlines()
    return {"lines": text_lines[:lines]}

class QueryRequest(BaseModel):
    query: str
    n_results: int = 5
    content_type: Optional[str] = None  # "source_code" or "documentation"
    project: Optional[str] = None
    interpret: Optional[bool] = False  # If true, provide a reasoning-based response

class SearchResult(BaseModel):
    file: str
    project: str
    content_type: str
    doc_category: Optional[str]
    class_names: Optional[str]
    namespace: Optional[str]
    relevance: float
    content: str

class QueryResponse(BaseModel):
    query: str
    total_results: int
    results: List[SearchResult]

@app.get("/", response_class=HTMLResponse)
async def root():
    """Simple web interface for testing"""
    html_content = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>PMB Vector Search</title>
        <style>
            body { font-family: Arial, sans-serif; margin: 40px; }
            .container { max-width: 800px; margin: 0 auto; }
            .search-box { width: 100%; padding: 10px; font-size: 16px; margin-bottom: 20px; }
            .filters { margin-bottom: 20px; }
            .result { border: 1px solid #ddd; padding: 15px; margin-bottom: 15px; border-radius: 5px; }
            .file-path { font-weight: bold; color: #0066cc; }
            .metadata { color: #666; font-size: 14px; margin: 5px 0; }
            .content { background: #f5f5f5; padding: 10px; margin-top: 10px; border-radius: 3px; }
            button { padding: 10px 20px; font-size: 16px; background: #0066cc; color: white; border: none; border-radius: 3px; cursor: pointer; }
            button:hover { background: #0052a3; }
        </style>
    </head>
    <body>
        <div class="container">
            <h1>PMB Vector Search</h1>
            <p>Search across PMB codebase and documentation using natural language</p>
            
            <input type="text" id="query" class="search-box" placeholder="Ask a question... (e.g., 'How is ConfigHelper used?')" />
            
            <div class="filters">
                <label>Type: 
                    <select id="content_type">
                        <option value="">All</option>
                        <option value="source_code">Source Code</option>
                        <option value="documentation">Documentation</option>
                    </select>
                </label>
                
                <label style="margin-left: 20px;">Results: 
                    <select id="n_results">
                        <option value="5">5</option>
                        <option value="10">10</option>
                        <option value="15">15</option>
                    </select>
                </label>
                
                <button onclick="search()" style="margin-left: 20px;">Search</button>
            </div>
            
            <div id="results"></div>
        </div>
        
        <script>
            async function search() {
                const query = document.getElementById('query').value;
                const content_type = document.getElementById('content_type').value;
                const n_results = parseInt(document.getElementById('n_results').value);
                
                if (!query.trim()) {
                    alert('Please enter a search query');
                    return;
                }
                
                const resultsDiv = document.getElementById('results');
                resultsDiv.innerHTML = '<p>Searching...</p>';
                
                try {
                    const response = await fetch('/query', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            query: query,
                            n_results: n_results,
                            content_type: content_type || null
                        })
                    });
                    
                    const data = await response.json();
                    displayResults(data);
                } catch (error) {
                    resultsDiv.innerHTML = '<p>Error: ' + error.message + '</p>';
                }
            }
            
            function displayResults(data) {
                const resultsDiv = document.getElementById('results');
                
                if (data.results.length === 0) {
                    resultsDiv.innerHTML = '<p>No results found for: "' + data.query + '"</p>';
                    return;
                }
                
                let html = '<h2>Results for: "' + data.query + '"</h2>';
                
                data.results.forEach((result, index) => {
                    html += '<div class="result">';
                    html += '<div class="file-path">' + (index + 1) + '. ' + result.file + '</div>';
                    html += '<div class="metadata">';
                    html += 'Type: ' + result.content_type;
                    if (result.project) html += ' | Project: ' + result.project;
                    if (result.doc_category) html += ' | Category: ' + result.doc_category;
                    if (result.namespace) html += ' | Namespace: ' + result.namespace;
                    html += ' | Relevance: ' + (result.relevance * 100).toFixed(1) + '%';
                    html += '</div>';
                    if (result.class_names) {
                        html += '<div class="metadata">Classes: ' + result.class_names + '</div>';
                    }
                    html += '<div class="content">' + result.content.replace(/\\n/g, '<br>') + '</div>';
                    html += '</div>';
                });
                
                resultsDiv.innerHTML = html;
            }
            
            // Allow Enter key to search
            document.getElementById('query').addEventListener('keypress', function(e) {
                if (e.key === 'Enter') {
                    search();
                }
            });
        </script>
    </body>
    </html>
    """
    return HTMLResponse(content=html_content)

@app.post("/query", response_model=QueryResponse)
async def query_vectors(req: QueryRequest):
    """Query the vector database"""
    if not collection:
        raise HTTPException(status_code=500, detail="Vector database not available")
    
    if req.interpret:
        print(f"🔍 Interpretation mode enabled for query: '{req.query}'")
    
    try:
        # Build where clause for filtering
        where_clause = {}
        if req.content_type:
            where_clause['content_type'] = req.content_type
        if req.project:
            where_clause['project_name'] = req.project
        
        # Perform the search
        results = collection.query(
            query_texts=[req.query],
            n_results=req.n_results,
            where=where_clause if where_clause else None
        )
        
        # Format results
        search_results = []
        if results['documents'] and results['documents'][0]:
            documents = results['documents'][0]
            metadatas = results['metadatas'][0]
            distances = results.get('distances', [[1.0] * len(documents)])[0]
            
            for doc, metadata, distance in zip(documents, metadatas, distances):
                search_results.append(SearchResult(
                    file=metadata.get('file_path', 'unknown'),
                    project=metadata.get('project_name', 'n/a'),
                    content_type=metadata.get('content_type', 'unknown'),
                    doc_category=metadata.get('doc_category'),
                    class_names=metadata.get('class_names'),
                    namespace=metadata.get('namespace'),
                    relevance=max(0, 1 - distance),  # Convert distance to relevance
                    content=doc[:500] + "..." if len(doc) > 500 else doc
                ))
        
        if req.interpret:
            print(f"ℹ️ Interpretation mode: summarizing results for '{req.query}'")
            for result in search_results:
                result_summary = result.content.split('\n')[0][:200]  # crude first-line summary
                result.content = f"Summary: {result_summary}...\n\n{result.content}"
        
        return QueryResponse(
            query=req.query,
            total_results=len(search_results),
            results=search_results
        )
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Search error: {str(e)}")

@app.get("/stats")
async def get_stats():
    """Get database statistics"""
    if not collection:
        raise HTTPException(status_code=500, detail="Vector database not available")
    
    try:
        # Get basic stats
        total_docs = collection.count()
        
        # Get sample of metadata to analyze
        sample = collection.get(limit=min(1000, total_docs))
        
        projects = set()
        content_types = set()
        doc_categories = set()
        file_types = {}
        
        if sample['metadatas']:
            for metadata in sample['metadatas']:
                if metadata.get('project_name'):
                    projects.add(metadata['project_name'])
                if metadata.get('content_type'):
                    content_types.add(metadata['content_type'])
                if metadata.get('doc_category'):
                    doc_categories.add(metadata['doc_category'])
                
                file_type = metadata.get('file_type', 'unknown')
                file_types[file_type] = file_types.get(file_type, 0) + 1
        
        return {
            "total_documents": total_docs,
            "projects": sorted(list(projects)),
            "content_types": sorted(list(content_types)),
            "doc_categories": sorted(list(doc_categories)),
            "file_types": file_types,
            "collection_name": COLLECTION_NAME
        }
        
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Stats error: {str(e)}")

if __name__ == "__main__":
    import uvicorn
    print("🚀 Starting PMB Vector Search API...")
    print("📊 Web interface: http://localhost:8000")
    print("📚 API docs: http://localhost:8000/docs")
    uvicorn.run(app, host="0.0.0.0", port=8000)