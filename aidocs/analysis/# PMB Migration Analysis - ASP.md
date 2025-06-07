# PMB Migration Analysis - ASP.NET MVC 4.8 to .NET 8

## Project Overview
- **Legacy System**: ASP.NET MVC 4.8, Entity Framework 6.4.4
- **Target System**: .NET 8, ASP.NET Core 8, EF Core 8
- **Codebase Size**: 88,027 LOC (274 files total)
  - **116 C# files**: 40,584 LOC
  - **35 Razor views**: 3,711 LOC
  - **39 JS files**: 28,782 LOC
  - **45 CSS files**: 9,320 LOC
- **Architecture**: 12 projects (1 web, 8 console, 3 libraries)

## Analysis Progress Tracker
- [x] Repository Structure Analysis (Complete)
- [x] **Step A: Dependency Mapping** ✅ (Complete)
- [x] **Step B: Authentication/Security Patterns** ✅ (Complete)
- [x] **Step C: EF Usage & Custom Code Patterns** ✅ (Complete)
- [x] **Step D: Integration Points & External APIs** ✅ (Complete)
- [x] **Step E: Configuration & Deployment Patterns** ✅ (Complete)
- [ ] Step C: EF Usage & Custom Code Patterns  
- [ ] Step D: Integration Points & External APIs
- [ ] Step E: Configuration & Deployment Patterns

---

## Step B: Authentication/Security Analysis ✅

### Current Authentication Architecture:
**Legacy ASP.NET Membership Provider System**

#### **Core Authentication Components:**
- **Forms Authentication** (web.config based)
- **Membership Provider** with SQL Server backend
- **Role Manager** with role-based authorization
- **60-minute session timeout** with sliding expiration

#### **Security Implementation:**
- **7 Controllers** with `[Authorize]` attributes
- **Role-based access**: Member, Administrator roles
- **Password Policy**: 7+ chars, 1 numeric, 1 special char
- **Account Lockout**: 5 failed attempts max
- **Anti-forgery tokens** implemented

#### **External Authentication:**
- **OAuth Bearer tokens** for MSB API
- **Auth0 integration** endpoints
- **Saskatchewan Health API** authentication

#### **Database Entities:**
- **Users, Memberships, Roles, UserProfiles** tables
- **Password reset tokens** with time limits

### **CRITICAL Migration Requirements:**

#### **Complete Authentication System Replacement:**
1. **Forms Authentication** → ASP.NET Core Authentication middleware
2. **Membership Provider** → ASP.NET Core Identity
3. **Role Manager** → Identity Roles system
4. **FormsAuthentication.SetAuthCookie()** → Identity SignInManager
5. **web.config authentication** → Program.cs/Startup.cs configuration

#### **Database Migration Needs:**
- **AspNet_* tables** → Identity schema migration
- **Custom user profile data** → Identity Extensions
- **Role mappings** → Identity Role claims

---

## Step C: Entity Framework & Code Patterns Analysis ✅

### Current EF Implementation:
**EF 6.4.4 Database-First with EDMX Model**

#### **Core Architecture:**
- **Single DbContext**: `MedicalBillingSystemEntities`
- **EDMX Model**: `MBS_Data_Model.edmx` with T4 generation
- **17 Entities**: Users, Claims, ServiceRecords, etc.
- **7 Repository Classes**: Interface-based pattern

#### **Repository Pattern Implementation:**
```
Controllers → Repositories → DbContext → Database
```
- **Manual DI**: Direct instantiation in controllers (no IoC)
- **IDisposable**: All repositories implement disposal
- **Performance**: `AutoDetectChangesEnabled = false`

#### **Critical EF6 → EF Core Migration Issues:**

##### **HIGH COMPLEXITY:**
1. **EDMX Elimination**: Database-First EDMX → Code-First or Database-First scaffolding
2. **DbContext Changes**: EF6 APIs → EF Core APIs
3. **Entity Configuration**: EDMX mapping → Fluent API/Data Annotations
4. **Connection Strings**: EF6 format → EF Core format

##### **MEDIUM COMPLEXITY:**
5. **Repository Updates**: EF6 methods → EF Core equivalents
6. **Query Syntax**: LINQ differences between versions
7. **Transaction Handling**: Different transaction APIs
8. **Migration Scripts**: EF6 migrations → EF Core migrations

