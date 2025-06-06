CREATE TABLE [dbo].[RejectedClaim] (
    [RejectedClaimId]  UNIQUEIDENTIFIER CONSTRAINT [DF_RejectedClaim_RejectedClaimId] DEFAULT (newid()) NOT NULL,
    [ClaimsInReturnId] UNIQUEIDENTIFIER NOT NULL,
    [CreatedDate]      DATETIME         NOT NULL,
    CONSTRAINT [PK_RejectedClaim] PRIMARY KEY CLUSTERED ([RejectedClaimId] ASC),
    CONSTRAINT [FK_RejectedClaim_ClaimsInReturn] FOREIGN KEY ([ClaimsInReturnId]) REFERENCES [dbo].[ClaimsInReturn] ([ClaimsInReturnId])
);

