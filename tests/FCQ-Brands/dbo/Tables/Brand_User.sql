CREATE TABLE [dbo].[Brand|User] (
    [Brand] INT NOT NULL,
    [User]  INT NOT NULL,
    CONSTRAINT [PK_Brand|User] PRIMARY KEY CLUSTERED ([Brand] ASC, [User] ASC),
    CONSTRAINT [FK_Brand|User_Brand] FOREIGN KEY ([Brand]) REFERENCES [dbo].[Brand] ([Id]),
    CONSTRAINT [FK_Brand|User_User] FOREIGN KEY ([User]) REFERENCES [dbo].[UserName] ([Id])
);

