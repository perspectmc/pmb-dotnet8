CREATE TABLE [dbo].[ServiceRecord] (
    [ServiceRecordId]       UNIQUEIDENTIFIER CONSTRAINT [DF_ServiceRecord_ServiceRecordId] DEFAULT (newid()) NOT NULL,
    [ClaimsInId]            UNIQUEIDENTIFIER NULL,
    [ClaimNumber]           INT              NOT NULL,
    [PatientFirstName]      NVARCHAR (255)   NULL,
    [PatientLastName]       NVARCHAR (255)   NULL,
    [DateOfBirth]           DATETIME         NOT NULL,
    [Sex]                   NVARCHAR (1)     NOT NULL,
    [Province]              NVARCHAR (25)    NOT NULL,
    [HospitalNumber]        NVARCHAR (255)   NULL,
    [ReferringDoctorNumber] INT              NULL,
    [ServiceDate]           DATETIME         NOT NULL,
    [ServiceStartTime]      TIME (7)         NULL,
    [ServiceEndTime]        TIME (7)         NULL,
    [LastModifiedDate]      DATETIME         CONSTRAINT [DF_ServiceRecord_LastModifiedDate] DEFAULT (getdate()) NULL,
    [Comment]               NVARCHAR (100)   NULL,
    [PaidClaimId]           UNIQUEIDENTIFIER NULL,
    [RejectedClaimId]       UNIQUEIDENTIFIER NULL,
    [CreatedDate]           DATETIME         CONSTRAINT [DF_ServiceRecord_CreatedDate] DEFAULT (getdate()) NOT NULL,
    [ClaimAmount]           FLOAT (53)       CONSTRAINT [DF_ServiceRecord_ClaimAmount] DEFAULT ((0)) NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL, 
    [PaidAmount] FLOAT NOT NULL, 
    CONSTRAINT [PK_ServiceRecord] PRIMARY KEY CLUSTERED ([ServiceRecordId] ASC),
    CONSTRAINT [FK_ServiceRecord_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]),
	CONSTRAINT [FK_ServiceRecord_ClaimsIn] FOREIGN KEY ([ClaimsInId]) REFERENCES [dbo].[ClaimsIn] ([ClaimsInId]),
    CONSTRAINT [FK_ServiceRecord_PaidClaim] FOREIGN KEY ([PaidClaimId]) REFERENCES [dbo].[PaidClaim] ([PaidClaimId]),
    CONSTRAINT [FK_ServiceRecord_RejectedClaim] FOREIGN KEY ([RejectedClaimId]) REFERENCES [dbo].[RejectedClaim] ([RejectedClaimId])
);