##### **LOW COMPLEXITY:**
9. **Entity Classes**: Mostly compatible (POCOs)
10. **Business Logic**: Repository interfaces remain same

#### **Migration Strategy Options:**
1. **Database-First Scaffolding**: Regenerate from existing DB
2. **Code-First Reverse Engineer**: Convert EDMX to Fluent API
3. **Hybrid Approach**: Gradual migration per entity

---

## Step D: Integration Points & External APIs Analysis ✅

### External Service Dependencies:

#### **CRITICAL MIGRATION IMPACT:**
1. **SOAP Service - InterfaxService** 
   - **Current**: WCF/SOAP-based fax delivery
   - **Impact**: Complete rewrite required (SOAP → REST/gRPC)
   - **Risk**: HIGH - Core business functionality

2. **HTTP API Integrations**
   - **5+ Medical billing APIs** with OAuth authentication
   - **Claims submission endpoints** 
   - **Health check services**
   - **Migration**: HttpClient modernization with DI

#### **File I/O Operations (15+ locations):**
- **Hardcoded file paths** requiring configuration
- **Return file processing** workflows
- **PDF generation** operations
- **Log file management**

#### **Database Integration Points:**
- **FaxDeliver entity** with complex workflows
- **Status tracking** and delivery management
- **File metadata** storage and retrieval

### **Migration Requirements:**

#### **SOAP → Modern API:**
- **Replace WCF ServiceReference** with HTTP client
- **Update InterfaxService** integration
- **Implement retry/resilience** patterns

#### **File Operations Modernization:**
- **Configuration-based paths** (not hardcoded)
- **IFileSystem abstraction** for testability
- **Cloud storage compatibility** preparation

---

## Step E: Configuration & Deployment Analysis ✅

### Configuration Architecture:
**21 Configuration Files** across multi-application structure

#### **Current Configuration Pattern:**
- **web.config/app.config** per application
- **No centralized configuration** management
- **Manual deployment** (no automated scripts found)

#### **Connection String Analysis:**
- **Multiple SQL Server instances**: Development vs Production
- **Hardcoded credentials** in plain text
- **EF EDMX metadata** connection strings
- **External service URLs** hardcoded

#### **CRITICAL SECURITY ISSUES:**
1. **Hardcoded passwords**: "i@mF1nes", "Pass@word1" in source
2. **SQL Authentication**: Not using Windows Authentication
3. **Unencrypted connections**: `Encrypt=False`
4. **Production credentials**: Visible in config files

#### **External Service Configuration:**
- **Claims Processing**: `https://cp.cmcs-skh.ca`
- **eHealth Saskatchewan**: `https://msbcustomerportal.ehealthsask.ca`
- **Database Names**: Inconsistent naming conventions

### **Migration Requirements for .NET 8:**

#### **Configuration System Overhaul:**
1. **web.config → appsettings.json** conversion
2. **Secret management**: Azure Key Vault/User Secrets
3. **Environment-specific configs**: Dev/Staging/Prod
4. **Connection string security**: Encrypted storage

#### **Deployment Modernization:**
1. **CI/CD pipelines**: Replace manual deployment
2. **Configuration transformation**: Environment-based
3. **Infrastructure as Code**: Terraform/ARM templates
4. **Container readiness**: Docker preparation

#### **Security Hardening:**
1. **Remove hardcoded secrets** immediately
2. **Implement Key Vault** integration
3. **Enable SQL encryption**: TLS connections
4. **Windows Authentication**: Where possible

---

## FINAL ANALYSIS SUMMARY

### **HIGH RISK COMPONENTS (Require Complete Rewrite):**
1. **ASP.NET MVC 5.2.7 → Core 8**: Framework migration
2. **Entity Framework 6.x → EF Core 8**: EDMX conversion
3. **ASP.NET Membership → Core Identity**: Auth system
4. **Configuration System**: Security & structure overhaul

### **MEDIUM RISK COMPONENTS:**
1. **Frontend Libraries**: jQuery/Bootstrap updates
2. **Background Services**: Console → Hosted services
3. **API Integrations**: HTTP client modernization

### **LOW RISK COMPONENTS:**
1. **Business Logic**: Repository patterns compatible
2. **External APIs**: Simple third-party integrations
3. **Database Schema**: Minimal changes needed

