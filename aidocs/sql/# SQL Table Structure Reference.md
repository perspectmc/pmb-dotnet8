# SQL Table Structure Reference

_This document outlines the database schema used in the Perspect Medical Billing platform._

## 📋 Table of Contents

- [Applications](#table-applications)
- [ClaimsIn](#table-claimsin)
- [PaidClaim](#table-paidclaim)
- [RejectedClaim](#table-rejectedclaim)
- [ClaimsInReturn](#table-claimsinreturn)
- [Users](#table-users)
- [ClaimsStatus](#table-claimsstatus)
- [ClaimNotes](#table-claimnotes)
- [PaymentMethods](#table-paymentmethods)
- [AuditLog](#table-auditlog)
- [ClaimsResubmitted](#table-claimsresubmitted)
- [ClaimsReturnPaymentSummary](#table-claimsreturnpaymentsummary)
- [FaxDeliver](#table-faxdeliver)
- [Memberships](#table-memberships)
- [Roles](#table-roles)
- [ServiceRecord](#table-servicerecord)
- [UnitRecord](#table-unitrecord)
- [UserCertificates](#table-usercertificates)
- [UserProfiles](#table-userprofiles)
- [UsersInRoles](#table-usersinroles)

Each section describes a specific table, including column names, data types, lengths, and any constraints. This file serves as a reference for both development and modernization efforts.

## Table: Applications

| Column          | Data Type       | Length/Precision | Constraints |
|-----------------|-----------------|------------------|-------------|
| ApplicationName | nvarchar        | 235.0            | NOT NULL    |
| ApplicationId   | uniqueidentifier|                  | NOT NULL    |
| Description     | nvarchar        | 256.0            |             |

| Table Purpose   | Tracks submitted claims or applications; likely used for grouping claims into batches or logical submissions. |  |  |
| Used In Files   | ClaimController.cs, ClaimsInRepository.cs, Applications.cs, ClaimSubmitter.cs |  |  |
| Connected Tables| ClaimsIn, PaidClaim, RejectedClaim, ClaimsInReturn (via foreign keys or indirect references) |  |  |
| Risks / Observations | Abstract naming — unclear how this table differs from ClaimsIn at a business level. Few constraints defined. |  |  |
| Recommendations | Clarify functional role of “Application” vs “Claim”. Add audit metadata (timestamps, user tracking). Document submission logic. |  |  |

## Table: ClaimsIn

| Column       | Data Type       | Length/Precision | Constraints |
|--------------|-----------------|------------------|-------------|
| ClaimsInId   | uniqueidentifier|                  | NOT NULL    |
| UserId       | uniqueidentifier|                  | NOT NULL    |
| CreatedDate  | datetime        |                  | NOT NULL    |
| DownloadDate | datetime        |                  |             |
| RecordIndex  | nvarchar        | 20               |             |
| ClaimAmount  | float           |                  | NOT NULL    |
| PaidAmount   | float           |                  | NOT NULL    |

## Table: PaidClaim

| Column        | Data Type       | Length/Precision | Constraints |
|---------------|-----------------|------------------|-------------|
| PaidClaimId   | uniqueidentifier|                  | NOT NULL    |
| ClaimId       | uniqueidentifier|                  | NOT NULL    |
| PaymentDate   | datetime        |                  |             |
| AmountPaid    | float           |                  | NOT NULL    |
| PaymentMethod | nvarchar        | 50               |             |

## Table: RejectedClaim

| Column         | Data Type       | Length/Precision | Constraints |
|----------------|-----------------|------------------|-------------|
| RejectedClaimId| uniqueidentifier|                  | NOT NULL    |
| ClaimId        | uniqueidentifier|                  | NOT NULL    |
| RejectionDate  | datetime        |                  | NOT NULL    |
| Reason         | nvarchar        | 500              |             |

## Table: ClaimsInReturn

| Column          | Data Type       | Length/Precision | Constraints |
|-----------------|-----------------|------------------|-------------|
| ClaimsInReturnId| uniqueidentifier|                  | NOT NULL    |
| ClaimsInId      | uniqueidentifier|                  | NOT NULL    |
| ReturnDate      | datetime        |                  | NOT NULL    |
| ReturnReason    | nvarchar        | 500              |             |

## Table: Users

| Column         | Data Type       | Length/Precision | Constraints |
|----------------|-----------------|------------------|-------------|
| UserId         | uniqueidentifier|                  | NOT NULL    |
| UserName       | nvarchar        | 100              | NOT NULL    |
| Email          | nvarchar        | 256              |             |
| CreatedDate    | datetime        |                  | NOT NULL    |
| IsActive       | bit             |                  | NOT NULL    |

## Table: ClaimsStatus

| Column         | Data Type       | Length/Precision | Constraints |
|----------------|-----------------|------------------|-------------|
| StatusId       | uniqueidentifier|                  | NOT NULL    |
| StatusName     | nvarchar        | 50               | NOT NULL    |
| Description    | nvarchar        | 256              |             |

## Table: ClaimNotes

| Column         | Data Type       | Length/Precision | Constraints |
|----------------|-----------------|------------------|-------------|
| NoteId         | uniqueidentifier|                  | NOT NULL    |
| ClaimId        | uniqueidentifier|                  | NOT NULL    |
| NoteText       | nvarchar        | 1000             |             |
| CreatedDate    | datetime        |                  | NOT NULL    |
| CreatedBy      | uniqueidentifier|                  | NOT NULL    |

## Table: PaymentMethods

| Column         | Data Type       | Length/Precision | Constraints |
|----------------|-----------------|------------------|-------------|
| PaymentMethodId| uniqueidentifier|                  | NOT NULL    |
| MethodName     | nvarchar        | 50               | NOT NULL    |
| Description    | nvarchar        | 256              |             |

## Table: AuditLog

| Column         | Data Type       | Length/Precision | Constraints |
|----------------|-----------------|------------------|-------------|
| AuditLogId     | uniqueidentifier|                  | NOT NULL    |
| TableName      | nvarchar        | 100              | NOT NULL    |
| RecordId       | uniqueidentifier|                  | NOT NULL    |
| Action         | nvarchar        | 50               | NOT NULL    |
| ActionDate     | datetime        |                  | NOT NULL    |
| UserId         | uniqueidentifier|                  |             |
| Details        | nvarchar        | 2000             |             |


## Table: ClaimsResubmitted

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| ResubmittedId | uniqueidentifier |  | NOT NULL |
| OriginalClaimId | uniqueidentifier |  | NOT NULL |
| ResubmissionDate | datetime |  | NOT NULL |
| Reason | nvarchar | 500 |  |

## Table: ClaimsReturnPaymentSummary

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| SummaryId | uniqueidentifier |  | NOT NULL |
| ApplicationId | uniqueidentifier |  | NOT NULL |
| PaymentTotal | float |  | NOT NULL |
| ClaimCount | int |  | NOT NULL |

## Table: FaxDeliver

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| FaxId | uniqueidentifier |  | NOT NULL |
| Recipient | nvarchar | 100 | NOT NULL |
| SentDate | datetime |  | NOT NULL |
| Status | nvarchar | 50 |  |

## Table: Memberships

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| MembershipId | uniqueidentifier |  | NOT NULL |
| UserId | uniqueidentifier |  | NOT NULL |
| MembershipLevel | nvarchar | 50 |  |
| StartDate | datetime |  | NOT NULL |

## Table: Roles

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| RoleId | uniqueidentifier |  | NOT NULL |
| RoleName | nvarchar | 100 | NOT NULL |

## Table: ServiceRecord

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| RecordId | uniqueidentifier |  | NOT NULL |
| ClaimsInId | uniqueidentifier |  | NOT NULL |
| ServiceDate | datetime |  | NOT NULL |
| ServiceCode | nvarchar | 20 | NOT NULL |
| FeeAmount | float |  | NOT NULL |

## Table: UnitRecord

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| UnitRecordId | uniqueidentifier |  | NOT NULL |
| ServiceRecordId | uniqueidentifier |  | NOT NULL |
| Units | int |  | NOT NULL |

## Table: UserCertificates

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| CertificateId | uniqueidentifier |  | NOT NULL |
| UserId | uniqueidentifier |  | NOT NULL |
| CertificateData | varbinary | max | NOT NULL |

## Table: UserProfiles

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| ProfileId | uniqueidentifier |  | NOT NULL |
| UserId | uniqueidentifier |  | NOT NULL |
| DisplayName | nvarchar | 100 |  |
| DefaultDiagnosticCode | nvarchar | 20 |  |

## Table: UsersInRoles

| Column | Data Type | Length/Precision | Constraints |
|--------|-----------|------------------|-------------|
| UserId | uniqueidentifier |  | NOT NULL |
| RoleId | uniqueidentifier |  | NOT NULL |
