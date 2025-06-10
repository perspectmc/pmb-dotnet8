# PMB CODE REVIEW MASTER TRACKER
*Updated: December 2024*

## PROJECT OVERVIEW
- **Project:** Perspect Medical Billing System (.NET Framework → .NET 8 migration)
- **Stakes:** Medical practices depend on this system - zero tolerance for breaking changes
- **Cost of errors:** Hours of debugging, thousands in development time, potential system downtime
- **Documentation Standard:** Comprehensive header comments with business context + migration actions
- **Strategy:** Bottom-up analysis (dependencies first, then complex files)

## PMB AI-HUMAN WORKING AGREEMENT

### System Context
- **Project**: Perspect Medical Billing System (.NET Framework → .NET 8 migration)
- **Stakes**: Medical practices depend on this system - zero tolerance for breaking changes
- **Cost of errors**: Hours of debugging, thousands in development time, potential system downtime

### Analysis Rules

#### Code Analysis
- **State limitations**: "Based on X lines of Y total" or "Partial file analysis"
- **Use qualifiers**: "appears unused" NOT "is unused"
- **Confidence levels**: HIGH/MEDIUM/LOW for all recommendations
- **Business context required**: Technical analysis without domain knowledge is incomplete

#### Dependencies & Using Statements
- **Never recommend removal** without full file verification
- **Search for actual usage patterns** before conclusions
- **Business operations matter**: If system does filtering/sorting/analysis, LINQ is likely used
- **Test first**: IDE verification mandatory before any changes

### Communication Standards
- **Lead with uncertainty** when appropriate
- **Document what cannot be verified**
- **Ask for business context** before technical recommendations
- **Conservative by default** - when in doubt, don't change

### Collaboration Protocol

#### My Role (AI)
- Technical analysis with stated limitations
- Pattern recognition and code structure insights
- Research and documentation support
- Preliminary findings with verification requirements

#### Your Role (Human)
- Business domain expertise
- Final verification authority
- Real-world testing capability
- Go/no-go decisions on implementations

#### Joint Process
1. **Analysis**: I provide findings with confidence levels
2. **Context**: You provide business requirements/constraints
3. **Verification**: You test in actual environment
4. **Implementation**: Only after verification passes

### Model Selection Strategy

#### Claude Sonnet 4 (General/Safe)
- **Code explanations** - "What does this do?"
- **Architecture discussions** - "How should we approach X?"
- **Documentation review** - WBS analysis, gap identification
- **Strategy conversations** - migration planning, timeline discussions
- **Research & analysis** - understanding patterns, business rules

#### Claude Opus 4 (Change Decisions)
- **Dependency modifications** - "Should we remove this using statement?"
- **Business logic changes** - Any code that affects medical billing
- **Database schema decisions** - Migration approaches, data preservation
- **Framework migration steps** - EF6 → EF Core specifics
- **Critical architecture choices** - Authentication system overhaul

### Quick Decision Framework
- **Documentation changes**: Sonnet 4 - generally safe to proceed
- **New files/features**: Sonnet 4 - safe with review
- **Dependency changes**: Opus 4 - mandatory verification required
- **Business logic changes**: Opus 4 - extreme caution + testing
- **Database schema changes**: Opus 4 - maximum verification required

### Remember
Every recommendation affects real medical practices. When uncertain, choose safety over efficiency. Use Opus 4 when code changes are involved - the cost difference is negligible compared to debugging broken medical billing systems.

## DOCUMENTATION TEMPLATE

### File Documentation Standard
All code files should include comprehensive header comments using this format:

```csharp
/*
=============================================================================
DOCUMENTATION CREATED USING PMB AI-HUMAN WORKING AGREEMENT
Project: Perspect Medical Billing System (.NET Framework → .NET 8 migration)
Stakes: Medical practices depend on this system - zero tolerance for breaking changes
=============================================================================

FILE: [FileName] ([File Category - e.g., Foundation File, Controller, Service])
BUSINESS PURPOSE: 
[Plain English explanation of what this file does for the business]

CORE BUSINESS WORKFLOW:
[Step-by-step business process this file supports]

KEY COMPONENTS:
[Bullet list of main classes/methods/features]

UPSTREAM DEPENDENCIES (Files that use this):
[List of files that import/call this file]

DOWNSTREAM DEPENDENCIES (What this file uses):
[List of files/libraries this file depends on]

BUSINESS IMPACT: [HIGH/MEDIUM/LOW] - [CRITICAL/IMPORTANT/SUPPORTING]
[Description of what breaks if this file fails]

MIGRATION ACTIONS NEEDED (.NET Framework → .NET 8):
- [ ] [Specific technical tasks for migration]

FUTURE ENHANCEMENTS RECOMMENDED:
- [ ] [Business-driven improvements to consider]

MIGRATION PRIORITY: [High/Medium/Low] ([Reasoning])
COMPLEXITY LEVEL: [High/Medium/Low] ([File size, dependencies, business logic])
AI ANALYSIS CONFIDENCE: [High/Medium/Low] ([Basis for confidence level])
=============================================================================
*/
```

