SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

USE [master];
GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = 'organisation')
                DROP DATABASE organisation;
GO

-- Create the MyPeople database.
CREATE DATABASE organisation COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

-- Specify a simple recovery model 
-- to keep the log growth to a minimum.
ALTER DATABASE organisation
                SET RECOVERY SIMPLE;
GO

USE organisation;
GO

CREATE TABLE [dbo].[Departments] (
    [Dpt] NVARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([Dpt] ASC)
);

CREATE TABLE [dbo].[Employees] (
    [Emp] NVARCHAR (50) NOT NULL,
    [Dpt] NVARCHAR (50) NOT NULL,
    [Salary] INT		NOT NULL,
    PRIMARY KEY CLUSTERED ([Dpt],[Emp] ASC)
);

CREATE TABLE [dbo].[Contacts] (
    [Dpt] NVARCHAR (50)		NOT NULL,
    [Contact] NVARCHAR (50) NOT NULL,
    [Client] INT			NOT NULL,
    PRIMARY KEY CLUSTERED ([Dpt],[Contact] ASC)
);

CREATE TABLE [dbo].[Tasks] (
    [Emp] NVARCHAR (50) NOT NULL,
    [Tsk] NVARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([Tsk],[Emp] ASC)
);
