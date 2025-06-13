# PMB CODE REVIEW MASTER TRACKER
*Updated: June 2025, 1:24PM

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

## VECTOR DATABASE SEARCH GUIDE

### Current Capabilities & Limitations

#### ✅ What Works Well
1. **Direct file access**: `get_file_content("aidocs/CLAUDE.md")` - Always works if you know the path
2. **Listing files**: `list_files(type=".md")` - Shows all indexed files
3. **Content searches**: Searching for exact headings or unique phrases
   - `"# PMB CODE REVIEW MASTER TRACKER"` → finds CLAUDE.md
   - `"PMB_Migration_Plan_NET8"` → finds that specific document
4. **Semantic searches**: General topic queries return related documents
   - `"billing system migration"` → returns relevant docs

#### ❌ Current Limitations
1. **Filename searches unreliable**: `"CLAUDE.md"` may not find the file
2. **No metadata filtering**: Can't use `where` clauses to filter by path
3. **Header searches inconsistent**: `"File: aidocs/CLAUDE.md"` doesn't guarantee results
4. **Recent files may rank low**: Newly indexed content appears after older similar content

### Effective Search Strategies

#### 🎯 To Find Specific Files
Instead of searching for filename, use:
1. **Unique content**: Search for the main heading or a distinctive phrase
   ```
   query: "PMB CODE REVIEW MASTER TRACKER"  # Finds CLAUDE.md
   query: "PMB_Migration_Plan_NET8"         # Finds migration plan
   ```

2. **Two-step approach**: List files first, then access directly
   ```
   Step 1: list_files(type=".md")  # Find the path
   Step 2: get_file_content("aidocs/CLAUDE.md")  # Read it
   ```

#### 🔍 To Find Topics/Concepts
Use semantic searches with domain terms:
- `"medical billing migration NET8"`
- `"claim processing architecture"`
- `"PMB validation requirements"`

#### 📁 To Navigate Documentation
1. **By category**: Search for category names
   - `"planning documents"` 
   - `"architecture analysis"`
   - `"infrastructure setup"`

2. **By content type**: Include document markers
   - `"Type: Documentation"`
   - `"Category: planning"`

### Quick Reference Commands

```python
# Get this file (CLAUDE.md)
get_file_content("aidocs/CLAUDE.md")

# List all documentation
list_files(type=".md")

# Find by exact heading
query_vector_db("# PMB CODE REVIEW MASTER TRACKER")

# Find by topic
query_vector_db("billing system architecture")

# Access specific file when path is known
get_file_content("aidocs/planning/PMB_Migration_Plan_NET8.md")
```

### Working Around Limitations

**Problem**: Need to find a file by name
**Solution**: Search for its unique heading or first line of content

**Problem**: Search returns wrong files
**Solution**: Use more specific content phrases from inside the document

**Problem**: Can't filter by folder
**Solution**: Include category names in search: "planning validation requirements"

**Problem**: Recent updates not found
**Solution**: Search for exact new content added, or use direct file access

### Future Improvements Needed
1. Enable metadata filtering in `query_vector_db()`
2. Add dedicated filename search function
3. Implement path-based filtering
4. Improve ranking for recently updated files

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

# ReturnModel.cs File review

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



### EnumTypes.cs - Foundation File
- **Status:** ✅ Documented & Added to Code
- **Location:** `src/MBS.Web.Portal/Constants/EnumTypes.cs`
- **Category:** Foundation File (Business workflow states and report types)
- **Business Impact:** HIGH - OPERATIONAL CRITICAL
- **Migration Priority:** High
- **Complexity Level:** Low (Simple enum definitions, stable values)
- **Key Insight:** Defines UI states and business workflows; mixed storage (integers/strings) reflects internal logic vs external API requirements
- **Dependencies Mapped:** 5+ upstream files identified (controllers, repositories, services)
- **Migration Actions:** 5 specific verification tasks defined
- **Business Context:** Controls dashboard display, claim status progression, and report generation; if broken, UI becomes unusable

**Complete Documentation Added to Code:**
```csharp
/*
=============================================================================
DOCUMENTATION CREATED USING PMB AI-HUMAN WORKING AGREEMENT
Project: Perspect Medical Billing System (.NET Framework → .NET 8 migration)
Stakes: Medical practices depend on this system - zero tolerance for breaking changes
=============================================================================

FILE: EnumTypes.cs (Foundation File - Business Workflow States)
BUSINESS PURPOSE: 
Defines the "states" and "categories" that drive the entire medical billing workflow.
When doctors see claim statuses in the dashboard or generate reports, these enums 
determine what options they have and what the system displays.

CORE BUSINESS WORKFLOW:
1. Doctor enters claims (Unsubmitted) → Hits submit → API to MSB (Submitted)
2. MSB processes claims → Returns: Paid (with payment) | Rejected (with codes) | Pending (under review)
3. System displays status to doctors using SearchClaimType enum values
4. Doctors generate reports using ReportType enum options
5. WCB claims follow separate workflow using ClaimType.WCB

KEY COMPONENTS:
- SearchClaimType enum: Claim lifecycle states (Unsubmitted→Submitted→Pending→Paid/Rejected)
- ReportType enum: Business report categories (Paid, Rejected, UnitRecord combinations)
- ClaimType enum: Insurance provider routing (MSB=Medical Services Branch, WCB=Workers Comp)
- DeliverStatus enum: External delivery tracking (WCB fax system integration)
- ClaimsInType enum: Return file processing categories

UPSTREAM DEPENDENCIES (Files that use this):
- ServiceRecordController.cs (UI state management, claim filtering)
- ServiceRecordRepository.cs (database queries by claim status)
- ClaimsReportCreator.cs (report generation using ReportType)
- FeedbackController.cs (WCB delivery status tracking)
- Multiple service classes (search and filtering operations)

DOWNSTREAM DEPENDENCIES (What this file uses):
- System.Web (basic web framework)
- No dependencies on other MBS files

BUSINESS IMPACT: HIGH - OPERATIONAL CRITICAL
- If broken: Dashboard shows wrong claim counts, reports fail, search broken
- Revenue impact: Doctors can't track claim status or generate required reports
- User experience: UI becomes unusable for claim management
- Data integrity: Status transitions could fail or display incorrectly

MIGRATION ACTIONS NEEDED (.NET Framework → .NET 8):
- [ ] Verify integer enum values preserved exactly in database storage
- [ ] Test SearchClaimType filtering in UI after EF6→EF Core migration
- [ ] Validate ReportType enum usage in report generation services
- [ ] Confirm ClaimType routing logic works with .NET 8 HTTP clients
- [ ] Test DeliverStatus integration with Interfax WCB fax service

FUTURE ENHANCEMENTS RECOMMENDED:
- [ ] Consider adding audit trail for claim status transitions
- [ ] Evaluate adding more granular pending states (MSB processing vs technical issues)
- [ ] Add enum for priority levels (routine vs urgent claims)

MIGRATION PRIORITY: High (Foundation file - UI and business logic depend on these)
COMPLEXITY LEVEL: Low (Simple enum definitions, stable values)
AI ANALYSIS CONFIDENCE: High (Complete file analysis, business workflow verified, production system confirmed working)
=============================================================================
*/
```




## NEXT TARGETS 🎯

### Immediate Queue (Low Dependency Foundation Files):


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