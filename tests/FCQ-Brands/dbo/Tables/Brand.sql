CREATE TABLE [dbo].[Brand] (
    [Id]   INT           IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (50) NOT NULL,
    [Link] NVARCHAR (50) NULL,
    CONSTRAINT [PK_Brand_1] PRIMARY KEY CLUSTERED ([Id] ASC)
);

