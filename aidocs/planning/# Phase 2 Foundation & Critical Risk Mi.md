# Phase 2: Foundation & Critical Risk Mitigation - Live Progress Tracker

## Overview
- **Priority**: Address highest-risk components first
- **Effort**: 80-100 story points
- **Timeline**: Weeks 4-6
- **Current Status**: Phase 2.2 Authentication System Overhaul

---

## 2.1 Environment Setup & High-Risk Assessment (12 SP)

### 2.1.1 Set up .NET 8 development environment (2 SP)
- [ ] Development environment configuration
- [ ] Package management setup
- [ ] Testing framework installation
- **Status**: Pending Windows machine build

### 2.1.2 Create migration branch and backup systems (3 SP)
- [ ] Git branch strategy implementation
- [ ] Cloud backup system implementation
- [ ] Backup restore testing procedures

### 2.1.3 🔴 CRITICAL: Security audit and credential removal (5 SP)
- [x] **COMPLETE**: Full credential audit across all projects
- [x] **COMPLETE**: Email security alert sent to Ben
- [x] **COMPLETE**: Security findings documented

**🚨 SECURITY AUDIT RESULTS - DEVELOPMENT CREDENTIALS CONFIRMED:**

**BEN'S FEEDBACK CLARIFICATION:**
- **Development credentials only** - Config files are for dev machine, not production
- **Production configs secure** - Not stored in repository (proper security practice)  
- **IIS protection active** - Web server blocks access to config files
- **Azure references outdated** - No longer using Azure deployment profiles
- **Repository security focus** - Source code access control is the actual security concern

**REVISED FINDINGS:**
- **5 unique development passwords** found across 9 projects (appropriate for dev environment)
- **Production safety confirmed** - Ben maintains separate, secure production configs
- **Migration planning validated** - Correctly identified development environment scope
- **VPS deployment approach** - Ben handles production config separately during cutover

**MIGRATION IMPACT:**
- **Development testing** - Can use existing dev credentials for local .NET 8 testing
- **Production deployment** - Ben's secure config management practices maintained
- **No emergency changes needed** - Development credentials serve their intended purpose
- **Security focus** - Repository access controls and source code security priority

### 2.1.4 Document 24 high-risk files and modernization sequence (2 SP)
- [x] **COMPLETE**: Authentication system analysis (855 LOC total)
- [x] **COMPLETE**: Story point validation for Phase 2.2
- [x] **COMPLETE**: Business risk assessment

**ANALYSIS RESULTS:**
- **Low migration risk** - Well-structured code with standard patterns
- **Framework substitution approach** - Preserve business logic, modernize infrastructure
- **Medical billing data preserved** - UserProfiles table unchanged

---

## 2.2 Authentication System Overhaul (🔴 HIGH RISK) (25-30 SP)

### Business Context Confirmed:
- **User Management**: Admin creates healthcare provider accounts with medical billing profiles
- **Session Management**: 60min → 10min timeout (admin configurable)
- **Email Management**: Users can change email, username stays stable for login
- **Medical Integration**: Group User Key from Physician Registry enables claims submission
- **Compliance**: PIPEDA audit logging with admin performance controls

### 2.2.1 AccountController.cs → ASP.NET Core Identity migration (8 SP)
- [x] **COMPLETE**: Identity infrastructure setup
- [x] **COMPLETE**: Authentication method migration
- [x] **COMPLETE**: User management workflow preservation

**MIGRATION APPROACH:**
- **Forms Authentication → SignInManager**: Modern authentication patterns
- **Session timeout**: Admin configurable (default 10min vs current 60min)
- **User creation workflow**: Preserved exactly - admin creates, users login immediately
- **Medical profile integration**: UserProfiles relationship maintained

### 2.2.2 AccountModels.cs → Identity ViewModels (6 SP)
- [x] **COMPLETE**: Password change model migration
- [x] **COMPLETE**: Email change model migration

**BUSINESS REQUIREMENTS PRESERVED:**
- **Password policy upgrade**: 7 → 12+ characters (healthcare standards)
- **Email change capability**: Doctors can update email addresses
- **Username stability**: Login credentials don't change when email changes
- **UI/UX identical**: Forms look and work exactly the same

