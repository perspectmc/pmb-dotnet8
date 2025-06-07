# PMB Medical Billing System - Enhanced Migration Work Breakdown Structure
*Strategic .NET 8 Migration with AI-Ready Architecture Seams*

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

June 7, 2025 v1

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

## Phase 1: Foundation & Migration Preparation (Weeks 1-2)

### 1.1 Environment Setup & Baseline
- [ ] **1.1.1** Document current system dependencies and third-party packages
- [ ] **1.1.2** Inventory all NuGet packages and their .NET 8 compatibility
- [ ] **1.1.3** Set up .NET 8 development environment alongside existing
- [ ] **1.1.4** Create migration branch in GitHub (`feature/net8-migration`)
- [ ] **1.1.5** Document current database schema and connection strings
- [ ] **1.1.6** Backup production database for testing scenarios

### 1.2 Pre-Migration Analysis
- [ ] **1.2.1** Run .NET Upgrade Assistant analysis (dry run)
- [ ] **1.2.2** Identify breaking changes and incompatible packages
- [ ] **1.2.3** Document web.config → appsettings.json mapping requirements
- [ ] **1.2.4** Catalog all custom HTTP modules/handlers for conversion
- [ ] **1.2.5** Review Global.asax.cs for startup configuration needs

### 1.3 **NEW: Architecture Seam Planning**
- [ ] **1.3.1** Map current business logic to future domain boundaries
  - [ ] Claims Processing domain (current focus)
  - [ ] Patient Management domain (future patient-centric)
  - [ ] Billing Rules Engine domain
  - [ ] Physician Registry domain
  - [ ] Reference Data Management (quarterly updates)
- [ ] **1.3.2** Identify AI integration touchpoints
  - [ ] Excel document parsing entry points (physician worksheets)
  - [ ] PDF policy manual processing hooks
  - [ ] Real-time validation extension points
- [ ] **1.3.3** Design namespace structure for future extraction
  - [ ] `PMB.Claims.*` - Claims processing domain (current aggregate root)
  - [ ] `PMB.Patients.*` - Patient management (future aggregate root)
  - [ ] `PMB.BillingRules.*` - Rule engine domain
  - [ ] `PMB.Physicians.*` - Physician registry domain
  - [ ] `PMB.ReferenceData.*` - MSB reference data management
  - [ ] `PMB.Documents.*` - Document processing pipeline (side application)

## Phase 2: Core Platform Migration (Weeks 3-6)

### 2.1 Project Structure Conversion
- [ ] **2.1.1** Convert main web project to ASP.NET Core
  - [ ] Update .csproj files to SDK-style format
  - [ ] Convert web.config to appsettings.json
  - [ ] Create Program.cs and Startup.cs
- [ ] **2.1.2** Convert class library projects with strategic namespace organization
  - [ ] `PMB.Core` - Shared kernel and common abstractions
  - [ ] `PMB.Claims` - Claims processing domain (current aggregate root)
  - [ ] `PMB.Patients` - Patient management (future aggregate root - seams only)
  - [ ] `PMB.BillingRules` - Business rules engine
  - [ ] `PMB.Physicians` - Physician management
  - [ ] `PMB.ReferenceData` - MSB reference data management
  - [ ] `PMB.Infrastructure` - Data access and external services
- [ ] **2.1.3** Update package references to .NET 8 compatible versions
- [ ] **2.1.4** Resolve namespace conflicts (System.Web → Microsoft.AspNetCore)

### 2.2 Configuration & Dependency Injection
- [ ] **2.2.1** Migrate configuration system
  - [ ] Connection strings
  - [ ] App settings with environment-specific overrides
  - [ ] Custom configuration sections for AI service endpoints (placeholder)
- [ ] **2.2.2** Implement built-in DI container with strategic service registration
  - [ ] Register domain services with interfaces
  - [ ] Configure Entity Framework context
  - [ ] Set up logging providers with structured logging
  - [ ] **NEW:** Register MediatR for CQRS pattern foundation
  - [ ] **NEW:** Register placeholder interfaces for future AI services

