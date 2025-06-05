CREATE TABLE [dbo].[PaidClaim] (
    [PaidClaimId]      UNIQUEIDENTIFIER CONSTRAINT [DF_PaidClaim_PaidClaimId] DEFAULT (newid()) NOT NULL,
    [ClaimsInReturnId] UNIQUEIDENTIFIER NOT NULL,
    [CreatedDate]      DATETIME         NOT NULL,
    CONSTRAINT [PK_PaidClaim] PRIMARY KEY CLUSTERED ([PaidClaimId] ASC),
    CONSTRAINT [FK_PaidClaim_ClaimsInReturn] FOREIGN KEY ([ClaimsInReturnId]) REFERENCES [dbo].[ClaimsInReturn] ([ClaimsInReturnId])
);