### 2.2.3 AccountRepository.cs → Async Identity operations (8 SP)
- [x] **COMPLETE**: Repository interface modernization
- [x] **COMPLETE**: EF6 → EF Core 8 conversion
- [x] **COMPLETE**: PIPEDA compliance logging

**TECHNICAL IMPROVEMENTS:**
- **Async patterns**: Better performance under load
- **Dependency injection**: Modern .NET 8 patterns
- **Medical data access**: UserProfiles, UserCertificates unchanged
- **Configurable audit logging**: Admin dashboard control for performance

### 2.2.4 Migrate AspNet_* tables to Identity schema (5 SP)
**CURRENT FOCUS**: Database schema migration for new VPS deployment

**ACTUAL DEPLOYMENT ARCHITECTURE:**
- **New VPS environment** with .NET 8 application
- **Database migration** from current server to new VPS
- **Production cutover** during maintenance window
- **Critical requirement**: Maintain exact database structure compatibility

### 2.2.4a: Schema Migration for VPS Deployment (2 SP)

**Development Phase:**
- **Local testing**: Prove migration works with sample database
- **Schema validation**: Ensure new Identity tables work with existing medical data
- **Wipe and repeat**: Test multiple times until process is bulletproof

**Production Migration Strategy:**
- **Current VPS**: Shut down to prevent new data entry
- **Database export**: Full production database backup
- **New VPS**: Import database + run Identity schema migration
- **Validation**: Confirm all medical billing data intact

### 2.2.4b: Database Structure Compatibility (2 SP)

**CRITICAL BUSINESS REQUIREMENT:**
**Same database structure** = New .NET 8 app must read existing medical data exactly as-is

**Schema Preservation:**
- **UserProfiles table**: ZERO changes to medical billing fields
- **Claims data**: All existing tables preserved unchanged
- **Medical relationships**: FK connections maintained
- **Group User Keys**: Physician Registry integration unaffected

**🚨 BUSINESS RISK:**
**Database compatibility failure** = New VPS system cannot read medical billing data
**Mitigation**: Extensive testing with production data copy before cutover

### 2.2.4c: Production Cutover Validation (1 SP)

**Maintenance Window Process:**
1. **Shutdown current system** - No new data entry
2. **Export production database** - Full backup with medical data
3. **Import to new VPS** - Database restore + Identity migration
4. **Validation tests** - Sample doctor logins, medical profile access
5. **Go live** - Switch DNS/routing to new VPS

**Success Criteria for Cutover:**
- [ ] All existing doctors can login with current passwords
- [ ] Medical profiles display with all billing fields intact
- [ ] Claims submission works with Group User Key validation
- [ ] Admin can manage users exactly as before
- [ ] All medical billing data accessible and unchanged

**BIGGEST RISK IDENTIFIED:**
Database migration must preserve medical billing data relationships perfectly

---

## 2.3 Entity Framework Core Migration (🔴 HIGH RISK) (20-25 SP)

### Business Context:
- **Current**: Entity Framework 6.4.4 with EDMX model (17 entities)
- **Target**: EF Core 8 with modern patterns
- **Critical**: Medical billing data structure UNCHANGED for VPS compatibility
- **Approach**: Database-first scaffolding to preserve existing schema

### 2.3.1 EDMX → EF Core Code-First conversion (15 SP)
**CURRENT FOCUS**: Converting legacy EDMX to modern EF Core patterns

**MIGRATION SCOPE:**
- **MBS_Data_Model.edmx**: Large EDMX file with T4 generation
- **17 entities**: Users, Claims, ServiceRecords, UserProfiles, etc.
- **Complex relationships**: Medical billing data interconnections
- **Performance settings**: AutoDetectChangesEnabled = false preserved


### 2.3.1x: Schema Compatibility Assurance

The .NET 8 application must read and interact with the existing medical billing database **without requiring any schema changes**.

