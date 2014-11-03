/*
This script was created by Visual Studio on 2/11/2014 at 9:02 AM.
Run this script on database FCQ-Brands.
This script performs its actions in the following order:
1. Disable foreign-key constraints.
2. Perform DELETE commands. 
3. Perform UPDATE commands.
4. Perform INSERT commands.
5. Re-enable foreign-key constraints.
Please back up your target database before running this script.
*/
SET NUMERIC_ROUNDABORT OFF
GO
SET XACT_ABORT, ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON
GO
/*Pointer used for text / image updates. This might not be needed, but is declared here just in case*/
DECLARE @pv binary(16)
BEGIN TRANSACTION
ALTER TABLE [dbo].[Brand|User] DROP CONSTRAINT [FK_Brand|User_Brand]
ALTER TABLE [dbo].[Brand|User] DROP CONSTRAINT [FK_Brand|User_User]
ALTER TABLE [dbo].[UserName] DROP CONSTRAINT [FK_UserName_UserAuth]
SET IDENTITY_INSERT [dbo].[UserAuth] ON
INSERT INTO [dbo].[UserAuth] ([Id], [UserName], [Email], [PrimaryEmail], [PhoneNumber], [FirstName], [LastName], [DisplayName], [Company], [BirthDate], [BirthDateRaw], [Address], [Address2], [City], [State], [Country], [Culture], [FullName], [Gender], [Language], [MailAddress], [Nickname], [PostalCode], [TimeZone], [Salt], [PasswordHash], [DigestHa1Hash], [Roles], [Permissions], [CreatedDate], [ModifiedDate], [InvalidLoginAttempts], [LastLoginAttempt], [LockedDate], [RecoveryToken], [RefId], [RefIdStr], [Meta]) VALUES (1, 'Aa', NULL, NULL, NULL, 'a', 'A', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'BjlyvQ==', 'liS8rZA9ySLgPw69vKxUGr61Cw89oLLTc+lx+8Y1UWo=', 'c139e5d35882ea7395193de4a12052f7', NULL, NULL, '20141017 09:50:22.547', '20141020 05:11:18.587', 0, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT INTO [dbo].[UserAuth] ([Id], [UserName], [Email], [PrimaryEmail], [PhoneNumber], [FirstName], [LastName], [DisplayName], [Company], [BirthDate], [BirthDateRaw], [Address], [Address2], [City], [State], [Country], [Culture], [FullName], [Gender], [Language], [MailAddress], [Nickname], [PostalCode], [TimeZone], [Salt], [PasswordHash], [DigestHa1Hash], [Roles], [Permissions], [CreatedDate], [ModifiedDate], [InvalidLoginAttempts], [LastLoginAttempt], [LockedDate], [RecoveryToken], [RefId], [RefIdStr], [Meta]) VALUES (2, 'Bb', NULL, NULL, NULL, 'b', 'B', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'BjlyvQ==', 'liS8rZA9ySLgPw69vKxUGr61Cw89oLLTc+lx+8Y1UWo=', 'c139e5d35882ea7395193de4a12052f7', NULL, NULL, '20141017 09:50:22.547', '20141020 05:11:18.587', 0, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT INTO [dbo].[UserAuth] ([Id], [UserName], [Email], [PrimaryEmail], [PhoneNumber], [FirstName], [LastName], [DisplayName], [Company], [BirthDate], [BirthDateRaw], [Address], [Address2], [City], [State], [Country], [Culture], [FullName], [Gender], [Language], [MailAddress], [Nickname], [PostalCode], [TimeZone], [Salt], [PasswordHash], [DigestHa1Hash], [Roles], [Permissions], [CreatedDate], [ModifiedDate], [InvalidLoginAttempts], [LastLoginAttempt], [LockedDate], [RecoveryToken], [RefId], [RefIdStr], [Meta]) VALUES (3, 'Cc', NULL, NULL, NULL, 'c', 'C', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'BjlyvQ==', 'liS8rZA9ySLgPw69vKxUGr61Cw89oLLTc+lx+8Y1UWo=', 'c139e5d35882ea7395193de4a12052f7', NULL, NULL, '20141017 09:50:22.547', '20141020 05:11:18.587', 0, NULL, NULL, NULL, NULL, NULL, NULL)
SET IDENTITY_INSERT [dbo].[UserAuth] OFF
SET IDENTITY_INSERT [dbo].[UserName] ON
INSERT INTO [dbo].[UserName] ([Id], [AuthId], [UserName]) VALUES (1, 1, N'Aa')
INSERT INTO [dbo].[UserName] ([Id], [AuthId], [UserName]) VALUES (2, 2, N'Bb')
INSERT INTO [dbo].[UserName] ([Id], [AuthId], [UserName]) VALUES (3, 3, N'Cc')
SET IDENTITY_INSERT [dbo].[UserName] OFF
SET IDENTITY_INSERT [dbo].[Brand] ON
INSERT INTO [dbo].[Brand] ([Id], [Name], [Link]) VALUES (1, N'Truelex', NULL)
INSERT INTO [dbo].[Brand] ([Id], [Name], [Link]) VALUES (2, N'Zimtam', NULL)
INSERT INTO [dbo].[Brand] ([Id], [Name], [Link]) VALUES (3, N'Icefan', NULL)
INSERT INTO [dbo].[Brand] ([Id], [Name], [Link]) VALUES (4, N'Warecone', NULL)
INSERT INTO [dbo].[Brand] ([Id], [Name], [Link]) VALUES (5, N'Roundware', NULL)
SET IDENTITY_INSERT [dbo].[Brand] OFF
INSERT INTO [dbo].[Brand|User] ([Brand], [User]) VALUES (2, 3)
INSERT INTO [dbo].[Brand|User] ([Brand], [User]) VALUES (2, 2)
ALTER TABLE [dbo].[Brand|User]
    ADD CONSTRAINT [FK_Brand|User_Brand] FOREIGN KEY ([Brand]) REFERENCES [dbo].[Brand] ([Id])
ALTER TABLE [dbo].[Brand|User]
    ADD CONSTRAINT [FK_Brand|User_User] FOREIGN KEY ([User]) REFERENCES [dbo].[UserName] ([Id])
ALTER TABLE [dbo].[UserName]
    ADD CONSTRAINT [FK_UserName_UserAuth] FOREIGN KEY ([AuthId]) REFERENCES [dbo].[UserAuth] ([Id]) ON DELETE CASCADE
COMMIT TRANSACTION
