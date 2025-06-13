# PMB Medical Billing System - Validation Requirements Analysis
## .NET 8 Migration - Phase 1.1.2 Functional Requirements

*Based on ServiceRecord model analysis - June 7, 2025*

---

## 1. Data Annotation Validation Attributes (ServiceRecordMetaData.cs)

### Required Fields:
- `PatientFirstName` - Required, Display Name: "First Name"
- `PatientLastName` - Required, Display Name: "Last Name" 
- `DateOfBirth` - Required, DataType.Date, Display Name: "Date Of Birth"
- `Sex` - Required, Display Name: "Sex"
- `HospitalNumber` - Required, Display Name: "Hospital Number"
- `ServiceDate` - Required, DataType.Date, DisplayFormat: "{0:ddMMyy}", Display Name: "Date Of Service"

### Optional Fields with Display Names:
- `Province` - Display Name: "Province"
- `ReferringDoctorNumber` - Display Name: "Referring Doctor Number"
- `DischargeDate` - DataType.Date, DisplayFormat: "{0:ddMMyy}", Display Name: "Date Of Discharge"
- `ServiceStartTime` - Display Name: "Start Time"
- `ServiceEndTime` - Display Name: "End Time"
- `Comment` - Display Name: "Comment"
- `Notes` - Display Name: "Personal Notes"
- `ServiceLocation` - Display Name: "Service Location"
- `FacilityNumber` - Display Name: "Facility Number"

### View Model Validation (ServiceRecordModels.cs):
- `BirthDateString` - Required, Display Name: "Date of Birth"
- `ServiceDateString` - Required, Display Name: "Date Of Service"
- `DischargeDateString` - Display Name: "Date Of Discharge"
- `ServiceStartTimeString` - Display Name: "Start Time"
- `ServiceEndTimeString` - Display Name: "End Time"

---

## 2. Custom Server-Side Business Rule Validation (ServiceRecordController.cs)

### Core Field Validation Methods:
- **`IsSexValid()`** - Validates sex is "M" or "F" (case insensitive)
- **`IsHospitalNumberValid()`** - Complex province-specific hospital number validation:
  - SK: 9 digits with Modulus 11 check digit validation
  - BC/ON/NS: 10 numeric digits
  - AB/MB/NB/YT: 9 numeric digits
  - PE: 8 numeric digits
  - NL: 12 numeric digits
  - NT: Format D/H/M/N/T + 7 numeric (8 total)
  - NU: Format 1 + 8 numeric (9 total)
  - QC: Not supported (always invalid)

- **`IsDoctorNumberValid()`** - Validates referring doctor numbers against static code list and special codes (9908, 9907, 9909, 9906, 9905, 9900)

- **`IsBirthDateValid()`** - Validates 4-character birth date format (MMYY)

- **`IsServiceDateValid()`** - Validates service date within 4-year range and not future dated

### Business Logic Validation:
- **`IsAtLeastOneUnitRecord()`** - Ensures at least one unit record exists
- **`IsOnlyOneSpecialUnitRecord()`** - Limits special codes (893A, 894A, 895A) to one per claim
- **`IsFiveWCBClaimRecords()`** - WCB claims with premium codes B/K limited to 5 unit records
- **`IsNotWCBRecordForMSBClaimRecord()`** - Prevents WCB codes in MSB claims
- **`GetNumberOfHospitalCareCode()`** - Limits hospital care service records to maximum of 2 per claim

### Unit Record Validation:
- **`IsUnitCodeValid()`** - Validates unit codes against fee code lists (MSB/WCB)
- **`IsUnitNumberValid()`** - Ensures unit numbers are positive integers
- **`IsDiagCodeValid()`** - Validates diagnostic codes against ICD code list
- **Time validation** - Start/end times in HHMM format (0000-2359)
- **Amount validation** - Manual entry amounts for zero-fee codes
- **Special circumstance validation** - Only allowed for fee determinant W/X codes (except TA)

### Comment Length Limits:
- MSB Claims: 770 characters maximum
- WCB Claims: 425 characters maximum
- Personal Notes: 1000 characters maximum

---

## 3. Client-Side Validation (ServiceRecordAction.js)

### Real-time Field Validation:
- **Hospital Number Validation** - `ValidationHSN()` function with province-specific rules
- **Time Format Validation** - `ValidateUnitTime()` for HHMM format
- **Character Count Validation** - Real-time comment/notes length checking
- **Required Field Indicators** - Dynamic red asterisk (*) for required fields

### Auto-completion and Data Integrity:
- Patient lookup by hospital number or name
- Unit code auto-completion with fee validation
- Diagnostic code auto-completion with ICD validation
- Referring doctor auto-completion
- Automatic premium code calculation based on service times and dates

### Business Rule Enforcement:
- Special code validation (893A, 894A, 895A) - only one allowed
- Unit time calculations for specific fee codes
- Premium code assignment based on service times and weekends
- Automatic unit number calculation for time-based codes
- Fee amount calculations with premium adjustments

### Dynamic UI Behavior:
- Discharge date field shown only for hospital care codes
- Manual entry fields enabled for zero-fee codes
- Required field indicators for time-dependent codes
- Referring doctor requirement for specific fee codes

---

## 4. Migration Requirements for .NET 8

### PRESERVE EXACTLY (Critical Business Rules):
1. **Province-specific hospital number validation** - Complex algorithms especially for SK (Modulus 11)
2. **Claim type restrictions** - WCB vs MSB code validation
3. **Special code limitations** - Only one 893A/894A/895A per claim
4. **Unit record limits** - Maximum counts and business logic
5. **Time-based calculations** - Premium codes and unit calculations
6. **Fee code validation** - Against static code lists with version control
7. **Comment length restrictions** - Different limits by claim type
8. **Required field dependencies** - Dynamic requirements based on selected codes

