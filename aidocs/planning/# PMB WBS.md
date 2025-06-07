# PMB Medical Billing System - Updated Migration Work Breakdown Structure v3
PMB Medical Billing System - Updated Migration Work Breakdown Structure v4
Strategic .NET 8 Migration with Requirements-Driven Task Prioritization
Updated: June 7, 2025 v4
Total Estimated Effort: 396-545 story points
Recommended Timeline: 22-26 weeks (solo developer, quality-focused)
Requirements Status: Phase 1 Complete ✅

## Key Updates in v4:
Requirements Integration Complete

Phase 1 requirements definition: Complete with comprehensive functional & non-functional analysis
Enhanced admin features: Hot-load data management, dashboard, audit logging (+16-25 SP)
Progressive detailed analysis: Skip formal Phase 1A, just-in-time planning approach
Database schema leverage: Admin features simplified using existing table structure

## Architectural Decisions Finalized

Zero business logic changes: All MSB validation rules preserved exactly
Admin enhancement timing: Phase 7-8 implementation (after stable .NET 8 foundation)
Future AI preparation: Architecture seams prepared without over-engineering
Security approach: Connection string encryption, defer database encryption

## Risk Mitigation Strategy

Progressive detailed analysis: Detailed planning before each major phase
Quality over timeline: No rushed implementation pressure
Deferred complexity: Database encryption, rule engine to Phase 2
Documentation discipline: Artifact-based tracking with thorough updates


Key Updates in v3:

## Formal Requirements Phase: High-level requirements documentation as Phase 1
Conditional Detailed Requirements: Phase 1A triggered based on scope changes
Risk-Based Task Prioritization: 24 high-risk files prioritized in early phases
Effort Estimation Integration: Story points based on file-by-file analysis
Flexible Timeline: Adjusts based on requirements complexity
Built-in Scope Management: Decision points for requirements evolution

# "PMB Medical Billing System - .NET 8 Migration Project
- 204+ files analyzed (Excel file-by-file review)
- 24 high-risk files identified requiring rewrites
- Microsoft .NET Upgrade Assistant: 38 story points baseline
- Total effort: 380-520 story points, 22-24 weeks
- Currently starting Phase 1: Requirements Definition
- Previous analysis: Dependencies, authentication, EF6→Core, security"

June 7, 2025 v2

Key Changes Made:
1. Hybrid Claims-Patient Architecture

Current: Claims remain the aggregate root during migration
Seams: Patient service interfaces and repository patterns prepared for Phase 2 transition
Risk Mitigation: Maintains current working logic while preparing for future refactoring

2. Simplified Multi-Payer Approach

Removed complex payer abstraction (since all SK payers use same format)
Kept existing WCB PDF generation approach
No unnecessary complexity added

3. Reference Data Management

Added specific focus on quarterly MSB updates (referring doctors, fee codes)
Automation interfaces to reduce manual pain points
Version control for reference data changes

4. Real-World AI Integration

On-premise/VPS-focused approach
Excel processor as side application
Real-time validation framework (building on hospital number validation pattern)

5. PIPEDA Compliance Focus

Removed "AI training data" references
Focused on access tracking and audit requirements
Canadian privacy law compliance

Next Steps:

Save this WBS as our strategic foundation
Code Review: When you're ready to share code, I can provide much more specific architectural guidance
Deep Dive: With real code, I can help identify the exact seams needed for the claims-to-patient transition

# June 7, 2025 v1

Major Additions
1. Architecture Seam Planning (Phase 1.3)

Maps current business logic to future domain boundaries
Identifies AI integration touchpoints early
Designs namespace structure for clean extraction

2. Strategic Project Structure (Phase 2.1.2)

Organizes projects by domain boundaries (PMB.Claims, PMB.BillingRules, etc.)
Sets foundation for future microservice extraction

3. CQRS Foundation (Phase 6)

Entire new phase for business logic refactoring
Command/Query separation with MediatR
Domain service extraction with AI-ready interfaces

4. API-First Design (Throughout)

API controllers alongside MVC controllers
Full API coverage for future SPA/mobile
Swagger/OpenAPI documentation generation