**Requirements:**
- Tables like `UserProfiles` and `ClaimsIn` remain unchanged.
- Relationships and data types are preserved as-is.
- Production database is the schema source of truth.

**Implication:**
All EF Core scaffolding and testing steps must comply with this compatibility rule.

### 2.3.1a: Database-First Scaffolding Strategy (5 SP)

**PRODUCTION SCHEMA SCAFFOLDING APPROACH:**
- **Download production database backup** to development environment
- **Restore production database locally** for scaffolding
- **Generate EF Core entities** from exact production schema
- **Perfect compatibility guarantee**: New VPS reads production data identically

**Business Benefit:**
- **Zero compatibility risk**: Doctors will see no difference
- **Real medical data structure**: All recent schema changes included
- **Live sync potential**: Can refresh development database anytime

**Process:**
1. **Production backup download** → Development restore
2. **EF Core scaffolding** from production schema
3. **Entity validation** with real medical billing structure
4. **VPS deployment confidence**: Perfect schema match guaranteed

**🚨 BUSINESS DECISION CONFIRMED:**
**Production database backup approach** ensures zero-risk VPS migration

### 2.3.1b: Entity Configuration Migration (5 SP)

**EDMX FILE CONFIRMED:**
- **Size**: 109KB (substantial model)
- **Framework**: Entity Framework 3.0 with SQL Server 2012+ compatibility
- **Last Updated**: June 6, 2025 (recently modified)
- **Location**: Source code (separate from database backup)

**MIGRATION APPROACH DECISION:**
**Option A: Reverse Engineer from Production Database** (Recommended)
- **Clean slate**: Generate fresh EF Core entities from production schema
- **Modern patterns**: Let EF Core create optimal configurations
- **No legacy baggage**: Avoid EF 3.0 → EF Core 8 conversion complexity

**Option B: Convert Existing EDMX Mappings**
- **Preserve current**: Convert 109KB EDMX to Fluent API
- **Complex conversion**: EF 3.0 patterns → EF Core 8 patterns
- **Risk**: Legacy configurations might not be optimal

**KEY ENTITIES IDENTIFIED FOR MEDICAL BILLING:**
- **ClaimsIn**: Medical claims processing (ClaimAmount, PaidAmount, Content)
- **ClaimsInReturn**: Claims return processing (financial totals)
- **UserProfiles**: Medical practitioner data (15+ fields)
- **Applications**: Application registration (GUID-based)

**🚨 BUSINESS LOGIC ALERT:**
**Recommendation**: Reverse engineer from production database (Option A)
**Rationale**: Cleaner, more reliable, leverages EF Core best practices
**Risk**: Must validate all medical billing entity relationships work identically

**CRITICAL MEDICAL DATA PRESERVATION:**
- **Financial data**: ClaimAmount, PaidAmount, TotalPaidAmount
- **Claims content**: varchar(max) fields with complete claim information
- **File tracking**: SubmittedFileName, FileSubmittedStatus
- **Audit trails**: CreatedDate, DownloadDate, status changes

### 2.3.1c: Validation & Testing (5 SP)

**STANDARD EF CORE VALIDATION PROCESS:**
Simple validation to ensure new .NET 8 app reads database correctly

### 2.3.1c.1: Scaffolding Validation (2 SP)

**Automated Entity Generation Testing:**
- **Run EF Core scaffolding** on production database copy
- **Verify entity classes generated** for all medical billing tables
- **Check relationships** between ClaimsIn, UserProfiles, etc.
- **Validate data types** match existing database columns

**Expected Outcome:**
- **Clean entity classes** generated automatically
- **Medical billing tables** properly mapped
- **No missing tables** or relationships

### 2.3.1c.2: Medical Data Access Testing (2 SP)

**Core Medical Billing Functions:**
- **Read UserProfiles** - Doctor information displays correctly
- **Access ClaimsIn data** - Medical claims load properly
- **Financial calculations** - ClaimAmount, PaidAmount fields work
- **Claims processing** - Submit/retrieve workflows function

**Testing Approach:**
- **Sample doctor login** - Verify profile loads
- **Claims data queries** - Medical billing information accessible
- **Financial reporting** - Payment amounts display correctly

