# 🧠 PMB AI Vector API Setup (Local + Public Access)

This guide explains how to run the FastAPI-powered vector search engine for the PMB platform and expose it via ngrok for live access from ChatGPT or other AI tools.

---

## 🔧 Requirements

- Python 3.10+
- Chroma vector DB already built (`ingest_pmb.py`)
- FastAPI + Uvicorn
- ngrok (installed via Homebrew)

---

## 🟩 1. Start the Local Vector API

```bash
cd ~/Desktop/PMB\ dotnet8/vectors/scripts
python3 vector_api.py
```

You should see:

```
✅ Connected to pmb_complete with 5244 documents
🚀 Starting PMB Vector Search API...
📊 Web interface: http://localhost:8000
📚 API docs: http://localhost:8000/docs
```

You can now test the vector search via the browser at `http://localhost:8000` or use the Swagger docs UI at `http://localhost:8000/docs`.

---

## 🌍 2. Expose the API with ngrok

This step allows ChatGPT (or any external tool) to query your local FastAPI service.

### 🧪 Step-by-step:

1. Install ngrok via Homebrew:
   ```bash
   brew install --cask ngrok
   ```

2. Authenticate ngrok with your account (only needed once):
   - Get your token from: https://dashboard.ngrok.com/get-started/your-authtoken
   - Then run:
     ```bash
     ngrok config add-authtoken YOUR_TOKEN_HERE
     ```

3. Start the tunnel:
   ```bash
   ngrok http 8000
   ```

   You'll see output like:
   ```
   Forwarding https://a1b2c3d4.ngrok-free.app → http://localhost:8000
   ```

4. Copy the `https://...ngrok.io` URL and paste it into ChatGPT when prompted, or use it in any remote tool to send requests to your API.

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
You are a senior AI engineer helping modernize a commercial medical billing platform called Perspect Medical Billing (PMB). The platform includes legacy .NET 4.8 source code, markdown documentation, and business planning files. You are connected to a vector database that contains the full content of both the source code and documentation.

When asked a question, you will:
1. Search the vector database using semantic and exact keyword matches.
2. Reference the actual content found — code, markdown, or config — not just summaries.
3. Interpret the content to infer how it functions in production, how it was designed, and what impact changes may have.
4. Always prioritize clarity, using concise bullet points, small code blocks, and plain language reasoning.
5. Highlight business rationale where applicable, especially for architectural or modernization decisions.
6. Provide relevant file paths and class/method names for traceability.
7. Avoid speculative or verbose responses unless explicitly requested.

Example prompt:
```
Search context: Modernizing PMB for .NET 8  
Query: “Explain the full lifecycle of a claim in production and where in the source code each state transition occurs.”  
Output:  
- Shows code for status transitions: Unsubmitted → Submitted → Paid/Held/Rejected  
- Lists source files like `ClaimSubmissionService.cs`, `RetrieveReturn.cs`, etc.  
- Includes reasoning chain explaining how data flows between layers  
- Highlights business rule enforcement locations  
```

---

Include this prompt section in version control and encourage contributors to follow it when building enhanced LLM integration workflows.