5. AI Integration Hooks

Document processing pipeline with metadata extraction
Audit logging designed for AI training data
Placeholder interfaces for future AI services
Domain events infrastructure

Strategic Balance Maintained
The enhanced WBS maintains your core principle: build architectural seams during migration without over-engineering. Each addition serves dual purposes:

Immediate migration needs (better organization, testability, maintainability)
Future AI integration points (clean interfaces, data collection, extensibility)

Phase 2 Preparation
The WBS now explicitly prepares for your AI vision:

Document Processing: Ready for Excel-to-claim AI extraction
Rule Engine: Prepared for AI-inferred billing rules
Trust Scoring: Foundation for ML-based physician risk assessment
API Layer: Complete coverage for AI service integration

# PMB Medical Billing System - Enhanced Migration Work Breakdown Structure
*Strategic .NET 8 Migration with AI-Ready Architecture Seams*

Phase 1: Requirements Definition & Risk Assessment ✅ COMPLETE
Priority: Formal requirements documentation and critical risk mitigation
Effort: 53-62 story points (ACTUAL)
Status: COMPLETE - June 7, 2025
1.1 High-Level Requirements Documentation ✅
Effort: 25 story points (ACTUAL)
1.1.1 Business Stakeholder Analysis ✅ (8 SP)

✅ Current system pain points and inefficiencies
✅ Future state vision and business objectives
✅ User role definitions and access requirements
✅ Integration requirements with external systems

1.1.2 Functional Requirements Definition ✅ (10 SP)

✅ Claims processing workflow requirements (preserve exactly)
✅ User management and security requirements (60min timeout, Member/Admin roles)
✅ External integration requirements (MSB API unchanged, OAuth modernization)
✅ Enhanced admin requirements (hot-load data, dashboard, configurability)

1.1.3 Non-Functional Requirements Specification ✅ (7 SP)

✅ Performance and scalability targets (<1sec response, ~100 concurrent users)
✅ Security and compliance requirements (access logging, connection encryption)
✅ Availability and disaster recovery requirements (99.999% uptime, cloud backups)
✅ Browser and device compatibility requirements

1.2 Requirements Impact Analysis ✅
Effort: 15-17 story points (ACTUAL)
1.2.1 Gap Analysis: Current vs. Future State ✅ (8 SP)

✅ Functional gaps identified (hot-load data management, admin dashboard)
✅ Technical architecture gaps assessed (service abstraction, audit logging)
✅ Database schema analysis complete (existing tables leverage admin features)

1.2.2 WBS Scope Adjustment Planning ✅ (5 SP)

✅ Enhanced admin features sequenced to Phase 7-8
✅ Total effort updated: 396-545 SP
✅ Timeline approach: Quality-focused, no pressure

1.2.3 Requirements Traceability Strategy ✅ (4 SP)

✅ Artifact-based tracking approach confirmed
✅ Documentation discipline established
✅ Progressive detailed analysis approach finalized

1.3 Detailed Requirements Cascade Decision ✅
Effort: 13-20 story points (ACTUAL)
1.3.1 Phase 1A Assessment ✅ (8 SP)

✅ DECISION: Skip formal Phase 1A detailed requirements
✅ APPROACH: Progressive detailed analysis before each WBS phase
✅ RATIONALE: Just-in-time planning without documentation overhead

1.3.2 Progressive Planning Strategy ✅ (12 SP)

✅ Phase-by-phase detailed analysis approach
✅ Architecture seam preparation without over-engineering
✅ Critical integration point identification



Phase 2: Foundation & Critical Risk Mitigation
Priority: Address highest-risk components first
Effort: 80-100 story points
Timeline: Weeks 4-6
Approach: Detailed analysis before phase start
2.1 Environment Setup & High-Risk Assessment (12 SP)
2.1.1 Set up .NET 8 development environment (2 SP)

Development environment configuration
Package management setup
Testing framework installation

2.1.2 Create migration branch and backup systems (3 SP)