### 2.3.1c.3: Performance Validation (1 SP)

**Ensure No Performance Degradation:**
- **Claims processing speed** - Same performance as current system
- **Database query efficiency** - EF Core optimizations working
- **Large claims handling** - varchar(max) content fields load properly

**Success Criteria:**
- [ ] All medical billing tables accessible through EF Core
- [ ] Doctor profiles and claims data display identically
- [ ] Financial calculations work correctly
- [ ] No performance degradation vs current system
- [ ] Ready for VPS deployment with confidence

**BUSINESS OUTCOME:**
Proven that new .NET 8 app reads existing medical billing database perfectly

---

## 2.4 Configuration Security Overhaul (🔴 HIGH RISK) (10-15 SP)

To standardize operational communications, all hardcoded personal email addresses will be replaced with `info@perspect.ca`.

**Changes Required:**
- `poch_ben@hotmail.com` → `info@perspect.ca`
- `ben@perspect.ca` → `info@perspect.ca`

**Update Locations:**
- `ConfigHelper.cs` default values
- `web.config` and `appsettings.*.json`

This ensures all error notifications and system alerts use an official support contact.
Consider makeing this a user defined variable from admin panel in case I sell system then new use can update with their email 

### Business Context:
- **Current**: web.config with hardcoded credentials (development environment)
- **Target**: Modern appsettings.json with secure configuration management
- **Production**: Ben handling production security separately
- **VPS Deployment**: Clean configuration structure for new environment

### 2.4.1 web.config → appsettings.json migration (8 SP)
**CURRENT FOCUS**: Development environment configuration modernization

**MIGRATION SCOPE:**
- **21 configuration files** across multiple projects
- **Development security** - Replace hardcoded credentials with User Secrets
- **Environment-specific configs** - Development, staging, production separation
- **VPS deployment preparation** - Clean configuration structure

### 2.4.1a: Configuration File Consolidation (3 SP)

**Current Configuration Chaos:**
- **web.config** - Main web application settings
- **Multiple app.config files** - One per console application project
- **Deployment profiles** - Various environment configurations
- **Hardcoded values** - Database connections, SMTP settings, API endpoints

**Target Configuration Structure:**
- **appsettings.json** - Base configuration
- **appsettings.Development.json** - Development-specific settings
- **appsettings.Production.json** - Production environment (for VPS)
- **User Secrets** - Development credentials (not in source control)

**Business Benefit:**
- **Clean VPS deployment** - No hardcoded credentials in new environment
- **Environment separation** - Development vs production configurations
- **Security improvement** - Credentials stored securely

### 2.4.1b: Development Environment Security (3 SP)

**Replace Hardcoded Development Credentials:**
- **Database connections** - Move to User Secrets for development
- **SMTP settings** - Secure email configuration for development
- **API endpoints** - Environment-specific medical services endpoints
- **External service credentials** - Development vs production API keys

**Development Security Approach:**
- **User Secrets** - Local development credentials (encrypted, not in source)
- **Environment variables** - Alternative secure credential storage
- **Configuration validation** - Ensure required settings present

**🚨 BUSINESS LOGIC ALERT:**
**Question**: For development environment, should we use real SMTP settings for testing email functionality, or mock email service?
**Impact**: Real SMTP = full testing capability, Mock = no accidental emails sent

### 2.4.1c: VPS Deployment Configuration (2 SP)

**Production-Ready Configuration Structure:**
- **Environment-specific settings** - Database connections per environment
- **External service endpoints** - Medical Services Branch API configurations
- **Performance settings** - Session timeouts, connection pooling
- **Logging configuration** - PIPEDA audit logging settings

**VPS Deployment Benefits:**
- **Clean configuration** - No development artifacts in production
- **Secure credential management** - Production credentials handled properly
- **Environment flexibility** - Easy configuration for different VPS environments

---

## Critical Files Identified from Analysis
**🔴 Authentication High-Risk Files:**
- AccountController.cs (613 LOC) - Forms Auth → Core Identity
- AccountModels.cs (121 LOC) - Password/email models
- AccountRepository.cs (121 LOC) - Data access modernization
- SecurityHelper.cs - Encryption modernization needed