### **IMMEDIATE ACTIONS REQUIRED:**
1. **Security**: Remove hardcoded credentials
2. **Planning**: Detailed migration sequencing
3. **Environment**: .NET 8 development setup
4. **Testing**: Comprehensive test strategy

---

## Code Complexity Analysis (Lines of Code)

### **Project Size Distribution:**
| Project | C# LOC | Complexity |
|---------|--------|------------|
| **MBS.Web.Portal** | 11,471 | High (Controllers: 4,510, Services: 3,600) |
| **MBS.RetrieveReturn** | 4,928 | Medium-High (Large single app) |
| **MBS.TestCodeUsed** | 7,874 | Medium (Testing utility) |
| **MBS.DataCache** | 10,830 | Medium (Static data) |
| **MBS.ReconcileClaims** | 830 | Low-Medium |
| **MBS.WebApiService** | 844 | Low-Medium |
| **MBS.Common** | 699 | Low |
| **MBS.DomainModel** | 613 | Low (EF entities) |

### **Frontend Complexity:**
- **28,782 LOC JavaScript** (likely jQuery/legacy)
- **9,320 LOC CSS** (Bootstrap 3.x styling)
- **3,711 LOC Razor views** (35 views)

---

## .NET Upgrade Assistant Analysis Integration ✅

### **Microsoft's Official Assessment:**
- **38 total issues** identified across 8 projects
- **25 mandatory fixes** required
- **38 story points** estimated effort
- **SDK-style project conversion** universally needed

### **Critical Findings Alignment:**

#### **Dependency Issues Confirmed:**
- **Microsoft.Bcl deprecated** (no replacement)
- **EntityFramework 6.4.4 → 6.5.1** (then → EF Core 8)
- **Microsoft.AspNet.Providers.Core** deprecated
- **System.Web.Providers** deprecated
- **Redundant packages** built into .NET 8

#### **Project Format Modernization:**
- **All 8 projects** need SDK-style conversion
- **Package reference format** required
- **.NET Framework 4.8 → .NET 8** migration

#### **Identity/Auth Updates:**
- **Microsoft.IdentityModel.Clients.ActiveDirectory** → Microsoft.Identity.Client
- **OAuth token handling** refactoring needed

### **Effort Estimation Validation:**
**Microsoft's 38 story points** aligns with our analysis:
- **High effort projects**: Web Portal (complex MVC)
- **Medium effort**: Console apps with EF dependencies
- **Lower effort**: Utility libraries

### **Phased Approach Confirmation:**
1. **SDK-style conversion** (foundational)
2. **Dependency upgrades** (compatibility)
3. **.NET 8 migration** (core framework)
4. **Performance optimization** (post-migration)

---

## Excel File-by-File Analysis Integration ✅

### **Comprehensive File Analysis Results:**
- **204+ files analyzed** across 25 project sheets
- **File-level risk assessment** with specific modernization actions
- **Detailed technical findings** per component

### **Risk Distribution Analysis:**
- **🔴 24 High-Risk Files**: Require complete rewrite
- **🟠 123 Medium-Risk Files**: Need significant updates  
- **🟢 57 Low-Risk Files**: Minimal changes required

### **Critical High-Risk Components:**

#### **Web Portal (11 High-Risk Files):**
- **AccountController.cs**: ASP.NET Core Identity + JWT migration
- **ClaimsInController.cs**: Extract to service layer + testing
- **ServiceRecordController.cs**: ViewModels + input sanitization
- **UserManagementController.cs**: Audit trails + role management
- **AccountModels.cs**: Identity ViewModels + MFA support
- **ServiceRecordModels.cs**: ViewModel + DTO separation
- **UserManagementModels.cs**: Audit metadata + role decoupling
- **AccountRepository.cs**: Async/await + activity logging
- **ServiceRecordRepository.cs**: Transaction scope + atomic saves

#### **Core Infrastructure (4 High-Risk Files):**
- **MBS_Data_Model.edmx**: Complete EF6 → EF Core migration
- **ClaimSubmitter.cs**: Async + retry + circuit-breaker
- **ReturnParser.cs**: Schema-based JSON + unit tests
- **SecurityHelper.cs**: Encryption scheme + key management

#### **Frontend Modernization (4 High-Risk Files):**
- **jQuery 2.1.1**: Upgrade to 3.6+ or native JS
- **SearchClaimBetaIndex.js**: Modularize + Vue/React candidate
- **ServiceRecordAction.js**: Component framework migration