Git branch strategy implementation
ENHANCED: Cloud backup system implementation
ENHANCED: Backup restore testing procedures

2.1.3 🔴 CRITICAL: Security audit and credential removal (5 SP)

Remove hardcoded passwords from connection strings ("i@mF1nes", "Pass@word1")
Implement Azure Key Vault/User Secrets
Update all connection strings to use secure storage

2.1.4 Document 24 high-risk files and modernization sequence (2 SP)

ENHANCED: Progressive detailed analysis for Phase 2 specific files
Risk mitigation strategy per file
Implementation sequence optimization

2.2 Authentication System Overhaul (🔴 HIGH RISK) (25-30 SP)
Files: AccountController.cs, AccountModels.cs, AccountRepository.cs
2.2.1 AccountController.cs → ASP.NET Core Identity migration (8 SP)

Replace FormsAuthentication with Identity SignInManager
ENHANCED: Implement modern OAuth with .NET 8 improvements
Add MFA foundation and rate limiting
PRESERVE: 60-minute session timeout

2.2.2 AccountModels.cs → Identity ViewModels (6 SP)

Migrate to Identity-based ViewModels
Add MFA support structures
Implement expiration logic
PRESERVE: All current validation rules

2.2.3 AccountRepository.cs → Async Identity operations (8 SP)

Convert to async/await patterns
ENHANCED: Integrate activity logging for PIPEDA compliance
Implement proper transaction handling

2.2.4 Migrate AspNet_* tables to Identity schema (5 SP)

Database schema migration planning
Data preservation strategy
PRESERVE: All existing user roles (Member/Administrator)

2.3 Entity Framework Core Migration (🔴 HIGH RISK) (20-25 SP)
Files: MBS_Data_Model.edmx
2.3.1 EDMX → EF Core Code-First conversion (15 SP)

Scaffold existing database to EF Core entities
Convert EDMX mappings to Fluent API configuration
CRITICAL: Validate all 17 entity relationships
DATABASE SCHEMA CONFIRMED: Leverage existing table structure

2.3.2 Repository pattern modernization (10 SP)

Update 7 existing repositories for EF Core
Implement proper async patterns
Add transaction scope support
PREPARE: Service layer extraction seams

2.4 Configuration Security Overhaul (🔴 HIGH RISK) (10-15 SP)
Files: web.config, Global.asax
2.4.1 web.config → appsettings.json migration (8 SP)

Convert all 21 configuration files
Implement environment-specific configs
CRITICAL: Secure all connection strings (remove hardcoded credentials)

2.4.2 Global.asax → Startup.cs conversion (7 SP)

Convert to ASP.NET Core middleware pipeline
Migrate application events
PREPARE: Admin configurability infrastructure


Phase 3: Core Claims Processing Migration
Priority: Business-critical claim processing components
Effort: 70-90 story points
Timeline: Weeks 7-10
3.1 Claims Controllers Modernization (🔴 HIGH RISK) (25-30 SP)
Files: ClaimsInController.cs, ServiceRecordController.cs
3.1.1 ClaimsInController.cs → Service layer extraction (12 SP)

Extract business logic to ClaimsProcessingService
PRESERVE: All existing business rules exactly
PREPARE: Architecture seams for future AI rule integration
Add comprehensive unit test coverage

3.1.2 ServiceRecordController.cs → ViewModel separation (8 SP)

Introduce proper ViewModels and DTOs
PRESERVE: All validation rules from ServiceRecordMetaData.cs
PRESERVE: Province-specific hospital number validation (Modulus 11, etc.)
Implement real-time validation hooks

3.1.3 Create corresponding API endpoints (5 SP)

PREPARE: Future mobile/SPA integration
Maintain identical functionality
PREPARE: "Imported" status workflow seams

3.2 Claims Models & Data Access (🔴 HIGH RISK) (20-25 SP)
Files: ServiceRecordModels.cs, ServiceRecordRepository.cs
3.2.1 ServiceRecordModels.cs → ViewModel/DTO separation (10 SP)

Modularize into separate ViewModel and DTO classes
PRESERVE: All current validation attributes exactly
PRESERVE: All business rule validation methods
Support real-time validation (hospital number, time format, etc.)