### PRESERVE EXACTLY (Data Integrity Rules):
1. **Cross-field validation** - Service date vs discharge date relationships
2. **Code list validation** - ICD, fee codes, referring doctors
3. **Amount calculations** - Fee × units + premiums with rounding rules
4. **Time validation** - 24-hour format with business hour premium logic

### PRESERVE EXACTLY (User Experience Features):
1. **Auto-completion** - Patient, code, and doctor lookups
2. **Real-time validation** - Immediate feedback on field changes
3. **Dynamic field requirements** - Context-sensitive required fields
4. **Calculation automation** - Time-based unit and premium calculations

---

## 5. .NET 8 Migration Strategy

### Data Annotations (Low Risk):
- **Migrate as-is** - Required, Display, DataType attributes
- **Update DisplayFormat** - ASP.NET Core compatible formats
- **Preserve error messages** - Maintain user-familiar text

### Custom Validation (Medium Risk):
- **Extract to services** - Move business rules from controllers
- **Preserve algorithms** - Hospital number validation logic
- **Maintain code lists** - ICD, fee codes, doctor numbers

### Client-Side Validation (High Risk):
- **jQuery modernization** - 2.1.1 → 3.6+ compatibility
- **Preserve functionality** - All auto-completion features
- **Maintain UX** - Real-time validation behavior

### Architecture Preparation:
- **Service layer** - Extract validation to reusable services
- **Async patterns** - Prepare for future performance improvements
- **API compatibility** - Enable future mobile/SPA integration

---

---

## 6. Phase 1.1.2 Requirements Q&A Results

### Q1: Claims Entry & Validation ✅
- **Q1a-c:** Preserve ALL validation rules exactly (MSB requirements)
- **Q1d:** No new validation requirements expected (MSB just upgraded after 20 years)
- **Future:** Prepare extensible rule engine for 300+ fee code rules via AI

### Q2: Submission & Processing ✅  
- **Q2a:** Modernize retry logic + prepare for AI rule validation pipeline
- **Future:** Excel document processing → "Imported" status → Approve → Unsubmitted

### Q3: Return Processing ✅
- **Q3a-b:** Keep ReturnParser.cs and reconciliation exactly as-is
- **Q3c:** No current manual intervention - will add for document processing

### Q4: User Management & Security ✅
- **Q4a:** Keep 60-minute timeout
- **Q4b:** Member/Administrator roles sufficient (use impersonation)

### Q5: External Integration ✅
- **Q5a:** Keep MSB API exactly as-is (brand new system)
- **Q5b:** Modernize OAuth with .NET 8 improvements

### Q6: Background Processing ✅
- **Q6a:** Keep current intervals as defaults + add admin dashboard controls
- **Q6b:** Keep all frequencies, make admin-configurable

### Q9: Security & Compliance ✅
- **Q9a:** Add basic access logging (configurable on/off)
- **Q9b:** Implement performance-friendly encryption (connection strings, TLS)
- **Q9c:** **DEFERRED:** Database encryption due to search performance impact
  - *Reason:* Partial matching on all fields (name, hospital number, fee codes) for reconciliation
  - *Future:* Phase 2 with encrypted indexes + search optimization

### Q12: Gap Analysis - Enhanced Admin Features ✅

**Functional Additions for Phase 1 (Database schema enables easy implementation):**

#### Q12a: Hot-Load Data Management (5-8 SP)
- **Current:** Manual copy/paste of MSB quarterly updates (MBS-Fees.txt, MBS-RefDoc.txt)
- **Future:** Admin interface for hot-loading without system restart
- **Implementation:** 
  - Convert text files to database tables (FeeCodes, ReferringDoctors)
  - Admin upload interface with validation
  - Runtime cache invalidation
  - Version control and rollback capability

#### Q12b: Admin Dashboard (8-12 SP) 
- **Current:** Must remote into VPS + check IIS logs
- **Future:** Web-based admin dashboard with metrics
- **Available Data (existing tables):**
  - Claims processing: ClaimsIn, PaidClaim, RejectedClaim (counts, revenue)
  - User activity: Users table + enhanced AuditLog population
  - System health: Background job status, API response times
  - Performance: Response times, peak usage patterns

#### Q12c: Enhanced Audit Logging (3-5 SP)
- **Current:** Basic AuditLog table (underutilized)
- **Future:** Comprehensive user activity tracking
- **Implementation:**
  - Populate existing AuditLog table with user access events
  - PIPEDA-compliant access tracking (configurable)
  - Dashboard integration for real-time monitoring

**Total Enhanced Admin Features: 16-25 SP (~1-2 weeks additional effort)**  
**Database schema analysis reveals implementation is simpler than initially estimated**

**Migration Tasks Added:**
1. **Cloud backup implementation** - automated database backups
2. **Backup restore testing** - verify recovery procedures work
3. **Hot-load data management** - fee codes + referring doctors
4. **Admin dashboard** - metrics and logging visibility
5. **Enhanced audit logging** - user activity tracking
6. **Admin configurability** - job schedules, file paths, settings
7. **Vendor abstraction layer** - fax service interface for quick replacement

---

## Decision Log

**Date:** June 7, 2025  
**Phase:** 1.1.2 Functional Requirements Complete  
**Key Decisions:**
1. **Preserve ALL business logic** exactly as-is
2. **Enhance admin configurability** (schedules, paths, settings)
3. **Modernize with .NET 8 features** (OAuth, security) 
4. **Prepare architecture seams** for AI rules + document processing
5. **Add vendor abstraction** for external services

**Next:** Phase 1.1.3 Non-Functional Requirements  
**Future Review:** Post-migration Phase 2 enhancements