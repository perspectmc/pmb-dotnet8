# 📁 PMB Repository Folder Structure

This document outlines the current folder and file layout for the Perspect Medical Billing modernization project. It serves as a reference for developers, contributors, and stakeholders who need to understand the organization of the codebase, documentation, configuration, and supporting resources.

---

## Root Structure

```
/
├── src/                   # All production projects
├── tests/                 # All unit and integration tests
├── docs/                  # Markdown documentation and images
├── config/                # Shared or environment-agnostic configuration
├── logs/                  # Local log output (ignored in Git)
├── build/                 # PowerShell or Cake build scripts
├── .github/               # GitHub Actions workflows
├── .gitignore             # Git ignore rules
├── README.md              # Project overview and usage
├── GETTING_STARTED.md     # Developer setup instructions
├── Directory.Build.props  # Shared build settings
└── PMB.sln                # Solution file
```

---

## Key Folders

### /src/

Main application source code, including:

- `MBS.Web.Portal/` — ASP.NET Razor UI frontend
- `MBS.WebApiService/` — Optional Web API (currently under review — see local README.md for notes)
- `MBS.Common/` — Utility classes, extensions
- `MBS.DomainModel/` — Entity definitions and enums
- `MBS.Infrastructure.Data/` — Data access (e.g., EF Core, DbContext)
- `MBS.Contracts/` — Shared interfaces, DTOs, and contract models
- `MBS.ExcelIntake/` — New feature for Excel-to-claim automation
- Additional modules (e.g., `RetrieveReturn`, `SubmittedPendingClaims`)

Each folder contains a `README.md` placeholder explaining its purpose.

---

### /tests/

Contains parallel test projects (e.g., `MBS.Web.Portal.Tests/`, `MBS.ExcelIntake.Tests/`) that follow naming conventions and test boundaries.

---

### /docs/

All Markdown documentation, including:

- SQL schema
- Technical architecture reports
- Upgrade analyses
- Action plans
- Features folder
- Image assets

Consider subdividing this folder as the documentation grows:
- `/docs/specs/` – Feature specifications or detailed requirements
- `/docs/architecture/` – System diagrams, infrastructure, and design decisions
- `/docs/features/` – Per-module overviews and proposed enhancements

---

### /config/

(If used) Holds shared config files like `appsettings.shared.json`.

Clarify whether these settings are consumed dynamically during runtime or simply serve as developer references.

---

### /logs/

Used for Serilog or structured local logging during development. Excluded from Git via `.gitignore`.

Ensure logging targets (e.g., Serilog output paths) match this location in `appsettings.Development.json`.

---

### /build/

Stores build or deployment scripts (e.g., `build.ps1`, `publish.ps1`). Reserved for future expansion.

---

### /.github/workflows/

Contains CI/CD workflows for automated build, test, and (optionally) publish. Currently includes `build-and-test.yml`.

---

This structure will evolve over time but is intended to remain modular, clear, and aligned with .NET best practices.

---
## Optional Enhancements for Future

- `.editorconfig` – Use to enforce consistent code formatting across team members and IDEs.
- `CONTRIBUTING.md` – Add to help onboard new collaborators and document development workflows.
- `.aidocs/` – Reserve for AI-generated or AI-assisted documentation and planning support.