### 2.3 MVC & Routing Updates
- [ ] **2.3.1** Update controller base classes and attributes
- [ ] **2.3.2** Convert ActionResults to new format
- [ ] **2.3.3** Update routing configuration
- [ ] **2.3.4** Migrate custom filters and attributes
- [ ] **2.3.5** Update model binding and validation
- [ ] **2.3.6** **NEW:** Create API controllers alongside MVC controllers
  - [ ] Claims API endpoints
  - [ ] Billing Rules API endpoints
  - [ ] Physician Registry API endpoints

## Phase 3: Data Access Modernization (Weeks 5-7)

### 3.1 Entity Framework Migration
- [ ] **3.1.1** Upgrade to Entity Framework Core
- [ ] **3.1.2** Convert DbContext configuration with domain separation
  - [ ] `ClaimsContext` for claims-related entities (current focus)
  - [ ] `PatientsContext` for patient data (patient-centric seams)
  - [ ] `PhysiciansContext` for physician data
  - [ ] `ReferenceDataContext` for MSB reference data
- [ ] **3.1.3** Update entity configurations and relationships
- [ ] **3.1.4** Test all existing CRUD operations
- [ ] **3.1.5** Implement async/await patterns in data access
- [ ] **3.1.6** **NEW:** Implement Repository pattern with hybrid aggregate design
  - [ ] Current: Claims as aggregate root with patient data access
  - [ ] Future-ready: Patient repository interfaces for Phase 2 transition
- [ ] **3.1.7** **NEW:** Add domain events infrastructure for audit trails

### 3.2 Database Compatibility & Strategic Enhancements
- [ ] **3.2.1** Verify SQL Server compatibility
- [ ] **3.2.2** Test connection pooling and performance
- [ ] **3.2.3** Update stored procedure calls if any
- [ ] **3.2.4** Implement proper transaction handling
- [ ] **3.2.5** **NEW:** Add audit log tables for PIPEDA compliance
- [ ] **3.2.6** **NEW:** Design reference data versioning for quarterly MSB updates
- [ ] **3.2.7** **NEW:** Create patient-centric query foundations (seams for Phase 2)

## Phase 4: UI & Frontend Updates (Weeks 6-8)

### 4.1 Razor Views Migration
- [ ] **4.1.1** Update Razor syntax for .NET 8
- [ ] **4.1.2** Convert HTML helpers to Tag helpers
- [ ] **4.1.3** Update _ViewStart.cs and _Layout.cshtml
- [ ] **4.1.4** Migrate bundling and minification
- [ ] **4.1.5** Update jQuery and JavaScript references

### 4.2 Static Files & Assets
- [ ] **4.2.1** Configure static file serving
- [ ] **4.2.2** Update CSS and JavaScript build process
- [ ] **4.2.3** Implement proper cache headers
- [ ] **4.2.4** Test file upload functionality
- [ ] **4.2.5** **NEW:** Implement document upload pipeline for future Excel processing
- [ ] **4.2.6** **NEW:** Create reference data import interface for MSB quarterly updates

### 4.3 **NEW: Frontend Architecture Seams**
- [ ] **4.3.1** Create JavaScript modules aligned with domain boundaries
- [ ] **4.3.2** Implement HTMX for progressive enhancement (SPA foundation)
- [ ] **4.3.3** Add real-time validation components (hospital number validation pattern)
- [ ] **4.3.4** Create reference data management interface for quarterly updates

## Phase 5: Security & Authentication (Weeks 7-9)

### 5.1 Authentication Migration
- [ ] **5.1.1** Convert membership provider to ASP.NET Core Identity
- [ ] **5.1.2** Migrate existing user accounts and roles
- [ ] **5.1.3** Update login/logout flows
- [ ] **5.1.4** Implement JWT token support for APIs
- [ ] **5.1.5** **NEW:** Maintain current role structure during migration (minimal changes)

