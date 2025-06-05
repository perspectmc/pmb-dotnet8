CREATE TABLE [dbo].[UnitRecord] (
    [UnitRecordId]    UNIQUEIDENTIFIER CONSTRAINT [DF_UnitRecord_UnitRecordId] DEFAULT (newid()) NOT NULL,
    [ServiceRecordId] UNIQUEIDENTIFIER NOT NULL,
    [UnitCode]        NVARCHAR (4)     NOT NULL,
    [UnitNumber]      INT              NOT NULL,
    [UnitAmount]      FLOAT (53)       NOT NULL,
    [UnitPremiumCode] NCHAR (1)        NOT NULL,
    [RecordIndex]     BIGINT           NOT NULL,
    [ExplainCode]     NVARCHAR (2)     NULL,
    [PaidAmount] FLOAT NOT NULL, 
    CONSTRAINT [PK_UnitRecord_1] PRIMARY KEY CLUSTERED ([UnitRecordId] ASC),
    CONSTRAINT [FK_UnitRecord_ServiceRecord] FOREIGN KEY ([ServiceRecordId]) REFERENCES [dbo].[ServiceRecord] ([ServiceRecordId])
);

