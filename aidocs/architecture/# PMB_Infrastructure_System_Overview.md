

# PMB Current State Architecture

_Last updated: May 27, 2025_

---

## 🧭 1. Hosting & Infrastructure

- **Platform:** ASP.NET Web Application (Web Forms + MVC Hybrid)  
- **Hosting:** Windows-based VPS  
- **Web Server:** IIS (Internet Information Services)  
- **Database:** Microsoft SQL Server 2017  
- **Email:** SMTP integration (configured, working)  
- **Fax:** Interfax (used exclusively for WCB submissions)  
- **CI/CD:** None — code is promoted manually via FTP or RDP

---

## 🗂️ 2. Project Folder Structure & Roles

| Folder                  | Purpose                                                                 |
|-------------------------|-------------------------------------------------------------------------|
| `MBS.Web.Portal`        | Main application: Razor views, jQuery, controllers, forms, email, PDFs  |
| `MBS.DomainModel`       | Entity definitions for claims, users, transactions                      |
| `MBS.Common`            | Helpers: string formatting, security, HTML encoding                     |
| `MBS.RetrieveReturn`    | Parser for MSB return files (`returns.TXT`) and claim status updater     |
| `MBS.ReconcileClaims`   | Legacy reconciliation logic (batch-style)                                |
| `MBS.PasswordChanger`   | Standalone password reset utility (manual use; method typo present)      |
| `MBS.ApplicationInitializer` | Auto-generated; no business logic                                  |
| `App_Data`              | Static PDFs, templates, legacy assets                                   |
| `App_Start`             | Standard ASP.NET startup config (routes, bundles, filters)              |
| `SQLDatabaseProj`       | DB schema project (now fully exported to Excel)                         |
| `MBS.TestCodeUsed`      | Deprecated code, not used                                               |
| `.nuget`, `packages`    | NuGet metadata and restore configs                                      |

---

## 🔄 3. Core Claims Lifecycle

1. **Claim Entry**  
   - User logs into the portal  
   - Creates a new claim (`ServiceRecord`) via web form  

2. **Claim Submission**  
   - Claims grouped into `ClaimsIn`  
   - Submitted to MSB via API (modern flow)

3. **Return File Handling**  
   - MSB processes claims → returns.TXT  
   - `RetrieveReturn` parses and updates:
     - `PaidClaim`  
     - `RejectedClaim`  
     - `Held` (status tracking)

4. **WCB Flow**  
   - WCB claims generate `WCB_Billing_Form.pdf`  
   - Sent via Interfax

5. **Admin Reporting & Email**  
   - PDF report generation  
   - Bulk email (manual, 500-char limit)  
   - Admin impersonation (no logging)

---

## 🗄️ 4. Database Schema (Summary)

| Table                   | Description                                  |
|-------------------------|----------------------------------------------|
| `ServiceRecord`         | Main claim unit                              |
| `ClaimsIn`              | Submission batch container                   |
| `ClaimsInReturn`        | MSB response file model                      |
| `RejectedClaim`, `PaidClaim` | Processed outcomes                    |
| `UserProfile`, `Role`   | Authentication and role tracking             |
| `BillingCode`, `DiagnosticCode`, `ReferringDoctor` | Reference data  |

**Notes:**  
- No ORM (e.g., Entity Framework)  
- Likely uses ADO.NET or inline SQL

---

## 🔌 5. Integration Points

- **MSB API:** Claim submission  
- **Interfax API:** PDF WCB submission  
- **SMTP:** Admin emails and system messages  
- ❌ No microservices or API abstraction layer

---

## ⚠️ 6. Known Gaps / Limitations

- No impersonation logging or audit trail  
- No version control (e.g., Git)  
- No CI/CD pipeline  
- Hardcoded values in Razor views  
- Legacy/unmaintained folders still present  
- No structured error logging  
- No rule automation or AI integration

---

## 🧱 7. Current-State System Diagram (Textual)

```
User → Web Portal (Razor/jQuery) → ServiceRecord
     → ClaimsIn → MSB API
     → returns.TXT → ClaimsInReturn
     → PaidClaim / RejectedClaim / Held
     → PDF Reporting, Interfax (WCB), Admin Email
```

---

## ✅ Summary

This architecture is **functional but legacy**:
- Monolithic ASP.NET stack
- Tightly coupled logic
- Manually deployed
- Fragmented tools (PDF, email, fax)

However, the system is well-positioned for modernization:
- Clear separation of business logic
- API-based submission already in place
- Aligned with domain-driven structure

This serves as a reference foundation for future-state design and gap closure planning.