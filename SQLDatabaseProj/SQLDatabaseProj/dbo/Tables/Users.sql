CREATE TABLE [dbo].[Users] (
    [ApplicationId]    UNIQUEIDENTIFIER NOT NULL,
    [UserId]           UNIQUEIDENTIFIER NOT NULL,
    [UserName]         NVARCHAR (50)    NOT NULL,
    [IsAnonymous]      BIT              NOT NULL,
    [LastActivityDate] DATETIME         NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [UserApplication] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId])
);


GO
CREATE NONCLUSTERED INDEX [IDX_UserName]
    ON [dbo].[Users]([UserName] ASC);

