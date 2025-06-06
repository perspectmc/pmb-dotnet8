# Current State Architecture Report

**Project Name:** PMB  
**Prepared by:** Colin  
**Date:** May 27, 2025  

**Purpose:**  
A detailed review of the current architecture to assess modernization needs, upgrade risks, and best practices for future development.

---

## 1. Overview of Current Architecture

### 1.1 Technology Stack

- **Backend:** .NET Framework (Legacy)  
- **Database:** SQL Server  
- **Frontend:** ASP.NET MVC with jQuery, Bootstrap  
- **API:** Custom Web API with OAuth authentication  
- **Entity Framework:** Legacy `.edmx` model (requires migration)  
- **Deployment:** IIS-hosted using FTP/Web Deploy (outdated)

### 1.2 Key Business Functions

- **Claim Processing**  
  Submission, reconciliation, reporting

- **User Management**  
  Authentication, certificate-based security, role-based access control

- **Billing & Finance**  
  MSB claim validation, transaction history, payment tracking

---

## 2. Application Layers

### 2.1 Presentation Layer (UI & Web Portal)

- `MVC Controllers` handle HTTP requests  
  - Example: `AccountController.cs`, `ClaimsInController.cs`

- `Views` use Razor templates

- `JavaScript Dependencies`  
  - Outdated jQuery 2.1.1 (should upgrade to 3.6)  
  - Bootstrap 3 (should upgrade to Bootstrap 5)

- Static assets (CSS, images) located in `/Content`

### 2.2 Business Logic Layer

- **Claims Processing**  
  - `ClaimSubmitter.cs`, `ServiceRecordRepository.cs`

- **Security & Authentication**  
  - `OAuthMessageHandler.cs`

- **Caching**  
  - `CacheModels.cs` (optimization needed)

### 2.3 Data Access Layer

- **Entity Framework**  
  - `.edmx` model: `MBS_Data_Model.edmx`  
  - Requires migration to EF Core Code First

- **Database**  
  - Core SQL tables (`ClaimsIn.sql`, `ServiceRecord.sql`, `Users.sql`)  
    - ⚠️ *Note: The legacy `SQLDatabaseProj` project has been marked obsolete by the original developer (June 6, 2025), but its schema content was historically used for reference.*

- **Repositories**  
  - `ClaimsInRepository.cs`, `HomeRepository.cs`

### 2.4 Infrastructure & Deployment

- **Hosting**  
  - IIS (FTP/Web Deploy — needs modernization)

- **Configuration Management**  
  - `App.config` / `Web.config` → should migrate to `appsettings.json`

- **Build & CI/CD**  
  - No structured CI/CD pipeline detected  
  - Historical Azure DevOps integration was present in some folder structures  
  - Needs GitHub Actions or clean Azure DevOps pipeline

---

## 3. Risks & Recommended Improvements

### 🔴 High Priority Issues

- Legacy ORM (`.edmx`) → migrate to EF Core Code First  
- FTP deployment usage → retire in favor of CI/CD pipeline  
- Outdated JavaScript (jQuery 2.1.1, Bootstrap 3) → upgrade or replace  
- No structured logging → implement Serilog or Application Insights

### 🟠 Medium Priority Issues

- Repository redundancy → optimize queries and caching  
- Config files reliance → move to `appsettings.json`  
- Package management → convert `.csproj` files to SDK-style format

---

## 4. Next Steps

- ✅ Phase 1: Run .NET Upgrade Assistant for compatibility analysis  
- ✅ Phase 2: Refactor obsolete dependencies and configurations  
- ✅ Phase 3: Upgrade testing and validation for modern architecture  
- ✅ Phase 4: CI/CD implementation for secure deployment

---

## Summary

This report provides a structured snapshot of the application's architecture, its current limitations, and recommended modernization actions. The next step is to use this as a reference when planning the upgrade.
