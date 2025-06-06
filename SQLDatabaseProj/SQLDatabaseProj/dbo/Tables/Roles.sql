CREATE TABLE [dbo].[Roles] (
    [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId]        UNIQUEIDENTIFIER NOT NULL,
    [RoleName]      NVARCHAR (256)   NOT NULL,
    [Description]   NVARCHAR (256)   NULL,
    PRIMARY KEY CLUSTERED ([RoleId] ASC),
    CONSTRAINT [RoleApplication] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId])
);