3.2.2 ServiceRecordRepository.cs → Transaction scope (12 SP)

Implement atomic save operations for full claims
Add async/await patterns
PRESERVE: All current query patterns
Implement proper error handling

3.3 External Service Integration Updates (25-30 SP)
Files: ClaimSubmitter.cs, ReturnParser.cs, OAuthMessageHandler.cs
3.3.1 ClaimSubmitter.cs → Modern HTTP client (10 SP)

Refactor to async with HttpClient
ENHANCED: Add retry policies and circuit breaker for future AI integration
Implement structured logging
PRESERVE: All current submission logic

3.3.2 ReturnParser.cs → Schema validation (8 SP)

PRESERVE: Current return file processing exactly
Add comprehensive unit tests
Validate against defined schemas
PREPARE: Future document processing pipeline seams

3.3.3 OAuthMessageHandler.cs → Middleware pattern (7 SP)

Convert to ASP.NET Core middleware
ENHANCED: Modern OAuth flows with .NET 8 improvements
Add diagnostics and monitoring hooks


Phase 4: User Management & Security
Priority: Administrative and security functions
Effort: 40-50 story points
Timeline: Weeks 11-12
4.1 User Management Modernization (🔴 HIGH RISK) (25-30 SP)
Files: UserManagementController.cs, UserManagementModels.cs
4.1.1 UserManagementController.cs → Audit trail implementation (12 SP)

ENHANCED: Log all impersonation activities (PIPEDA compliance)
Support multi-role audit trails
NEW: Populate existing AuditLog table comprehensively
PRESERVE: Current impersonation workflow

4.1.2 UserManagementModels.cs → Audit metadata (10 SP)

Add audit metadata to all models
Decouple role editing from impersonation logic
ENHANCED: PIPEDA-compliant tracking (configurable)

4.2 Security Infrastructure (15-20 SP)
Files: SecurityHelper.cs
4.2.1 SecurityHelper.cs → Modern encryption (15 SP)

ENHANCED: Implement .NET 8 encryption for connection strings
Centralize key management
Replace any insecure algorithms
DEFERRED: Database field encryption (search performance impact)


Phase 5: Frontend Modernization
Priority: User interface and client-side functionality
Effort: 50-70 story points
Timeline: Weeks 13-14
5.1 JavaScript Framework Updates (🔴 HIGH RISK) (35-45 SP)
Files: jquery-2.1.1.js, SearchClaimBetaIndex.js, ServiceRecordAction.js
5.1.1 jQuery 2.1.1 → 3.6+ upgrade (15 SP)

Replace with CDN or npm-managed jQuery 3.6+
PRESERVE: All existing functionality exactly
PRESERVE: All auto-completion features (patients, codes, doctors)
Test all validation patterns

5.1.2 SearchClaimBetaIndex.js → Component framework (12 SP)

Modularize JS components
PRESERVE: All search functionality including partial matching
Move filters to reusable components
Evaluate Vue/React migration path for Phase 2

5.1.3 ServiceRecordAction.js → Form framework (10 SP)

Componentize functionality
PRESERVE: All real-time validation (hospital numbers, time format)
PRESERVE: All dynamic UI behavior
Add comprehensive unit tests

5.2 UI/UX Enhancements (15-25 SP)
5.2.1 Bootstrap 3.1.1 → Bootstrap 5+ migration (15 SP)

PRESERVE: All current UX patterns exactly
Responsive design improvements
PREPARE: Admin dashboard UI foundation

5.2.2 Responsive design improvements (8 SP)

Mobile compatibility
PREPARE: Future mobile app integration


Phase 6: Background Services & Console Apps
Priority: Background processing applications
Effort: 60-80 story points
Timeline: Weeks 15-16
6.1 Console Applications → Hosted Services (40-50 SP)
Files: All console application Program.cs files
6.1.1 MBS.RetrieveReturn → Background service (12 SP)

Convert to .NET 8 hosted service
Add retry support and structured logging
ENHANCED: Admin configurable scheduling
Implement configuration validation