**🔴 Configuration High-Risk Files:**
- web.config - Hardcoded credentials removal
- Global.asax.cs - Core middleware conversion
- Multiple app.config files - Security cleanup

**🔴 Entity Framework High-Risk:**
- MBS_Data_Model.edmx - Complete EDMX → EF Core migration

---

## Current Status Summary
**✅ COMPLETED (Phase 2.1 + 2.2):**
- Security audit and production notification
- Authentication system analysis and migration planning
- Complete authentication trilogy migration approach


# Phase 2.4.2: Global.asax → Startup.cs Conversion (7 SP)
Current Status: WAITING FOR PMB GLOBAL.ASAX ANALYSIS
STEP 1: PMB Global.asax Analysis - VERIFYING ASSUMPTIONS
STEP 1: PMB Global.asax Analysis - ASSUMPTIONS VERIFIED
✅ Assumption #1: Error Email Notifications - VERIFIED
Email Configuration:

Default Email: poch_ben@hotmail.com (Ben's personal email)
Configurable: Can be overridden in web.config with "SupportEmail" setting
Error Types: ALL unhandled exceptions in the web application
Email Content: Exception message, memory usage, stack trace, inner exceptions

🚨 MIGRATION REQUIREMENT:
Change ALL email addresses from Ben's personal emails to info@perspect.ca for development environment

# Other Email Notifications Found:

Certificate Expiry: ben@perspect.ca (GetCertExpiryEmail)
Claims Processing: Same support email for batch job errors
Service Record Errors: Same support email for claims submission failures

🚨 MIGRATION REQUIREMENT:
Change ALL email addresses from Ben's personal emails to info@perspect.ca for development environment:

poch_ben@hotmail.com → info@perspect.ca
ben@perspect.ca → info@perspect.ca
Update in both ConfigHelper.cs defaults AND web.config/appsettings.json

✅ Assumption #2: Medical Data Management - COMPLETELY UNDERSTOOD
Current Medical Data Architecture:

Version: 2.0.0.36 (versioned medical billing data)
StaticCodeList.cs: 10,826 lines of compiled medical data (ACTIVE)
Real Saskatchewan Medical Codes: Fee codes, ICD classifications, MSB error codes
Static Implementation: All data compiled into application code

🚨 CRITICAL OPERATIONAL IMPROVEMENT REQUIREMENT:
Medical Data Update Modernization:

Current Process: Manual file processing → C# code generation → Deploy entire application
Target Process: Admin panel file upload → Automatic database update → Live updates

ADMIN PANEL REQUIREMENT:

File Upload Interface: Upload RefDoc.txt, Fees.txt from MSB portal
Automatic Processing: Parse and update database tables automatically
Live Updates: No application deployment required
Validation: Verify data format and completeness before applying
Audit Trail: Track who uploaded what data when
Rollback Capability: Revert to previous data if needed

🔍 RESEARCH NEEDED:

Where are MBS-RefDoc.txt and MBS-Fees.txt actually used?
Current loading mechanism that we haven't discovered yet
Integration points with medical billing workflows

✅ Assumption #3: Job Scheduler - FULLY VERIFIED
JobScheduler Status: COMPLETELY DISABLED
What JobScheduler Was Supposed to Do:

Quartz.NET Scheduler: Enterprise job scheduling framework
Two Automated Jobs:

ReturnRetrivalJob: Download/process medical billing returns from MSB
SubmitPendingClaims: Submit pending medical claims to MSB


Cron Scheduling:

Daily processing (3-6 AM)
Pending claims every 10 minutes



Migration Questions:

Why was JobScheduler disabled?
Should automation be restored in .NET 8 migration?
Current workflow: How are claims submitted and returns processed now?


Active Components in Application_Start():

ViewEngine Setup: Razor view engine configuration
MVC Configuration: Areas, Web API, filters, bundles, routes
Security Headers: MVC response header disabled
Model Metadata: Cached data annotations provider

Commented Out Components (Legacy Medical Data Caching):

Medical Fee Tables: Fee.txt, WCBFee.txt caching
Medical Codes: ICD.txt, ExplainCode.txt, CareCode.txt
Reference Data: RefDoc.txt caching
Scheduling: RunSchedule.txt caching
Job Scheduler: Quartz.NET scheduling (commented out)

Other Methods:

Application_BeginRequest(): HTTPS redirect + cache headers
Application_Error(): Email notification to support team
Legacy Caching: File-based caching with refresh callbacks (disabled)

Business Context

Medical Billing System: PMB application startup modernization
Target: .NET 8 with modern Program.cs pattern
Priority: Maintain medical data security and session management


# STEP 2: .NET 8 Migration Planning
2.4.2a: Application Startup Logic Migration (3 SP)
Current Global.asax Components:

ViewEngines.Engines.Clear() / Add(RazorViewEngine) → Modern view system
AreaRegistration.RegisterAllAreas() → .NET 8 area routing
WebApiConfig.Register() → ASP.NET Core API configuration
FilterConfig.RegisterGlobalFilters() → Global filter registration
BundleConfig.RegisterBundles() → Asset bundling modernization
RouteConfig.RegisterRoutes() → Controller routing

## Migration Strategy:

Replace Global.asax Application_Start() → Program.cs service configuration
Convert MVC 5 patterns → ASP.NET Core MVC patterns
Modernize dependency injection → Built-in DI container
Update medical billing services → Scoped service registration



## 2.4.2b: Security & Session Migration (2 SP)
HTTPS Enforcement Migration:

Current: Application_BeginRequest with manual redirect
Target: app.UseHttpsRedirection() middleware

Cache Headers Migration:

Current: Manual Response.Cache settings in Application_BeginRequest
Target: Modern cache policy middleware

Session Configuration:

Medical billing requirement: 10-minute configurable timeout
Security: HTTPS-only, secure cookies for PHI protection

## 2.4.2c: Error Handling Migration (2 SP)
Email Notification Migration:

Current: Application_Error sends emails to Ben's personal email
Target: Global exception middleware with info@perspect.ca
Enhancement: Structured logging for PIPEDA compliance

Error Handling Modernization:

Global exception middleware replaces Application_Error
Secure error pages for PHI protection
PIPEDA audit logging integration
Migration Considerations

 Graceful shutdown → IHostApplicationLifetime
 Resource cleanup → Dependency injection disposal
 Final audit logging → Application stop events


# Implementation Strategy
Phase 1: Analyze Current Global.asax

Review existing Application_Start logic
Identify PMB-specific initialization
Document medical data handling requirements

Phase 2: Create Modern Program.cs

Configure services and DI
Set up middleware pipeline
Implement session and security settings

Phase 3: Migration Testing

Test application startup
Verify session timeout behavior
Validate error handling and logging


Medical Billing Specific Considerations
Security Requirements

PHI Protection: No medical data in logs or error messages
Session Security: Secure cookies, HTTPS-only
Audit Compliance: PIPEDA logging requirements

Performance Requirements

Fast startup: Medical office workflow
Session management: Doctor login experience
Error recovery: Minimal disruption to billing operations

### 2.4.3: PIPEDA Compliance Considerations

All security, session, and logging systems must conform to Canadian privacy standards (PIPEDA).

**Key Safeguards:**
- No PHI in logs or error messages
- HTTPS-only cookies and session enforcement
- Configurable audit logging from the admin panel

All modules referencing user data must reflect these standards.
# Next Steps

Review current Global.asax.cs - Identify specific PMB logic
Plan Program.cs structure - Modern .NET 8 patterns
Configure development environment - Test migration approach
Validate with medical workflows - Ensure no disruption to billing


Story Points Breakdown

2.4.2a: Application Startup (3 SP) - Complex DI and configuration
2.4.2b: Session & Error Handling (2 SP) - Medical security requirements
2.4.2c: Application Lifecycle (2 SP) - Graceful shutdown and cleanup

Total: 7 SP