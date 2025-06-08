"""
PMB Vector Database Configuration
Configuration settings for ChromaDB integration with PMB migration project
"""

import os
from pathlib import Path

# Project paths
PROJECT_ROOT = Path(__file__).parent.parent  # pmb-dotnet8/
SRC_DIR = PROJECT_ROOT / "src"
DOCS_DIR = PROJECT_ROOT / "aidocs"  # AI documentation
VECTORS_DIR = PROJECT_ROOT / "vectors"
CHROMA_DB_DIR = VECTORS_DIR / "chroma_db"

# ChromaDB settings
COLLECTION_NAME = "pmb_complete"  # Updated name for complete context
EMBEDDING_MODEL = "all-MiniLM-L6-v2"  # Good balance of speed/quality

# File processing settings
SUPPORTED_EXTENSIONS = [".cs", ".config", ".json", ".xml", ".md"]  # Added .md
IGNORE_PATTERNS = [
    "bin/", "obj/", "packages/", ".vs/", 
    "Debug/", "Release/", "TestResults/",
    "*.dll", "*.exe", "*.pdb"
]

# Chunk settings for large files
MAX_CHUNK_SIZE = 1000  # characters
CHUNK_OVERLAP = 100    # character overlap between chunks

# Git integration
TRACK_FILE_CHANGES = True
LAST_UPDATE_FILE = VECTORS_DIR / ".last_update"

# Metadata fields stored with each vector
METADATA_FIELDS = [
    "file_path",      # Relative path from project root
    "content_type",   # "source_code" or "documentation"
    "project_name",   # MBS.Common, MBS.Web.Portal, etc. (for source)
    "doc_category",   # analysis, planning, architecture, etc. (for docs)
    "file_type",      # .cs, .config, .md, etc.
    "class_names",    # Extracted class names (for .cs files)
    "namespace",      # C# namespace (for .cs files)
    "chunk_index",    # For multi-chunk files
    "file_size",      # Original file size
    "last_modified",  # File modification timestamp
    "git_hash"        # Git commit hash when processed
]

# Search settings
DEFAULT_SEARCH_RESULTS = 10
SIMILARITY_THRESHOLD = 0.7

def get_git_hash():
    """Get current git commit hash"""
    try:
        import subprocess
        result = subprocess.run(
            ["git", "rev-parse", "HEAD"], 
            cwd=PROJECT_ROOT,
            capture_output=True, 
            text=True
        )
        return result.stdout.strip()[:8]  # Short hash
    except:
        return "unknown"

def is_file_ignored(file_path):
    """Check if file should be ignored based on patterns"""
    file_str = str(file_path)
    return any(pattern in file_str for pattern in IGNORE_PATTERNS)

def get_project_name(file_path):
    """Extract project name from file path"""
    parts = file_path.parts
    if "src" in parts:
        src_index = parts.index("src")
        if src_index + 1 < len(parts):
            return parts[src_index + 1]
    return "unknown"

def get_doc_category(file_path):
    """Extract document category from aidocs path"""
    parts = file_path.parts
    if "aidocs" in parts:
        aidocs_index = parts.index("aidocs")
        if aidocs_index + 1 < len(parts):
            return parts[aidocs_index + 1]  # architecture, analysis, etc.
    return "general"

def get_content_type(file_path):
    """Determine if this is source code or documentation"""
    parts = file_path.parts
    if "src" in parts:
        return "source_code"
    elif "aidocs" in parts:
        return "documentation"
    return "unknown"

print(f"PMB Vector Config loaded - Project root: {PROJECT_ROOT}")