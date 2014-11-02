CREATE TABLE [dbo].[UserName] (
    [Id]       INT           IDENTITY (1, 1) NOT NULL,
    [AuthId]   INT           NOT NULL,
    [UserName] NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_UserName] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserName_UserAuth] FOREIGN KEY ([AuthId]) REFERENCES [dbo].[UserAuth] ([Id]) ON DELETE CASCADE
);






GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_UserName]
    ON [dbo].[UserName]([UserName] ASC);



