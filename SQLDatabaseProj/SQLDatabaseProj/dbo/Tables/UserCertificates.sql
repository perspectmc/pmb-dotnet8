CREATE TABLE [dbo].[UserCertificates] (
    [UserId]             UNIQUEIDENTIFIER NOT NULL,
    [CertificateEncoded] NVARCHAR (MAX)   NOT NULL,
    [CertificatePassKey] NVARCHAR (100)   NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_UserCertificates_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
);

