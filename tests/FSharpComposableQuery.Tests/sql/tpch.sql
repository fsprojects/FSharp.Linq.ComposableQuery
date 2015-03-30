SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

USE [master];
GO

:setvar DatabaseName "FCQ-TPCH"

IF EXISTS (SELECT * FROM sys.databases WHERE name = [$(DatabaseName)])
                DROP DATABASE [$(DatabaseName)];
GO

CREATE DATABASE [$(DatabaseName)] COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

-- Specify a simple recovery model to keep the log growth to a minimum.
ALTER DATABASE [$(DatabaseName)] SET RECOVERY SIMPLE;
GO

USE [$(DatabaseName)];
GO

CREATE TABLE [dbo].[customer](
    [C_CustKey] [int] NOT NULL,
    [C_Name] [varchar](64) NOT NULL,
    [C_Address] [varchar](64) NOT NULL,
    [C_NationKey] [int] NOT NULL,
    [C_Phone] [varchar](64) NOT NULL,
    [C_AcctBal] [decimal](13, 2) NOT NULL,
    [C_MktSegment] [varchar](64) NOT NULL,
    [C_Comment] [varchar](120) NOT NULL,
    [skip] [varchar](64) NOT NULL
) ON [PRIMARY]
GO
CREATE TABLE [dbo].[lineitem](
    [L_OrderKey] [int] NOT NULL,
    [L_PartKey] [int] NOT NULL,
    [L_SuppKey] [int] NOT NULL,
    [L_LineNumber] [int] NOT NULL,
    [L_Quantity] [int] NOT NULL,
    [L_ExtendedPrice] [decimal](13, 2) NOT NULL,
    [L_Discount] [decimal](13, 2) NOT NULL,
    [L_Tax] [decimal](13, 2) NOT NULL,
    [L_ReturnFlag] [varchar](64) NOT NULL,
    [L_LineStatus] [varchar](64) NOT NULL,
    [L_ShipDate] [datetime] NOT NULL,
    [L_CommitDate] [datetime] NOT NULL,
    [L_ReceiptDate] [datetime] NOT NULL,
    [L_ShipInstruct] [varchar](64) NOT NULL,
    [L_ShipMode] [varchar](64) NOT NULL,
    [L_Comment] [varchar](64) NOT NULL,
    [skip] [varchar](64) NOT NULL
) ON [PRIMARY]
GO
CREATE TABLE [dbo].[nation](
    [N_NationKey] [int] NOT NULL,
    [N_Name] [varchar](64) NOT NULL,
    [N_RegionKey] [int] NOT NULL,
    [N_Comment] [varchar](160) NOT NULL,
    [skip] [varchar](64) NOT NULL
) ON [PRIMARY]
GO
CREATE TABLE [dbo].[orders](
    [O_OrderKey] [int] NOT NULL,
    [O_CustKey] [int] NOT NULL,
    [O_OrderStatus] [varchar](64) NOT NULL,
    [O_TotalPrice] [decimal](13, 2) NOT NULL,
    [O_OrderDate] [datetime] NOT NULL,
    [O_OrderPriority] [varchar](15) NOT NULL,
    [O_Clerk] [varchar](64) NOT NULL,
    [O_ShipPriority] [int] NOT NULL,
    [O_Comment] [varchar](80) NOT NULL,
    [skip] [varchar](64) NOT NULL
) ON [PRIMARY]
GO
CREATE TABLE [dbo].[part](
    [P_PartKey] [int] NOT NULL,
    [P_Name] [varchar](64) NOT NULL,
    [P_Mfgr] [varchar](64) NOT NULL,
    [P_Brand] [varchar](64) NOT NULL,
    [P_Type] [varchar](64) NOT NULL,
    [P_Size] [int] NOT NULL,
    [P_Container] [varchar](64) NOT NULL,
    [P_RetailPrice] [decimal](13, 2) NOT NULL,
    [P_Comment] [varchar](64) NOT NULL,
    [skip] [varchar](64) NOT NULL
) ON [PRIMARY]
GO
CREATE TABLE [dbo].[partsupp](
    [PS_PartKey] [int] NOT NULL,
    [PS_SuppKey] [int] NOT NULL,
    [PS_AvailQty] [int] NOT NULL,
    [PS_SupplyCost] [decimal](13, 2) NOT NULL,
    [PS_Comment] [varchar](200) NOT NULL,
    [skip] [varchar](64) NOT NULL
) ON [PRIMARY]
GO
CREATE TABLE [dbo].[region](
    [R_RegionKey] [int] NOT NULL,
    [R_Name] [varchar](64) NOT NULL,
    [R_Comment] [varchar](160) NOT NULL,
    [skip] [varchar](64) NOT NULL
) ON [PRIMARY]
GO
CREATE TABLE [dbo].[supplier](
    [S_SuppKey] [int] NOT NULL,
    [S_Name] [varchar](64) NOT NULL,
    [S_Address] [varchar](64) NOT NULL,
    [S_NationKey] [int] NOT NULL,
    [S_Phone] [varchar](18) NOT NULL,
    [S_AcctBal] [decimal](13, 2) NOT NULL,
    [S_Comment] [varchar](105) NOT NULL,
    [skip] [varchar](64) NOT NULL
) ON [PRIMARY]
GO