### 5.2 Authorization & Security
- [ ] **5.2.1** Convert authorization attributes and policies
- [ ] **5.2.2** Implement HTTPS redirection
- [ ] **5.2.3** Add security headers middleware
- [ ] **5.2.4** Update CORS policies for API access
- [ ] **5.2.5** Implement comprehensive audit logging for sensitive operations
- [ ] **5.2.6** **NEW:** Add PIPEDA-compliant audit logging (access tracking)

## Phase 6: Business Logic Refactoring (Weeks 7-9)

### 6.1 **NEW: Domain Service Extraction (Hybrid Claims-Patient Design)**
- [ ] **6.1.1** Extract Claims Processing Service (current aggregate root)
  - [ ] Create `IClaimsProcessingService` interface
  - [ ] Implement with current business logic
  - [ ] Add patient data access seams for future refactoring
- [ ] **6.1.2** Create Patient Service Interfaces (future aggregate root)
  - [ ] Create `IPatientService` interface (placeholder implementation)
  - [ ] Design patient-centric query patterns
  - [ ] Prepare for Phase 2 aggregate root transition
- [ ] **6.1.3** Extract Reference Data Management Service
  - [ ] Create `IReferenceDataService` interface
  - [ ] Implement MSB quarterly update automation
  - [ ] Version control for referring doctors and fee codes

### 6.2 **NEW: Command/Query Separation (CQRS) - Claims-Focused**
- [ ] **6.2.1** Implement MediatR request/response pattern
- [ ] **6.2.2** Create Commands for state-changing operations
  - [ ] ProcessClaimCommand (current focus)
  - [ ] UpdateReferenceDataCommand
  - [ ] ValidateHospitalNumberCommand (real-time validation)
- [ ] **6.2.3** Create Queries for read operations
  - [ ] GetClaimDetailsQuery
  - [ ] GetPatientClaimsQuery (patient-centric seam)
  - [ ] SearchReferenceDataQuery
- [ ] **6.2.4** Add pipeline behaviors for cross-cutting concerns
  - [ ] Logging behavior
  - [ ] Validation behavior (real-time validation foundation)
  - [ ] **Future:** Document processing behavior (Excel integration)

## Phase 7: Testing & Validation (Weeks 8-10)

### 7.1 Automated Testing
- [ ] **7.1.1** Set up unit testing framework (xUnit)
- [ ] **7.1.2** Create integration tests for key workflows
- [ ] **7.1.3** Implement repository pattern tests
- [ ] **7.1.4** Add controller action tests (both MVC and API)
- [ ] **7.1.5** Test data access layer thoroughly
- [ ] **7.1.6** **NEW:** Test domain service interfaces and contracts
- [ ] **7.1.7** **NEW:** Create integration tests for CQRS handlers

### 7.2 System Validation
- [ ] **7.2.1** Functional testing of all major features
- [ ] **7.2.2** Performance comparison testing
- [ ] **7.2.3** Security penetration testing
- [ ] **7.2.4** Load testing with production-like data
- [ ] **7.2.5** User acceptance testing scenarios
- [ ] **7.2.6** **NEW:** Validate API endpoints for future SPA/mobile consumption
- [ ] **7.2.7** **NEW:** Test audit logging and event sourcing foundations

## Phase 8: Deployment & DevOps (Weeks 9-11)

### 8.1 CI/CD Pipeline
- [ ] **8.1.1** Set up GitHub Actions workflow
- [ ] **8.1.2** Implement automated build process
- [ ] **8.1.3** Configure automated testing in pipeline
- [ ] **8.1.4** Set up staging environment deployment
- [ ] **8.1.5** Implement database migration scripts
- [ ] **8.1.6** **NEW:** Add API documentation generation (Swagger/OpenAPI)
- [ ] **8.1.7** **NEW:** Configure environment-specific AI service placeholders

### 8.2 Production Deployment
- [ ] **8.2.1** Prepare production server for .NET 8
- [ ] **8.2.2** Plan zero-downtime deployment strategy
- [ ] **8.2.3** Create rollback procedures
- [ ] **8.2.4** Configure monitoring and logging with structured logs
- [ ] **8.2.5** Set up automated backups
- [ ] **8.2.6** **NEW:** Deploy API endpoints for future consumption

