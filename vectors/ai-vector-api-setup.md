<file name=0 path=/Users/perspect/Desktop/PMB dotnet8/vectors/ai-vector-api-setup.md># 🧠 PMB AI Vector API Setup (Local + Public Access)

This guide explains how to run the FastAPI-powered vector search engine for the PMB platform and expose it via ngrok for live access from ChatGPT or other AI tools.

---


## 🚀 Cold Start: How to Launch the PMB Vector API from Scratch

Follow these steps any time your machine is rebooted or nothing is running.

### 1. Start the FastAPI "Vector" Server Locally

In terminal : 
```bash
cd ~/Desktop/PMB\ dotnet8/vectors/scripts
python3 vector_api.py

Then leave running in background.
```

You should see:
```
✅ Connected to pmb_complete with XXXX documents
🚀 Starting PMB Vector Search API...
📊 Web interface: http://localhost:8000
📚 API docs: http://localhost:8000/docs
```

### 2. Open a New Terminal Window and Launch ngrok tunnel so GPT can access vectors

```bash
ngrok http 8000
```

You'll get a public URL like:
```
Forwarding https://a1b2c3d4.ngrok-free.app → http://localhost:8000
```

Copy the `https://...ngrok-free.app` link.

### 2a - Visit: xxxx.ngrok-free.app

After running ngrok, paste the public URL into your browser. You’ll land on a screen that says:

You are about to visit: xxxx.ngrok-free.app

Click the “Visit Site” button to confirm trust and activate the tunnel.
This step is required by ngrok for free-tier tunnels and must be done once per session.

### 3. Paste the URL into ChatGPT When Prompted

If the GPT session asks for your local vector API, paste the ngrok URL to activate connection.

---

To use vector search automatically, simply type:

```
VQ
```

in a new chat. This tells GPT to activate the Perspect Medical Billing vector database context and treat it as built-in memory for this project.

---

## 🔧 Requirements

- Python 3.10+
- Chroma vector DB already built (`ingest_pmb.py`)
- FastAPI + Uvicorn
- ngrok (installed via Homebrew)

---

## 💬 3. Interact with the Vector API

Use tools like:

- **Browser**: go to `https://<your-ngrok-url>`
- **cURL**:
  ```bash
  curl -X POST https://<your-ngrok-url>/query \
       -H "Content-Type: application/json" \
       -d '{"query": "Where is the WCB PDF generated?"}'
  ```

- **ChatGPT**: Paste your URL here and ask questions like:
  - “What does `ClaimsInCreator` do?”
  - “How is impersonation handled?”
  - “Where is the WCB PDF created and sent?”

---

## 🧠 Tips for Future Use

- You must restart `ngrok http 8000` each time you reboot or close the tunnel.
- Keep your token private — never share your full `ngrok.yml` or token in public.
- Store this workflow in Git for future team members.

---

## 🔐 Notes

- Authtoken is saved to: `~/Library/Application Support/ngrok/ngrok.yml` (macOS)
- You can reset your token at: https://dashboard.ngrok.com/get-started/your-authtoken

---

## 🎯 Sample Prompt for ChatGPT Interaction

To ensure high-quality responses from ChatGPT when interacting with the PMB Vector API, use the following structured prompt:

---
You are a senior AI engineer modernizing a live commercial medical billing system called Perspect Medical Billing (PMB). The system includes legacy .NET 4.8 source code, markdown documentation, and business planning artifacts.

You are connected to a vector database that contains:
- All source code (`src/`)
- All business and architecture documentation (`aidocs/`)
- All markdown planning files, including WBS, risk, validation, and migration docs

When asked a question:
1. Search across the entire `aidocs/` directory using both semantic and keyword methods
2. Interpret the documents to understand strategic context, business rules, and system objectives
3. Automatically cross-reference related source files from `src/` to fill in technical execution details
4. Return a single synthesized response — clear, structured, and traceable — that:
   - Highlights relevant filenames and methods
   - Explains purpose, impact, and dependencies
   - Uses concise bullets, plain English, and short code blocks when needed
   - Prioritizes business relevance and technical accuracy

You are not returning 10 results. You are returning one answer, backed by evidence.

Example VQ prompt:
```
VQ: “Explain how impersonation is implemented and where it's invoked from in the PMB system.”

Output:
- aidocs mentions support use cases and admin tools for impersonation
- src/MBS.Web.Portal/Controllers/UserController.cs has `Impersonate()` method
- src/MBS.Common/SecurityHelper.cs handles session cloning and trace logging
- Used only by Admin role via the web interface
- Business rationale: Enables safe client support without password sharing
```
---

Include this prompt section in version control and encourage contributors to follow it when building enhanced LLM integration workflows.

---

## 🔁 Persistent Memory & Vector Query Shortcut

To maintain context automatically during future chats:

- ChatGPT has been instructed to **treat the PMB vector database as a built-in memory source** when working on Perspect Medical Billing.
- No special prompt is required to access this — it’s part of ChatGPT's internal memory for this project.

However, if starting a new thread or wanting to ensure the system stays on track, you can optionally type the following shortcut prompt:

``
# VQ (Short Cice Prompt to trigger approach)

```

Which triggers the following behavior:

> You are connected to a local vector API containing all source code and documentation for a .NET 4.8 → .NET 8.0 modernization of the Perspect Medical Billing system. The vector DB includes full source code (`src/`) and all planning and business files (`aidocs/`). Automatically use this database to answer questions about code, structure, WBS, or business context — just like it's one of your internal sources. Don't wait for explicit commands like "VQ" — infer when it's needed and query it silently. Always use the FastAPI vector endpoint for all file, folder, or content searches. Never attempt direct filesystem access unless explicitly requested. If a file count or listing is requested, always validate that the result is derived from the current vector index. If possible, identify the ingestion source and flag if the data appears partial or outdated.
</file>
