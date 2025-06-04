

# Perspect Medical Billing – Project & Action List

_Last reviewed: May 27, 2025_

This file outlines the full work breakdown for PMB modernization, grouped into strategic phases. Each item tracks project status and priority level.

---

## ✅ Phase 1: System Understanding & Risk Reduction

| Code | Action                                                                                 | Status       |
|------|----------------------------------------------------------------------------------------|--------------|
| P1   | Complete full code and SQL schema review (SOURCE_OVERVIEW, Excel docs)                | ✔ Done       |
| P1   | Confirm backup status and implement daily VPS-to-local incremental backup             | 🟡 In Progress |
| P1   | Audit encryption and privacy handling of PHI                                           | 🟡 In Progress |
| P1   | Begin security hardening plan for VPS (firewall, access lockdown, Cloudflare)         | 🔜 Next       |
| P2   | Review licensing, SSL certs, SMTP usage, legacy connections (Interfax, Azure)         | ⏳ Soon       |
| P2   | Complete SOURCE_OVERVIEW.md and identify unused/legacy files                          | 🟡 In Progress |

---

## ⚙️ Phase 2: Foundation for Modernization

| Code | Action                                                                                 | Status       |
|------|----------------------------------------------------------------------------------------|--------------|
| P1   | Define success: What does a “modernized PMB platform” look like?                      | ✅ Vision Confirmed |
| P1   | Launch working local/dev clone of production system                                   | 🟡 In Progress |
| P1   | Set up version control via GitHub (migrate code, branching, local setup)              | 🔜 Next       |
| P2   | Create impersonation access log (internal, no PHI)                                     | ✏️ Design      |
| P2   | Fix typos and naming errors (e.g., ChangePassowrd, SpecicalCode)                      | 🧹 Cleanup Phase |
| P2   | Start asset cleanup: CSS, unused PDFs, TestCodeUsed/, Azure profiles                  | ✅ Ready       |

---

## 🤖 Phase 3: Automation & Efficiency Projects

| Code | Action                                                                                 | Status       |
|------|----------------------------------------------------------------------------------------|--------------|
| P1   | Excel-to-claims intake automation (spreadsheets → ServiceRecords → Unsubmitted)       | 🛠 Dev Phase  |
| P1   | Add AI-based parser to handle inconsistent Excel formats                              | 🔜 Next       |
| P2   | Introduce `Imported` status for uploaded but unvalidated claims                       | ✏️ Design Spec |
| P2   | Admin-facing upload for MSB reference files (codes, doctors)                          | 🖥️ UX Needed  |
| P2   | Allow manual addition of unverified referring doctors                                 | 🔗 Linked Feature |
| P2   | AI billing rule parser (Colin’s CS50 AI project)                                      | 🧠 Parallel Project |

---

## 🧱 Phase 4: Infrastructure Modernization

| Code | Action                                                                                 | Status       |
|------|----------------------------------------------------------------------------------------|--------------|
| P1   | Plan .NET upgrade path (e.g., .NET 6 or 8) and investigate Membership dependency       | 🔍 Research   |
| P2   | Evaluate AI/hardware dev machine (if not using secondary VPS)                         | 🧪 Research Shared |
| P2   | Replace outdated publishing (FTP/Azure) with GitHub CI or manual pull model           | 🔁 With Git   |

---

## 💬 Phase 5: User & Communication Features

| Code | Action                                                                                 | Status       |
|------|----------------------------------------------------------------------------------------|--------------|
| P2   | Implement secure messaging (in-portal + tokenized email link)                         | ✏️ Design     |
| P2   | Enable error logging + email alert system (to Colin and/or Ben)                       | 🪵 Add to Logging |
| P2   | Improve reports: summaries, insights, future AI analytics                             | 📊 Future Phase |

---

## 🧬 Phase 6: Future Expansion Toward EMR

| Code | Action                                                                                 | Status       |
|------|----------------------------------------------------------------------------------------|--------------|
| P3   | Design architecture for EMR: scheduling, labs, PIP integration                        | 🏗️ Planning Stage |
| P3   | Prepare Ben to mentor Finn (5–10 hrs/week)                                            | 👥 Post-setup |
| P3   | Build business continuity and succession plan                                         | 🛡️ Once Stable |

---

## 🚧 Parallel Projects

- **SonarQube-based code quality review** — Identify and reduce technical debt  
- **Billing Rules as Inference Engine** — Leverage AI for claim logic validation  
- **GitHub Modernization & CI/CD** — Infrastructure uplift and dev workflow overhaul