## Phase 9: Post-Migration Optimization & AI Foundation (Weeks 11-12)

### 9.1 Performance Optimization
- [ ] **9.1.1** Profile application performance
- [ ] **9.1.2** Optimize database queries
- [ ] **9.1.3** Implement caching strategies
- [ ] **9.1.4** Optimize memory usage
- [ ] **9.1.5** Fine-tune garbage collection

### 9.2 **NEW: Phase 2 Foundation Validation**
- [ ] **9.2.1** Validate claims-to-patient aggregate transition readiness
- [ ] **9.2.2** Test patient-centric query patterns with current data
- [ ] **9.2.3** Validate document processing pipeline hooks for Excel integration
- [ ] **9.2.4** Test reference data versioning and update automation
- [ ] **9.2.5** Validate real-time validation framework extensibility

### 9.3 **NEW: Phase 2 Preparation**
- [ ] **9.3.1** Document patient-centric transition plan and architectural seams
- [ ] **9.3.2** Create Phase 2 backlog prioritizing:
  - [ ] Patient aggregate root refactoring
  - [ ] Excel processing side application
  - [ ] EMR features (scheduling, patient-centric views)
- [ ] **9.3.3** Establish baseline metrics for patient-centric performance impact
- [ ] **9.3.4** Document reference data automation for business process improvement
- [ ] **9.3.5** Prepare stakeholder demo showcasing hybrid architecture foundations

## Risk Mitigation & Contingencies

### Critical Risks
1. **Third-party package incompatibilities** - Research alternatives before starting
2. **Database migration issues** - Maintain parallel testing environment
3. **Authentication data loss** - Create comprehensive backup/restore procedures
4. **Performance degradation** - Establish baseline metrics early
5. **Business continuity** - Plan for rollback at any phase
6. **NEW: Over-engineering architectural seams** - Maintain balance between future-readiness and current functionality
7. **NEW: Claims-to-Patient transition complexity** - Validate patient-centric seams don't break current claims processing

### Success Criteria
- [ ] All existing functionality works in .NET 8
- [ ] Performance is equal or better than current system
- [ ] Security is enhanced with modern practices
- [ ] Deployment process is automated and reliable
- [ ] **NEW:** Claims-centric functionality maintained with patient-centric seams
- [ ] **NEW:** API endpoints provide feature parity with MVC controllers
- [ ] **NEW:** CQRS foundation supports real-time validation and future Excel processing
- [ ] **NEW:** Reference data management automation reduces quarterly manual updates
- [ ] **NEW:** Audit logging supports PIPEDA compliance and future analytics

## Strategic Architecture Outcomes

### Immediate (.NET 8 Migration)
- Modern, maintainable codebase on supported platform
- API-first design supporting future SPA/mobile development
- Enhanced security and deployment automation
- Clear domain boundaries with extraction-ready interfaces

### Enabled for Phase 2 (Patient-Centric + AI Integration)
- **Patient-Centric Refactoring**: Hybrid architecture ready for aggregate root transition
- **Excel Processing**: Side application foundation for physician worksheet automation
- **EMR Features**: Patient-centric seams ready for scheduling and patient views
- **Reference Data Automation**: Quarterly update pain point solved
- **Real-time Validation**: Framework ready for complex business rule validation
- **API Layer**: Full API coverage for future SPA/mobile development

## Estimated Timeline: 12 weeks
- **Weeks 1-2**: Preparation and architectural planning
- **Weeks 3-6**: Core migration with strategic refactoring
- **Weeks 7-9**: Domain extraction and security hardening
- **Weeks 10-12**: Deployment automation and AI foundation validation

## Key Deliverables
1. Fully functional .NET 8 application with enhanced architecture
2. API-first design with comprehensive endpoint coverage
3. Domain-driven service boundaries ready for microservice extraction
4. CQRS foundation with MediatR pipeline
5. Automated CI/CD pipeline with API documentation
6. Security-hardened deployment with structured logging
7. AI-ready architecture with validated integration points
8. Comprehensive technical documentation and Phase 2 roadmap