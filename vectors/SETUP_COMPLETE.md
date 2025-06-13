# PMB Vector Database Access - Complete Setup Guide ✅

## 🎯 What This Does
This setup gives you two ways to search through your PMB medical billing system codebase using natural language:
1. **ChatGPT** - via web API
2. **Claude Desktop** - via MCP server (recommended for cost)

Your database contains **9,864 documents** from your PMB project including source code, documentation, and configuration files.

---

## 🚀 Method 1: Using ChatGPT (Web API)

### ✅ Status: Ready to Use
- **URL**: https://0b9e-68-69-221-58.ngrok-free.app
- **Local**: http://localhost:8000

### Step-by-Step Instructions:

#### Step 1: Verify It's Running
1. Open Terminal
2. Check if the API is running:
   ```bash
   curl https://0b9e-68-69-221-58.ngrok-free.app/stats
   ```
3. You should see database statistics

#### Step 2: Use with ChatGPT
1. Go to ChatGPT
2. Copy and paste this prompt template:
   ```
   Please make a POST request to this URL:
   https://0b9e-68-69-221-58.ngrok-free.app/query
   
   With this JSON data:
   {
     "query": "YOUR QUESTION HERE",
     "n_results": 5
   }
   
   Example: Replace "YOUR QUESTION HERE" with "how is user authentication handled"
   ```

#### Step 3: Example Queries
- "How is user authentication implemented?"
- "Show me the database connection logic"
- "Find all controllers in the web portal"
- "What are the main domain models?"

---

## 🚀 Method 2: Using Claude Desktop (MCP Server) - RECOMMENDED

### ✅ Status: Configured and Ready

### Step-by-Step Instructions:

#### Step 1: Verify Claude Desktop is Installed
1. Look for Claude Desktop app in your Applications folder
2. If not installed, download from: https://claude.ai/download

#### Step 2: Restart Claude Desktop
1. **Quit Claude Desktop completely** (Cmd+Q)
2. **Reopen Claude Desktop**
3. This loads the new MCP server configuration

#### Step 3: Verify MCP Server is Working
1. In Claude Desktop, start a new conversation
2. Type: "What tools do you have available?"
3. You should see tools like:
   - `query_vector_db`
   - `get_file_content` 
   - `list_files`

#### Step 4: Start Using It
Simply ask Claude natural language questions about your codebase:

**Example Conversations:**
```
You: "Search the codebase for user authentication logic"
Claude: [Uses query_vector_db tool automatically]

You: "Show me the content of the AccountController file"
Claude: [Uses get_file_content tool automatically]

You: "List all the C# files in the project"
Claude: [Uses list_files tool automatically]
```

#### Step 5: Advanced Queries
```
"Find all database connection code"
"Show me how claims are processed"
"What are the main MVC controllers?"
"Find error handling patterns"
"Show me the user management functionality"
```

---

## 🔧 Troubleshooting

### If ChatGPT Method Doesn't Work:
1. Check if ngrok is running: `ps aux | grep ngrok`
2. Check if API is running: `ps aux | grep vector_api`
3. Restart both:
   ```bash
   cd /Users/perspect/Desktop/PMB\ dotnet8/vectors/scripts
   python vector_api.py &
   ngrok http 8000
   ```

### If Claude Desktop Method Doesn't Work:

#### Common Error: "spawn python ENOENT"
This means Claude Desktop can't find Python. The configuration has been updated to use your virtual environment's Python.

1. **Completely quit Claude Desktop** (Cmd+Q)
2. **Reopen Claude Desktop**
3. Test the MCP server manually:
   ```bash
   /Users/perspect/Desktop/PMB\ dotnet8/.venv/bin/python3 /Users/perspect/Desktop/PMB\ dotnet8/vectors/scripts/test_mcp.py
   ```
4. If test fails, check the configuration file:
   ```bash
   cat ~/Library/Application\ Support/Claude/claude_desktop_config.json
   ```
5. The config should show the full path to your virtual environment Python:
   ```json
   {
     "mcpServers": {
       "pmb-vector-db": {
         "command": "/Users/perspect/Desktop/PMB dotnet8/.venv/bin/python3",
         "args": ["/Users/perspect/Desktop/PMB dotnet8/vectors/scripts/mcp_server.py"]
       }
     }
   }
   ```

---

## 📊 What's in Your Database

- **Total Documents**: 9,864
- **Source Code Files**: 639 .cs files, 13 .csproj files
- **Documentation**: 317 .md files
- **Projects**: MBS.Web.Portal, MBS.Common, MBS.DataCache, etc.
- **Content Types**: Source code and documentation

---

## 💡 Pro Tips

### For Best Results:
1. **Be specific**: "Show me user authentication in the web portal" vs "show me auth"
2. **Ask follow-ups**: "Get the full content of that AccountController file"
3. **Filter by type**: "Find only documentation about claims processing"

### Cost Comparison:
- **ChatGPT**: Pay per API call (your existing ChatGPT subscription)
- **Claude Desktop**: Much cheaper, often free tier available
- **Cline (VS Code)**: Most expensive (~$20/day for heavy use)

---

## 📁 File Locations

```
vectors/
├── scripts/
│   ├── vector_api.py      # HTTP API server
│   ├── mcp_server.py      # MCP server for Claude
│   └── test_mcp.py        # Test MCP server
├── config.py              # Configuration
├── chroma_db/             # Database files
└── SETUP_COMPLETE.md      # This guide
```

---

## ✅ Quick Start Checklist

### For ChatGPT:
- [ ] API running at https://0b9e-68-69-221-58.ngrok-free.app
- [ ] Test with curl command
- [ ] Use POST request template in ChatGPT

### For Claude Desktop:
- [ ] Claude Desktop installed
- [ ] Restart Claude Desktop
- [ ] Ask "What tools do you have?"
- [ ] Start asking questions about your codebase

**You're all set! Both methods are working and ready to use.**
