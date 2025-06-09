# 🧠 Vector API Functionality – Task List (PMB Platform)

This document outlines future enhancements to the `vector_api.py` interface used for AI-assisted development, codebase analysis, and modernization tracking of the Perspect Medical Billing platform.

---

## ✅ Core Features to Add

### 1. 🔍 DIFF Support
- **Endpoint:** `POST /diff`
- **Description:** Compare any two files (by name or content). Return raw diff + natural-language summary.
- **Purpose:** Code reviews, upgrade verification, GPT/Claude output comparisons.

---

### 2. 📂 File Listing
- **Endpoint:** `GET /files`
- **Description:** Return all indexed files, filterable by type (`.cs`, `.md`, etc.) or folder.
- **Purpose:** File navigation, Claude integration, auditing.

---

### 3. 🧠 Summarization
- **Endpoint:** `POST /summarize`
- **Description:** Summarize a specific file or class. Include role, responsibilities, key interactions.
- **Options:** Toggle security relevance (e.g. PIPEDA, audit risk).

---

### 4. 🔗 Usage Tracing
- **Endpoint:** `POST /trace`
- **Description:** For a class/method/enum name, find all files that use it.
- **Purpose:** Safe refactoring, onboarding, billing rule integration.

---

### 5. 🏷️ Custom Tagging
- **Endpoint:** `POST /tag`, `GET /tags`
- **Description:** Assign custom tags to files (e.g. `"legacy"`, `"security-critical"`, `"to-modernize"`).
- **Purpose:** Prioritized reviews, scoped modernization planning, dev filters.

---

### 6. 📥 Upload Changed Files (Optional)
- **Endpoint:** `POST /upload`
- **Description:** Send updated files into vector index without full reparse.
- **Purpose:** Useful in CI/CD workflows or interactive review sessions.

---

### 7. 🧪 Prompt Sandbox (HTML Frontend)
- **Feature:** UI with prebuilt prompt buttons (Summarize, Trace, Diff)
- **Purpose:** Junior dev and business user friendly interface.

---

## 🔧 Notes
- All features should respect vector index consistency.
- Audit log or lightweight usage tracking may be added later.
- Deprecate direct Python filesystem access (e.g., `os.walk`, `Path.rglob`) in favor of vector API endpoints (`GET /files`, `GET /file-content`) to avoid macOS sandbox issues and maintain consistent visibility.

## 📌 Next Testing Tasks

- [ ] Test `POST /diff` endpoint with two sample files to ensure diff and summary are returned.
- [ ] Test `GET /files` endpoint to verify full file listing and filter parameters.
- [ ] Test `POST /summarize` endpoint on a sample `.cs` and `.md` file.
- [ ] Test `POST /trace` endpoint for a known class name to validate usage tracing.
- [ ] Test `POST /tag` and `GET /tags` endpoints to confirm tagging functionality.
- [ ] Test `POST /upload` endpoint with a new or modified file to ensure partial re-indexing.
- [ ] Verify Prompt Sandbox UI correctly triggers each endpoint via buttons.