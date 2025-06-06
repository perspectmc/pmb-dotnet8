CREATE TABLE [dbo].[ClaimsIn] (
    [ClaimsInId]   UNIQUEIDENTIFIER CONSTRAINT [DF_ClaimsIn_ClaimsInId] DEFAULT (newid()) NOT NULL,
    [UserId]       UNIQUEIDENTIFIER NOT NULL,
    [CreatedDate]  DATETIME         CONSTRAINT [DF_ClaimsIn_CreatedDate] DEFAULT (getdate()) NOT NULL,
    [DownloadDate] DATETIME         CONSTRAINT [DF_ClaimsIn_DownloadDate] DEFAULT (getdate()) NULL,
    [RecordIndex]  NVARCHAR (20)    NULL,
    [ClaimAmount]  FLOAT (53)       CONSTRAINT [DF_ClaimsIn_ClaimAmount] DEFAULT ((0)) NOT NULL,
    [PaidAmount] FLOAT NOT NULL DEFAULT 0, 
    CONSTRAINT [PK_ClaimsIn] PRIMARY KEY CLUSTERED ([ClaimsInId] ASC),
    CONSTRAINT [FK_ClaimsIn_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
);



