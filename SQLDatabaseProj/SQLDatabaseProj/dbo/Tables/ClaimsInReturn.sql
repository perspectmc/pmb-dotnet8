CREATE TABLE [dbo].[ClaimsInReturn] (
    [ClaimsInReturnId] UNIQUEIDENTIFIER CONSTRAINT [DF_Table_1_ClaimsInReturn] DEFAULT (newid()) NOT NULL,
    [UserId]           UNIQUEIDENTIFIER NOT NULL,
    [TotalSubmitted]   FLOAT (53)       NOT NULL,
    [TotalApproved]    FLOAT (53)       NOT NULL,
    [UploadDate]       DATETIME         NOT NULL,
    [RecordIndex]      INT              IDENTITY (1, 1) NOT NULL,
    [TotalPaid]        INT              CONSTRAINT [DF_ClaimsInReturn_TotalPaid] DEFAULT ((0)) NOT NULL,
    [TotalRejected]    INT              CONSTRAINT [DF_ClaimsInReturn_TotalRejected] DEFAULT ((0)) NOT NULL,
    [ReturnFooter] NVARCHAR(110) NULL, 
    CONSTRAINT [PK_ClaimsInReturn] PRIMARY KEY CLUSTERED ([ClaimsInReturnId] ASC),
    CONSTRAINT [FK_ClaimsInReturn_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
);