### Documentation Process
1. **Business context first** - Always understand business purpose before technical analysis
2. **Dependency mapping** - Identify upstream/downstream relationships
3. **Migration focus** - Every file gets migration-specific action items
4. **Future-oriented** - Include enhancement recommendations
5. **Confidence levels** - State limitations and verification needs

## FILES COMPLETED ✅

### ReturnModel.cs - Foundation File
- **Status:** ✅ Documented & Added to Code
- **Location:** `src/MBS.Common/ReturnModel.cs`
- **Category:** Foundation File (Low dependencies, high usage)
- **Business Impact:** HIGH - OPERATIONAL CRITICAL
- **Migration Priority:** High
- **Complexity Level:** Low (Simple data structures, minimal dependencies)
- **Key Insight:** Core "translator" between MSB API responses and internal system
- **Dependencies Mapped:** 5 upstream files identified
- **Migration Actions:** 5 specific tasks defined
- **Business Context:** Defines enums and data structures for MSB communication; if broken, automated claim processing stops

**Complete Documentation Added to Code:**
```csharp
/*
=============================================================================
DOCUMENTATION CREATED USING PMB AI-HUMAN WORKING AGREEMENT
Project: Perspect Medical Billing System (.NET Framework → .NET 8 migration)
Stakes: Medical practices depend on this system - zero tolerance for breaking changes
=============================================================================

FILE: ReturnModel.cs (Foundation File - Used by entire system)
BUSINESS PURPOSE: 
Defines the "language" for communicating with MSB (Medical Services Branch).
When MSB sends back responses to claims, this file tells our system how to 
interpret and categorize those responses.

CORE BUSINESS WORKFLOW:
1. Physicians submit claims → MSB reviews → MSB sends return files
2. DAILY returns = Rejections only (allows quick fixes before 2-week deadline)
3. BIWEEKLY returns = Complete results (paid + rejected claims)
4. WCB returns = Workers compensation (separate fax workflow)
5. System automatically processes these using API keys embedded per practice

KEY COMPONENTS:
- ReturnFileType enum: Categorizes MSB response files (DAILY/BIWEEKLY/WCB/MANUAL)
- ErrorType enum: Defines 9 types of processing errors that can occur
- MSBAccessToken class: Handles API authentication with MSB systems
- ReturnModel classes: Data structures for file processing

UPSTREAM DEPENDENCIES (Files that use this):
- ServiceRecordController.cs (claim submission & status display)
- ReturnRetrivalJob.cs (automated daily/biweekly file pickup)
- ClaimService.cs (MSB API integration)
- ReturnParser.cs (converts MSB format to database format)
- SubmitPendingClaims.cs (processes submitted claim responses)

DOWNSTREAM DEPENDENCIES (What this file uses):
- Only basic .NET System libraries (very stable)
- No dependencies on other MBS files

BUSINESS IMPACT: HIGH - OPERATIONAL CRITICAL
- If broken: Automated claim processing stops, manual intervention required
- Revenue impact: Delayed processing, missed 2-week correction deadlines
- Fallback: Manual download from MSB portal + manual upload (labor intensive)
- Affects: All practices using the automated billing platform

MIGRATION ACTIONS NEEDED (.NET Framework → .NET 8):
- [ ] Verify enum serialization patterns work in .NET 8
- [ ] Test JSON serialization for API communication
- [ ] Validate MSBAccessToken OAuth flow compatibility
- [ ] Test ReturnFileType enum database storage/retrieval
- [ ] Verify ErrorType enum handling in error scenarios

FUTURE ENHANCEMENTS RECOMMENDED:
- [ ] Add manual file upload capability as backup to API processing
- [ ] Consider adding more granular error types for better troubleshooting
- [ ] Evaluate adding retry logic definitions for failed API calls

MIGRATION PRIORITY: High (Foundation file - many others depend on this)
COMPLEXITY LEVEL: Low (Simple data structures, minimal dependencies)
AI ANALYSIS CONFIDENCE: High (complete file analysis, business context verified)
=============================================================================
*/
```