#### **Deployment & Config (5 High-Risk Files):**
- **web.config**: Migrate to appsettings.json
- **Global.asax**: Convert to Startup.cs + Middleware
- **FTP deployment**: Replace with CI/CD pipeline

### **Specific Modernization Actions Identified:**
1. **Authentication**: ASP.NET Core Identity + JWT + MFA
2. **Data Layer**: EF6 EDMX → EF Core Code-First
3. **Controllers**: Service layer extraction + async patterns
4. **Models**: ViewModel/DTO separation + validation
5. **Repositories**: Async + transactions + logging
6. **Frontend**: jQuery → Modern JS frameworks
7. **Configuration**: web.config → appsettings.json
8. **Deployment**: FTP → CI/CD pipelines
9. **Security**: Modern encryption + key management
10. **Testing**: Unit test coverage for business logic

### **Effort Estimation Refinement:**
- **Base Framework Migration**: 38 story points (Microsoft tool)
- **High-Risk Component Rewrites**: 24 × 3-5 points = 72-120 points
- **Medium-Risk Updates**: 123 × 1-2 points = 123-246 points
- **Frontend Modernization**: 28K LOC JavaScript = 40-60 points
- **Testing & QA**: 25% of development effort = 50-100 points

**Total Estimated Effort: 323-564 story points**

## Step A: Dependency Mapping Analysis ✅

### Package Inventory (47 total packages across 9 projects):

#### **HIGH RISK - Major Breaking Changes**
- **ASP.NET MVC 5.2.7** → ASP.NET Core MVC 8 (Complete rewrite)
- **Entity Framework 6.1.0/6.4.4/6.5.1** → EF Core 8 (Major changes)
- **System.Web.* packages** → ASP.NET Core equivalents
- **Microsoft.AspNet.Web* (5.2.7)** → ASP.NET Core 8

#### **MEDIUM RISK - Significant Updates**
- **jQuery 2.1.1** → Modern jQuery (Security/compatibility)
- **Bootstrap 3.1.1** → Bootstrap 5+ (Breaking CSS changes)
- **Quartz 2.6.2** → Quartz.NET 3.x+ (API changes)
- **NLog 4.7.5** → NLog 5.x (Configuration changes)
- **Microsoft.IdentityModel 5.3.0** → Modern Identity (Auth changes)

#### **LOW RISK - Compatible/Easy Updates**
- **Newtonsoft.Json 13.0.3** → System.Text.Json or keep
- **Common.Logging 3.4.1** → Microsoft.Extensions.Logging
- **Modernizr 2.7.2** → Remove (outdated)

#### **FRAMEWORK VERSION INCONSISTENCIES**
- **Mixed .NET versions**: 4.5, 4.52, 4.72, 4.8
- **EF version conflicts**: 6.1.0, 6.4.4, 6.5.1 across projects

### Critical Migration Blockers:
1. **Entity Framework EDMX** → Code-First conversion required
2. **System.Web dependencies** → Complete web stack replacement
3. **jQuery/Bootstrap versions** → Frontend modernization needed
4. **Authentication system** → Identity framework migration

---

## Migration Complexity Matrix
| Component | Current Version | Target Version | Risk Level | Migration Effort |
|-----------|----------------|----------------|------------|------------------|
| ASP.NET MVC | 5.2.7 | Core 8 MVC | **HIGH** | Complete Rewrite |
| Entity Framework | 6.1.0-6.5.1 | EF Core 8 | **HIGH** | Major Refactor |
| Authentication | IdentityModel 5.3.0 | Core Identity | **HIGH** | Auth System Rebuild |
| jQuery/Bootstrap | 2.1.1/3.1.1 | Modern | **MEDIUM** | Frontend Update |
| Quartz.NET | 2.6.2 | 3.x+ | **MEDIUM** | API Migration |
| NLog | 4.7.5 | 5.x | **MEDIUM** | Config Changes |
| Newtonsoft.Json | 13.0.3 | System.Text.Json | **LOW** | Optional |
| Common.Logging | 3.4.1 | MS Extensions | **LOW** | Simple Replace |

---

## Next Steps:
1. Execute dependency analysis commands
2. Document findings in this artifact
3. Move to Step B upon confirmation