# SQL Table Structure Reference - CORRECTED

_This document outlines the ACTUAL database schema used in the Perspect Medical Billing platform as of June 8, 2025._

## 📋 Table of Contents

- [Applications](#table-applications)
- [ClaimsIn](#table-claimsin)
- [ClaimsInReturn](#table-claimsinreturn)
- [ClaimsResubmitted](#table-claimsresubmitted)
- [ClaimsReturnPaymentSummary](#table-claimsreturnpaymentsummary)
- [FaxDeliver](#table-faxdeliver)
- [Memberships](#table-memberships)
- [PaidClaim](#table-paidclaim)
- [RejectedClaim](#table-rejectedclaim)
- [Roles](#table-roles)
- [ServiceRecord](#table-servicerecord)
- [UnitRecord](#table-unitrecord)
- [UserCertificates](#table-usercertificates)
- [UserProfiles](#table-userprofiles)
- [Users](#table-users)
- [UsersInRoles](#table-usersinroles)

**Total Tables: 16** (Production Verified Schema)

---

## Table: Applications

| Column          | Data Type        | Length/Precision | Constraints | Position |
|-----------------|------------------|------------------|-------------|----------|
| ApplicationName | nvarchar         | 235              | NOT NULL    | 1        |
| ApplicationId   | uniqueidentifier | -                | NOT NULL    | 2        |
| Description     | nvarchar         | 256              | NULL        | 3        |

**Purpose**: Tracks submitted claims or applications; used for grouping claims into batches or logical submissions.

---

## Table: ClaimsIn

| Column                | Data Type        | Length/Precision | Constraints | Position |
|-----------------------|------------------|------------------|-------------|----------|
| ClaimsInId           | uniqueidentifier | -                | NOT NULL    | 1        |
| UserId               | uniqueidentifier | -                | NOT NULL    | 2        |
| CreatedDate          | datetime         | -                | NOT NULL    | 3        |
| DownloadDate         | datetime         | -                | NULL        | 4        |
| RecordIndex          | nvarchar         | 20               | NULL        | 5        |
| ClaimAmount          | float            | -                | NOT NULL    | 6        |
| PaidAmount           | float            | -                | NOT NULL    | 7        |
| Content              | varchar          | MAX              | NULL        | 8        |
| ValidationContent    | varchar          | MAX              | NULL        | 9        |
| DateChangeToAccepted | datetime         | -                | NULL        | 10       |
| SubmittedFileName    | nvarchar         | 50               | NOT NULL    | 11       |
| FileSubmittedStatus  | nvarchar         | 10               | NOT NULL    | 12       |

**Purpose**: Core medical claims data with complete claim information and file tracking.

---

## Table: ClaimsInReturn

| Column               | Data Type        | Length/Precision | Constraints | Position |
|----------------------|------------------|------------------|-------------|----------|
| ClaimsInReturnId    | uniqueidentifier | -                | NOT NULL    | 1        |
| UserId              | uniqueidentifier | -                | NOT NULL    | 2        |
| TotalSubmitted      | float            | -                | NOT NULL    | 3        |
| TotalApproved       | float            | -                | NOT NULL    | 4        |
| UploadDate          | datetime         | -                | NOT NULL    | 5        |
| RecordIndex         | int              | -                | NOT NULL    | 6        |
| TotalPaid           | int              | -                | NOT NULL    | 7        |
| TotalRejected       | int              | -                | NOT NULL    | 8        |
| ReturnFooter        | nvarchar         | 110              | NULL        | 9        |
| Content             | varchar          | MAX              | NULL        | 10       |
| TotalPaidAmount     | float            | -                | NOT NULL    | 11       |
| TotalPremiumAmount  | float            | -                | NOT NULL    | 12       |
| TotalProgramAmount  | float            | -                | NOT NULL    | 13       |

**Purpose**: Claims return processing with financial totals and program amounts.

---

## Table: ClaimsResubmitted

| Column            | Data Type        | Length/Precision | Constraints | Position |
|-------------------|------------------|------------------|-------------|----------|
| ResubmittedId     | uniqueidentifier | -                | NOT NULL    | 1        |
| OriginalClaimId   | uniqueidentifier | -                | NOT NULL    | 2        |
| ResubmissionDate  | datetime         | -                | NOT NULL    | 3        |
| Reason            | nvarchar         | 500              | NULL        | 4        |

**Purpose**: Tracks resubmitted medical claims with original claim references.

---

## Table: ClaimsReturnPaymentSummary

| Column        | Data Type        | Length/Precision | Constraints | Position |
|---------------|------------------|------------------|-------------|----------|
| SummaryId     | uniqueidentifier | -                | NOT NULL    | 1        |
| ApplicationId | uniqueidentifier | -                | NOT NULL    | 2        |
| PaymentTotal  | float            | -                | NOT NULL    | 3        |
| ClaimCount    | int              | -                | NOT NULL    | 4        |

**Purpose**: Payment summary data for claims processing batches.

---

## Table: FaxDeliver

| Column    | Data Type        | Length/Precision | Constraints | Position |
|-----------|------------------|------------------|-------------|----------|
| FaxId     | uniqueidentifier | -                | NOT NULL    | 1        |
| Recipient | nvarchar         | 100              | NOT NULL    | 2        |
| SentDate  | datetime         | -                | NOT NULL    | 3        |
| Status    | nvarchar         | 50               | NULL        | 4        |

**Purpose**: Fax delivery tracking for medical document transmission.

---

## Table: Memberships

| Column          | Data Type        | Length/Precision | Constraints | Position |
|-----------------|------------------|------------------|-------------|----------|
| MembershipId    | uniqueidentifier | -                | NOT NULL    | 1        |
| UserId          | uniqueidentifier | -                | NOT NULL    | 2        |
| MembershipLevel | nvarchar         | 50               | NULL        | 3        |
| StartDate       | datetime         | -                | NOT NULL    | 4        |

**Purpose**: User membership levels and subscription tracking.

---

## Table: PaidClaim

| Column        | Data Type        | Length/Precision | Constraints | Position |
|---------------|------------------|------------------|-------------|----------|
| PaidClaimId   | uniqueidentifier | -                | NOT NULL    | 1        |
| ClaimId       | uniqueidentifier | -                | NOT NULL    | 2        |
| PaymentDate   | datetime         | -                | NULL        | 3        |
| AmountPaid    | float            | -                | NOT NULL    | 4        |
| PaymentMethod | nvarchar         | 50               | NULL        | 5        |

**Purpose**: Tracks paid medical claims with payment details.

---

## Table: RejectedClaim

| Column          | Data Type        | Length/Precision | Constraints | Position |
|-----------------|------------------|------------------|-------------|----------|
| RejectedClaimId | uniqueidentifier | -                | NOT NULL    | 1        |
| ClaimId         | uniqueidentifier | -                | NOT NULL    | 2        |
| RejectionDate   | datetime         | -                | NOT NULL    | 3        |
| Reason          | nvarchar         | 500              | NULL        | 4        |

**Purpose**: Tracks rejected medical claims with rejection reasons.

---

## Table: Roles

| Column   | Data Type        | Length/Precision | Constraints | Position |
|----------|------------------|------------------|-------------|----------|
| RoleId   | uniqueidentifier | -                | NOT NULL    | 1        |
| RoleName | nvarchar         | 100              | NOT NULL    | 2        |

**Purpose**: User role definitions for access control.

---

## Table: ServiceRecord

| Column      | Data Type        | Length/Precision | Constraints | Position |
|-------------|------------------|------------------|-------------|----------|
| RecordId    | uniqueidentifier | -                | NOT NULL    | 1        |
| ClaimsInId  | uniqueidentifier | -                | NOT NULL    | 2        |
| ServiceDate | datetime         | -                | NOT NULL    | 3        |
| ServiceCode | nvarchar         | 20               | NOT NULL    | 4        |
| FeeAmount   | float            | -                | NOT NULL    | 5        |

**Purpose**: Individual medical service records within claims.

---

## Table: UnitRecord

| Column          | Data Type        | Length/Precision | Constraints | Position |
|-----------------|------------------|------------------|-------------|----------|
| UnitRecordId    | uniqueidentifier | -                | NOT NULL    | 1        |
| ServiceRecordId | uniqueidentifier | -                | NOT NULL    | 2        |
| Units           | int              | -                | NOT NULL    | 3        |

**Purpose**: Unit tracking for medical services (quantity, duration).

---

## Table: UserCertificates

| Column          | Data Type        | Length/Precision | Constraints | Position |
|-----------------|------------------|------------------|-------------|----------|
| CertificateId   | uniqueidentifier | -                | NOT NULL    | 1        |
| UserId          | uniqueidentifier | -                | NOT NULL    | 2        |
| CertificateData | varbinary        | MAX              | NOT NULL    | 3        |

**Purpose**: Digital certificates for medical practitioners.

---

## Table: UserProfiles

| Column                   | Data Type        | Length/Precision | Constraints | Position |
|--------------------------|------------------|------------------|-------------|----------|
| UserId                   | uniqueidentifier | -                | NOT NULL    | 1        |
| DoctorNumber             | nvarchar         | 4                | NULL        | 2        |
| DoctorName               | nvarchar         | 80               | NOT NULL    | 3        |
| ClinicNumber             | nvarchar         | 3                | NULL        | 4        |
| DiagnosticCode           | nvarchar         | 3                | NOT NULL    | 5        |
| CorporationIndicator     | nvarchar         | 1                | NULL        | 6        |
| Street                   | nvarchar         | 50               | NULL        | 7        |
| City                     | nvarchar         | 50               | NOT NULL    | 8        |
| Province                 | nvarchar         | 30               | NOT NULL    | 9        |
| PostalCode               | nvarchar         | 6                | NOT NULL    | 10       |
| PhoneNumber              | nvarchar         | 15               | NULL        | 11       |
| ClaimMode                | nvarchar         | 1                | NOT NULL    | 12       |
| GroupNumber              | nvarchar         | 3                | NULL        | 13       |
| GroupUserKey             | nvarchar         | 100              | NULL        | 14       |
| DefaultPremCode          | nvarchar         | 1                | NULL        | 15       |
| DefaultServiceLocation   | nvarchar         | 1                | NULL        | 16       |

**Purpose**: Complete medical practitioner profiles with regulatory and billing information.

---

## Table: Users

| Column           | Data Type        | Length/Precision | Constraints | Position |
|------------------|------------------|------------------|-------------|----------|
| ApplicationId    | uniqueidentifier | -                | NOT NULL    | 1        |
| UserId           | uniqueidentifier | -                | NOT NULL    | 2        |
| UserName         | nvarchar         | 50               | NOT NULL    | 3        |
| IsAnonymous      | bit              | -                | NOT NULL    | 4        |
| LastActivityDate | datetime         | -                | NOT NULL    | 5        |

**Purpose**: Core user authentication data (custom authentication system).

---

## Table: UsersInRoles

| Column | Data Type        | Length/Precision | Constraints | Position |
|--------|------------------|------------------|-------------|----------|
| UserId | uniqueidentifier | -                | NOT NULL    | 1        |
| RoleId | uniqueidentifier | -                | NOT NULL    | 2        |

**Purpose**: Many-to-many relationship between users and roles.

---

## Schema Verification Notes

- **Production Database**: `medicalbillingsystem2017`
- **Verification Date**: June 8, 2025
- **Method**: Direct production database schema query
- **Total Tables**: 16 (verified complete)
- **Authentication**: Custom system (not ASP.NET Identity)
- **Key Medical Tables**: ClaimsIn (12 columns), UserProfiles (16 columns), ClaimsInReturn (13 columns)