## NEXT TARGETS 🎯

### Immediate Queue (Low Dependency Foundation Files):

#### 1. EnumTypes.cs - NEXT TARGET
- **Location:** `src/MBS.Web.Portal/Constants/EnumTypes.cs`
- **Expected Category:** Foundation File
- **Size:** 48 lines (small, manageable)
- **Purpose:** Business workflow states and report types
- **Dependencies:** Minimal (only basic System.Web.*)
- **Priority:** High (used throughout system for dropdowns/states)
- **Business Value:** Defines user interface states and report categories

#### 2. ServiceRecord.cs
- **Location:** `src/MBS.DomainModel/ServiceRecord.cs`
- **Expected Category:** Domain Model (Auto-generated entity)
- **Size:** 68 lines
- **Purpose:** Core medical claim entity
- **Dependencies:** Minimal (auto-generated from database)
- **Priority:** High (foundation for all claim operations)
- **Business Value:** Represents individual medical service claims

#### 3. StaticCodeList.cs
- **Location:** `src/MBS.DataCache/StaticCodeList.cs`
- **Expected Category:** Data/Lookup File
- **Size:** 11,409 lines (massive but simple structure)
- **Purpose:** Medical codes, diagnosis codes, fee schedules
- **Dependencies:** Only System.Collections.Generic
- **Priority:** Medium (important but large/repetitive)
- **Business Value:** Central repository for all medical billing codes

### Future Queue (Higher Complexity):

#### Service Layer Files:
- **WCBPdfCreator.cs** - PDF generation for workers compensation claims
- **ReturnParser.cs** - Processes MSB return files
- **ClaimService.cs** - MSB API integration

#### Controller Files:
- **ServiceRecordController.cs** - The 3,707 line beast! (Save for last)
- **HomeController.cs** - Dashboard functionality
- **AccountController.cs** - Authentication

#### Repository Files:
- **ServiceRecordRepository.cs** - Database operations for claims
- Various other repository classes

## BUSINESS CONTEXT UNDERSTANDING

### Core Medical Billing Workflow:
1. **Claim Creation:** Physicians enter patient/service data via web portal
2. **Claim Submission:** System submits to MSB via API (or WCB via fax)
3. **Processing:** MSB reviews claims → Pay, Hold, or Reject with codes
4. **Return Processing:** System picks up daily rejections + biweekly complete returns
5. **Status Display:** Physicians see results, fix rejections, resubmit

### Key Business Rules:
- **2-week deadline:** Rejections must be fixed and resubmitted within 2 weeks
- **Daily rejections:** New feature to allow quick fixes before deadline
- **WCB separate:** Workers compensation uses different workflow (PDF + fax)
- **API keys per practice:** Each doctor has embedded authentication
- **Fallback option:** Manual download/upload possible but labor-intensive

### System Architecture Insights:
- **Foundation files:** Define data structures and enums (low dependencies)
- **Service layer:** Business logic and external integrations
- **Controller layer:** Web interface and user interactions
- **Repository layer:** Database operations
- **Auto-generated files:** Entity Framework models from database

## CONVERSATION STARTER TEMPLATE

### For Next AI Session:
```
Please read this PMB Code Review Master Tracker file first. We're documenting the PMB medical billing system for .NET 8 migration using our established working agreement and documentation template.

CONTEXT: We've completed ReturnModel.cs (foundation file for MSB API communication). 

NEXT TARGET: [FILE NAME] - [BRIEF DESCRIPTION]

PROCESS:
1. Understand business purpose first
2. Map dependencies (upstream/downstream)
3. Assess migration risks and actions needed
4. Create comprehensive documentation using our template
5. Update this tracker file

The goal is business-context-rich documentation that helps with migration planning and developer handover.
```

## MIGRATION STRATEGY NOTES

### Bottom-Up Approach Rationale:
- Start with foundation files (minimal dependencies, high usage)
- Build understanding of core business entities first
- Map dependency relationships before tackling complex controllers
- Identify migration risks early in foundational components

### Risk Assessment Framework:
- **High Risk:** Complex controllers, external API integrations, database operations
- **Medium Risk:** Service layer files, PDF generation, file processing
- **Low Risk:** Foundation files, enums, simple data structures

### Success Metrics:
- Complete business context documentation for all critical files
- Clear migration action items for each file
- Dependency mapping to understand change impact
- Risk assessment to prioritize migration efforts

---

*This tracker serves as the single source of truth for our documentation effort and should be updated after each file completion.*