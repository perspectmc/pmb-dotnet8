# .NET Upgrade Analysis Report

**Project Name:** MBS Application  
**Prepared by:** Colin  
**Date:** May 27, 2025  
**Target Upgrade:** .NET 8.0  
**Tools Used:** .NET Upgrade Assistant

---

## 📝 Overview

This report documents the results of a full upgrade analysis across the Perspect Medical Billing application. Using the .NET Upgrade Assistant, eight project components were evaluated for compatibility, dependencies, and conversion readiness for .NET 8.0. Recommendations are prioritized by risk, and the summary includes estimated effort in story points.

---

## 1. Summary of Findings

- Projects analyzed: 7  
- Total issues identified: 38  
- Mandatory fixes: 25  
- Optional upgrades: 7  
- Potential enhancements: 6  
- Estimated upgrade effort: 38 story points  
- Two projects (MBS.ReconcileClaims, SQLDatabaseProj) marked obsolete and excluded from scope

**Key upgrade categories:**
- NuGet dependency updates
- Project format conversions
- Framework compatibility adjustments

---

## 2. Project-Specific Issues & Recommendations

### 2.1 MBS.Common
- Microsoft.Bcl deprecated → no supported version  
- Microsoft.Net.Http → replace with System.Net.Http 4.3.4  
- Convert to SDK-style `.csproj`  
- Upgrade from .NET Framework 4.8 → .NET 8.0

### 2.2 MBS.DataCache
- Project format conversion required  
- Upgrade to .NET 8.0

### 2.3 MBS.DomainModel
- EntityFramework 6.4.4 → upgrade to 6.5.1  
- Convert to SDK format  
- Upgrade to .NET 8.0

### 2.4 MBS.PasswordChanger
- Deprecated: Microsoft.AspNet.Providers.Core, System.Web.Providers  
- Convert to SDK format  
- Upgrade to .NET 8.0

### 2.5 MBS.ReconcileClaims (Archived)
- This project is marked obsolete by the original developer (Ben).
- No upgrade work required unless needed for legacy reference.

### 2.6 MBS.SubmittedPendingClaims
- EntityFramework 6.4.4 → upgrade to 6.5.1  
- Convert project format  
- Upgrade to .NET 8.0

### 2.7 MBS.TestCodeUsed
- EntityFramework 6.4.4 → upgrade to 6.5.1  
- Convert project format  
- Upgrade to .NET 8.0

### 2.8 MBS.WebApiService
- Outdated/duplicated packages:
  - Microsoft.NETCore.Platforms, System.IO, System.Net.Http
  - System.Security.Cryptography.\*, Microsoft.IdentityModel.Clients.ActiveDirectory  
- Migrate to Microsoft.Identity.Client  
- Remove redundant packages (now built into .NET 8)  
- Convert to SDK-style project  
- Upgrade to .NET 8.0-windows

### 2.9 SQLDatabaseProj (Archived)
- Marked obsolete by developer; excluded from .NET 8 upgrade.
- Retain for reference only. No further action required.

---

## 3. Key Upgrade Recommendations

### 🔴 Mandatory Fixes
- Convert all project files to SDK-style format  
- Remove deprecated NuGet packages  
- Upgrade Entity Framework to 6.5.1  
- Remove redundant packages already built into .NET 8.0  
- Migrate from .NET Framework 4.8 → .NET 8.0

### 🟠 Recommended Enhancements
- Implement structured logging (e.g., Serilog)  
- Optimize SQL queries for performance  
- Refactor OAuth and token handling in Web API  
- Introduce CI/CD with GitHub Actions or Azure DevOps  
- Review and refactor redundant repository code

---

## 4. Next Steps

- ✅ Phase 1: Convert project files to SDK-style format  
- ✅ Phase 2: Upgrade NuGet dependencies and remove deprecated packages  
- ✅ Phase 3: Migrate to .NET 8.0 and validate compatibility  
- ✅ Phase 4: Apply performance and security optimizations

---

## 5. Useful Links

- [.NET Upgrade Assistant Guide]  
- [NuGet Package Deprecation Announcements]  
- [Entity Framework Migration Guide]  
- [SDK-style Project Format Documentation]

---

## Final Thoughts

This upgrade plan provides a clear, prioritized roadmap to modernize the application stack, reduce technical debt, and prepare the system for long-term maintainability on the .NET 8 platform.