6.1.2 MBS.ReconcileClaims → Background service (10 SP)

PRESERVE: Current reconciliation logic exactly
ENHANCED: Admin configurable intervals
Add monitoring hooks

6.1.3 MBS.SubmittedPendingClaims → Background service (8 SP)

ENHANCED: Admin dashboard integration
PRESERVE: Current processing logic

6.1.4 MBS.PasswordChanger → Admin utility service (5 SP)

ENHANCED: Admin dashboard integration
Security improvements

6.1.5 MBS.TestCodeUsed → Maintenance service (5 SP)

ENHANCED: Admin monitoring integration

6.2 Scheduling & Job Management (15-25 SP)
6.2.1 Quartz.NET 2.6.2 → 3.x+ migration (15 SP)

ENHANCED: Admin configurable job scheduling
Modern scheduling patterns
PREPARE: Hot-load data management scheduling

6.2.2 Modern job scheduling implementation (10 SP)

NEW: Admin dashboard job control
Performance monitoring
Error handling and retry logic


Phase 7: Enhanced Admin Features & API Layer
Priority: Admin enhancements and external integrations
Effort: 55-85 story points
Timeline: Weeks 17-18
7.1 Enhanced Admin Features (🆕 NEW) (16-25 SP)
7.1.1 Hot-Load Data Management (5-8 SP)

NEW: Convert MBS-Fees.txt → FeeCodes database table
NEW: Convert MBS-RefDoc.txt → ReferringDoctors database table
NEW: Admin upload interface with validation
NEW: Runtime cache invalidation (zero downtime updates)
NEW: Version control and rollback capability

7.1.2 Admin Dashboard Implementation (8-12 SP)

NEW: Claims processing metrics (leverage ClaimsIn, PaidClaim, RejectedClaim tables)
NEW: User activity monitoring (enhance AuditLog population)
NEW: System health dashboard (background jobs, API response times)
NEW: Performance metrics (response times, peak usage)
NEW: Real-time logging visibility (replace VPS remote access)

7.1.3 Enhanced Audit Logging (3-5 SP)

NEW: Comprehensive user access tracking
NEW: PIPEDA-compliant logging (configurable on/off)
NEW: Dashboard integration for real-time monitoring

7.2 External Service Modernization (25-35 SP)
7.2.1 Interfax SOAP → REST API conversion (8 SP)

Replace WCF service reference with HttpClient
ENHANCED: Vendor abstraction layer (IFaxService interface)
Implement modern retry patterns
Add comprehensive error handling

7.2.2 Saskatchewan Health API updates (10 SP)

PRESERVE: All current API integration exactly
ENHANCED: Monitoring and logging integration
PREPARE: Future AI integration hooks

7.2.3 Claims processing API modernization (15 SP)

ENHANCED: Modern retry patterns with circuit breaker
PREPARE: Future AI rule validation pipeline
Structured logging integration

7.3 API Endpoint Creation (15-25 SP)
7.3.1 Create REST API controllers for all MVC functionality (20 SP)

PREPARE: Future mobile/SPA integration
PREPARE: "Imported" claims workflow API endpoints
Full API coverage maintaining identical functionality

7.3.2 Implement OpenAPI/Swagger documentation (5 SP)

Comprehensive API documentation
PREPARE: Future integrations


Phase 8: Testing & Quality Assurance
Priority: Comprehensive validation
Effort: 30-40 story points
Timeline: Weeks 19-20
8.1 Automated Testing Implementation (25-30 SP)
8.1.1 Unit test coverage for high-risk components (15 SP)

CRITICAL: All validation rules testing (province-specific, business rules)
CRITICAL: Claims processing workflow testing
Authentication and security testing

8.1.2 Integration testing for critical workflows (10 SP)

End-to-end claims processing
NEW: Admin features integration testing
External API integration testing

8.1.3 API endpoint testing (8 SP)

NEW: Admin dashboard API testing
NEW: Hot-load data management testing
Full API coverage testing

8.2 System Validation (10-15 SP)
8.2.1 Performance testing and optimization (10 SP)

