CREATE TABLE [dbo].[UserProfiles] (
    [UserId]               UNIQUEIDENTIFIER NOT NULL,
    [DoctorNumber]         SMALLINT         NOT NULL,
    [DoctorName]           NVARCHAR (80)    NOT NULL,
    [ClinicNumber]         SMALLINT         NOT NULL,
    [DiagnosticCode]       NVARCHAR (3)     NOT NULL,
    [CorporationIndicator] NVARCHAR (1)     NULL,
    [Street]               NVARCHAR (50)    NULL,
    [City]                 NVARCHAR (50)    NOT NULL,
    [Province]             NVARCHAR (30)    NOT NULL,
    [PostalCode]           NVARCHAR (6)     NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_UserProfile_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
);

