SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

USE [master];
GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = 'MyXml')
                DROP DATABASE MyXml;
GO

-- Create the MyXml database.
CREATE DATABASE MyXml COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

-- Specify a simple recovery model 
-- to keep the log growth to a minimum.
ALTER DATABASE MyXml
                SET RECOVERY SIMPLE;
GO

USE MyXml;
GO

CREATE TABLE [dbo].[Data] (
    [Name] NVARCHAR (255) NOT NULL,
    [ID]     INT         NOT NULL,
    [Entry]  INT         NOT NULL,
    [Pre]    INT         NOT NULL,
    [Post]   INT         NOT NULL,
    [Parent] INT         NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);

CREATE TABLE [dbo].[Attribute] (
    [Element]INT         NOT NULL,
    [Name]   NVARCHAR (255) NOT NULL,
    [Value]   NVARCHAR (255) NOT NULL,
    PRIMARY KEY CLUSTERED ([Element],[Name] ASC)
);

CREATE TABLE [dbo].[Text] (
    [ID]INT         NOT NULL,
    [Value]   NVARCHAR (255) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC)
);