VALIDATE: <1 second response time target
VALIDATE: ~100 concurrent user capacity
NEW: Admin dashboard performance testing

8.2.2 Security penetration testing (5 SP)

VALIDATE: Connection string encryption
VALIDATE: Access logging functionality
VALIDATE: Authentication security


Phase 9: Deployment & Production Cutover (UPDATED)
Priority: Production readiness and controlled transition
Effort: 25-35 story points
Timeline: Weeks 21-23
9.1 New VPS Setup & Staging Deployment (15-20 SP)
9.1.1 New VPS procurement and environment setup (5 SP)

New VPS specification and setup
.NET 8 runtime environment installation
SQL Server setup and configuration
Security hardening and firewall configuration

9.1.2 Staging deployment to new VPS (8 SP)

Deploy complete .NET 8 application to new VPS
Database deployment with production data copy
Configuration setup (connection strings, app settings)
Admin dashboard and enhanced features testing
Cloud backup system implementation and testing

9.1.3 Comprehensive staging validation (7 SP)

Full UAT execution on new VPS with real data
Performance testing under production-like conditions
Integration testing with external systems (MSB API, fax service)
Admin features validation (hot-load data, dashboard, scheduling)
Backup and restore procedures testing

9.2 Production Cutover Planning & Execution (10-15 SP)
9.2.1 Cutover procedure development (5 SP)

Maintenance window planning and user communication
DNS/network cutover procedures
Database final sync procedures
Rollback plan if issues arise during cutover
Go/No-go decision criteria checklist

9.2.2 Production cutover execution (8 SP)

Maintenance window execution
Final database sync from old to new VPS
Network/DNS switch from old VPS to new VPS
Live system validation and smoke testing
User notification and system availability confirmation

9.2.3 Post-cutover monitoring and old VPS retirement (2 SP)

48-72 hour monitoring of new production system
Performance validation under real production load
Old VPS preservation for emergency rollback (30-day retention)
Final old VPS decommission after validation period

Three-stage deployment: Local Development → New VPS Testing → Production Cutover

Updated Requirements Integration & Success Criteria
Phase 1 Requirements Integration ✅ COMPLETE

Business Logic: All MSB validation rules preserved exactly
User Experience: All current UX patterns maintained
Performance: <1 second response times maintained
Security: Enhanced with modern .NET 8 features
Integration: All external services preserved, OAuth modernized

Enhanced Success Criteria
✅ All 24 high-risk files successfully modernized
✅ Zero security vulnerabilities in production
✅ Performance equal or better than current system
✅ 100% functional parity maintained
✅ Comprehensive test coverage for business logic
🆕 Admin configurability implemented (hot-load data, dashboard, scheduling)
🆕 Cloud backup and restore procedures validated
🆕 Architecture seams prepared for Phase 2 AI integration
Requirements Validation Checkpoints

Week 4: Authentication requirements and security implementation
Week 7: Claims processing business rules validation
Week 11: User management and audit logging verification
Week 15: Background processing and admin configurability
Week 17: Admin features and external integrations
Week 20: Final requirements validation before production

Deferred to Phase 2 (Future AI Integration)

Database field encryption - With search architecture redesign
Admin review capabilities - For document processing workflow
Rule engine implementation - For 300+ fee code AI rules
Excel document processing - AI-driven claims extraction
Encrypted search optimization - Maintain partial matching performance


Updated Total Effort Summary
Core Migration: 380-520 SP (Original)
Enhanced Admin Features: +16-25 SP
TOTAL ESTIMATED EFFORT: 396-545 SP
Timeline Options:

Conservative: 26 weeks (solo developer, quality-focused)
Aggressive: 22 weeks (if admin features are simplified)
Recommended: 24 weeks (balanced approach)

Risk Mitigation Strategy:

Progressive detailed analysis before each phase
Quality over timeline - no rushed implementation
Comprehensive testing throughout migration
Artifact-based documentation with thorough updates
Architecture seam preparation without over-engineering

WBS v4 Status: Updated with Phase 1 Requirements Integration Complete ✅