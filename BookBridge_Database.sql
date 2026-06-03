-- ============================================================
--  BookBridge Database Script
--  Compatible: SQL Server 2019/2022, Azure SQL, LocalDB
--  .NET 10 / EF Core 10
--  Run this in SSMS or sqlcmd to create the full database
-- ============================================================

USE master;
GO

-- Drop and recreate
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'BookBridgeDb')
BEGIN
    ALTER DATABASE BookBridgeDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BookBridgeDb;
END
GO

CREATE DATABASE BookBridgeDb
    COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

USE BookBridgeDb;
GO

-- ─────────────────────────────────────────────
-- 1. ASP.NET Identity Tables
-- ─────────────────────────────────────────────

CREATE TABLE [AspNetRoles] (
    [Id]               NVARCHAR(450)  NOT NULL,
    [Name]             NVARCHAR(256)  NULL,
    [NormalizedName]   NVARCHAR(256)  NULL,
    [ConcurrencyStamp] NVARCHAR(MAX)  NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE TABLE [AspNetUsers] (
    [Id]                   NVARCHAR(450)    NOT NULL,
    [FullName]             NVARCHAR(200)    NOT NULL DEFAULT '',
    [ProfilePicture]       NVARCHAR(500)    NULL,
    [Address]              NVARCHAR(500)    NULL,
    [City]                 NVARCHAR(100)    NULL,
    [CNIC]                 NVARCHAR(20)     NULL,
    [IsVerified]           BIT              NOT NULL DEFAULT 0,
    [IsBanned]             BIT              NOT NULL DEFAULT 0,
    [BanReason]            NVARCHAR(500)    NULL,
    [CreatedAt]            DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [LastActive]           DATETIME2        NULL,
    [WalletBalance]        DECIMAL(18,2)    NOT NULL DEFAULT 0,
    [AverageRating]        FLOAT            NOT NULL DEFAULT 0,
    [TotalRatings]         INT              NOT NULL DEFAULT 0,
    -- Identity columns
    [UserName]             NVARCHAR(256)    NULL,
    [NormalizedUserName]   NVARCHAR(256)    NULL,
    [Email]                NVARCHAR(256)    NULL,
    [NormalizedEmail]      NVARCHAR(256)    NULL,
    [EmailConfirmed]       BIT              NOT NULL DEFAULT 0,
    [PasswordHash]         NVARCHAR(MAX)    NULL,
    [SecurityStamp]        NVARCHAR(MAX)    NULL,
    [ConcurrencyStamp]     NVARCHAR(MAX)    NULL,
    [PhoneNumber]          NVARCHAR(50)     NULL,
    [PhoneNumberConfirmed] BIT              NOT NULL DEFAULT 0,
    [TwoFactorEnabled]     BIT              NOT NULL DEFAULT 0,
    [LockoutEnd]           DATETIMEOFFSET   NULL,
    [LockoutEnabled]       BIT              NOT NULL DEFAULT 1,
    [AccessFailedCount]    INT              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [EmailIndex]    ON [AspNetUsers] ([NormalizedEmail]);
CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] NVARCHAR(450) NOT NULL,
    [RoleId] NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId],[RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id]         INT           IDENTITY(1,1) NOT NULL,
    [UserId]     NVARCHAR(450) NOT NULL,
    [ClaimType]  NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_Users] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider]       NVARCHAR(128) NOT NULL,
    [ProviderKey]         NVARCHAR(128) NOT NULL,
    [ProviderDisplayName] NVARCHAR(MAX) NULL,
    [UserId]              NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider],[ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_Users] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId]        NVARCHAR(450) NOT NULL,
    [LoginProvider] NVARCHAR(128) NOT NULL,
    [Name]          NVARCHAR(128) NOT NULL,
    [Value]         NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId],[LoginProvider],[Name]),
    CONSTRAINT [FK_AspNetUserTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id]         INT           IDENTITY(1,1) NOT NULL,
    [RoleId]     NVARCHAR(450) NOT NULL,
    [ClaimType]  NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_Roles] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
);
GO

-- ─────────────────────────────────────────────
-- 2. Categories
-- ─────────────────────────────────────────────

CREATE TABLE [Categories] (
    [Id]          INT           IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [Icon]        NVARCHAR(100) NULL,
    [BookCount]   INT           NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);
GO

-- ─────────────────────────────────────────────
-- 3. Books
-- ─────────────────────────────────────────────

CREATE TABLE [Books] (
    [Id]                INT            IDENTITY(1,1) NOT NULL,
    [Title]             NVARCHAR(300)  NOT NULL,
    [Author]            NVARCHAR(200)  NOT NULL,
    [ISBN]              NVARCHAR(20)   NULL,
    [Description]       NVARCHAR(MAX)  NULL,
    [Publisher]         NVARCHAR(200)  NULL,
    [PublishedYear]     INT            NULL,
    [Language]          NVARCHAR(50)   NOT NULL DEFAULT 'English',
    [Pages]             INT            NULL,
    -- Enums stored as TINYINT: BookCondition (0=New,1=LikeNew,2=Good,3=Fair,4=Poor)
    [Condition]         TINYINT        NOT NULL DEFAULT 0,
    -- TransactionType (0=Sell,1=Donate,2=Borrow,3=Exchange)
    [TransactionType]   TINYINT        NOT NULL DEFAULT 0,
    -- BookStatus (0=Available,1=Borrowed,2=Sold,3=Donated,4=Exchanged,5=Pending)
    [Status]            TINYINT        NOT NULL DEFAULT 0,
    [Price]             DECIMAL(18,2)  NULL,
    [DepositAmount]     DECIMAL(18,2)  NULL,
    [BorrowDurationDays] INT           NULL,
    [Location]          NVARCHAR(200)  NULL,
    [City]              NVARCHAR(100)  NULL,
    [IsFeatured]        BIT            NOT NULL DEFAULT 0,
    [ViewCount]         INT            NOT NULL DEFAULT 0,
    [CreatedAt]         DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]         DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    [OwnerId]           NVARCHAR(450)  NOT NULL,
    [CategoryId]        INT            NOT NULL,
    CONSTRAINT [PK_Books] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Books_Owner]    FOREIGN KEY ([OwnerId])    REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Books_Category] FOREIGN KEY ([CategoryId]) REFERENCES [Categories]([Id])
);
GO

CREATE INDEX [IX_Books_Status]          ON [Books] ([Status]);
CREATE INDEX [IX_Books_TransactionType] ON [Books] ([TransactionType]);
CREATE INDEX [IX_Books_CategoryId]      ON [Books] ([CategoryId]);
CREATE INDEX [IX_Books_OwnerId]         ON [Books] ([OwnerId]);
CREATE INDEX [IX_Books_IsFeatured]      ON [Books] ([IsFeatured]);
CREATE INDEX [IX_Books_CreatedAt]       ON [Books] ([CreatedAt] DESC);
GO

-- ─────────────────────────────────────────────
-- 4. Book Images
-- ─────────────────────────────────────────────

CREATE TABLE [BookImages] (
    [Id]        INT           IDENTITY(1,1) NOT NULL,
    [ImagePath] NVARCHAR(500) NOT NULL,
    [IsPrimary] BIT           NOT NULL DEFAULT 0,
    [BookId]    INT           NOT NULL,
    CONSTRAINT [PK_BookImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BookImages_Book] FOREIGN KEY ([BookId]) REFERENCES [Books]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_BookImages_BookId] ON [BookImages] ([BookId]);
GO

-- ─────────────────────────────────────────────
-- 5. Borrow Requests
-- ─────────────────────────────────────────────

CREATE TABLE [BorrowRequests] (
    [Id]               INT           IDENTITY(1,1) NOT NULL,
    [RequestedAt]      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [ApprovedAt]       DATETIME2     NULL,
    [BorrowedAt]       DATETIME2     NULL,
    [DueDate]          DATETIME2     NULL,
    [ReturnedAt]       DATETIME2     NULL,
    -- RequestStatus (0=Pending,1=Approved,2=Rejected,3=Cancelled,4=Completed)
    [Status]           TINYINT       NOT NULL DEFAULT 0,
    [BorrowDays]       INT           NOT NULL DEFAULT 7,
    [Message]          NVARCHAR(500) NULL,
    [RejectionReason]  NVARCHAR(500) NULL,
    [IsLate]           BIT           NOT NULL DEFAULT 0,
    [BorrowerId]       NVARCHAR(450) NOT NULL,
    [OwnerId]          NVARCHAR(450) NOT NULL,
    [BookId]           INT           NOT NULL,
    CONSTRAINT [PK_BorrowRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BorrowRequests_Borrower] FOREIGN KEY ([BorrowerId]) REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_BorrowRequests_Owner]    FOREIGN KEY ([OwnerId])    REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_BorrowRequests_Book]     FOREIGN KEY ([BookId])     REFERENCES [Books]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_BorrowRequests_BorrowerId] ON [BorrowRequests] ([BorrowerId]);
CREATE INDEX [IX_BorrowRequests_OwnerId]    ON [BorrowRequests] ([OwnerId]);
CREATE INDEX [IX_BorrowRequests_BookId]     ON [BorrowRequests] ([BookId]);
CREATE INDEX [IX_BorrowRequests_Status]     ON [BorrowRequests] ([Status]);
GO

-- ─────────────────────────────────────────────
-- 6. Exchange Requests
-- ─────────────────────────────────────────────

CREATE TABLE [ExchangeRequests] (
    [Id]               INT           IDENTITY(1,1) NOT NULL,
    [Status]           TINYINT       NOT NULL DEFAULT 0,
    [Message]          NVARCHAR(500) NULL,
    [CreatedAt]        DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [RequesterId]      NVARCHAR(450) NOT NULL,
    [RequestedBookId]  INT           NOT NULL,
    [OfferedBookId]    INT           NULL,
    CONSTRAINT [PK_ExchangeRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExchangeRequests_Requester]     FOREIGN KEY ([RequesterId])     REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_ExchangeRequests_RequestedBook] FOREIGN KEY ([RequestedBookId]) REFERENCES [Books]([Id]),
    CONSTRAINT [FK_ExchangeRequests_OfferedBook]   FOREIGN KEY ([OfferedBookId])   REFERENCES [Books]([Id])
);
GO

-- ─────────────────────────────────────────────
-- 7. Transactions
-- ─────────────────────────────────────────────

CREATE TABLE [Transactions] (
    [Id]                    INT            IDENTITY(1,1) NOT NULL,
    -- TransactionType (0=Sell,1=Donate,2=Borrow,3=Exchange)
    [Type]                  TINYINT        NOT NULL DEFAULT 0,
    -- TransactionStatus (0=Pending,1=Completed,2=Refunded,3=Failed)
    [Status]                TINYINT        NOT NULL DEFAULT 0,
    [Amount]                DECIMAL(18,2)  NOT NULL DEFAULT 0,
    [StripePaymentIntentId] NVARCHAR(200)  NULL,
    [PaymentMethod]         NVARCHAR(100)  NULL,
    [CreatedAt]             DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    [CompletedAt]           DATETIME2      NULL,
    [Notes]                 NVARCHAR(500)  NULL,
    [BuyerId]               NVARCHAR(450)  NOT NULL,
    [SellerId]              NVARCHAR(450)  NOT NULL,
    [BookId]                INT            NOT NULL,
    CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Transactions_Buyer]  FOREIGN KEY ([BuyerId])  REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_Transactions_Seller] FOREIGN KEY ([SellerId]) REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_Transactions_Book]   FOREIGN KEY ([BookId])   REFERENCES [Books]([Id])
);
GO

CREATE INDEX [IX_Transactions_BuyerId]  ON [Transactions] ([BuyerId]);
CREATE INDEX [IX_Transactions_SellerId] ON [Transactions] ([SellerId]);
CREATE INDEX [IX_Transactions_BookId]   ON [Transactions] ([BookId]);
GO

-- ─────────────────────────────────────────────
-- 8. Reviews
-- ─────────────────────────────────────────────

CREATE TABLE [Reviews] (
    [Id]             INT           IDENTITY(1,1) NOT NULL,
    [Rating]         TINYINT       NOT NULL DEFAULT 5,
    [Comment]        NVARCHAR(MAX) NULL,
    [CreatedAt]      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [ReviewerId]     NVARCHAR(450) NOT NULL,
    [ReviewedUserId] NVARCHAR(450) NOT NULL,
    [BookId]         INT           NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reviews_Reviewer]     FOREIGN KEY ([ReviewerId])     REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_Reviews_ReviewedUser] FOREIGN KEY ([ReviewedUserId]) REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_Reviews_Book]         FOREIGN KEY ([BookId])         REFERENCES [Books]([Id]),
    CONSTRAINT [CK_Reviews_Rating]       CHECK ([Rating] BETWEEN 1 AND 5)
);
GO

CREATE INDEX [IX_Reviews_ReviewedUserId] ON [Reviews] ([ReviewedUserId]);
CREATE INDEX [IX_Reviews_ReviewerId]     ON [Reviews] ([ReviewerId]);
GO

-- ─────────────────────────────────────────────
-- 9. Conversations (Chat)
-- ─────────────────────────────────────────────

CREATE TABLE [Conversations] (
    [Id]            INT           IDENTITY(1,1) NOT NULL,
    [CreatedAt]     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [LastMessageAt] DATETIME2     NULL,
    [User1Id]       NVARCHAR(450) NOT NULL,
    [User2Id]       NVARCHAR(450) NOT NULL,
    [BookId]        INT           NULL,
    CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Conversations_User1] FOREIGN KEY ([User1Id]) REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_Conversations_User2] FOREIGN KEY ([User2Id]) REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_Conversations_Book]  FOREIGN KEY ([BookId])  REFERENCES [Books]([Id])
);
GO

CREATE INDEX [IX_Conversations_User1Id] ON [Conversations] ([User1Id]);
CREATE INDEX [IX_Conversations_User2Id] ON [Conversations] ([User2Id]);
GO

-- ─────────────────────────────────────────────
-- 10. Messages
-- ─────────────────────────────────────────────

CREATE TABLE [Messages] (
    [Id]             INT           IDENTITY(1,1) NOT NULL,
    [Content]        NVARCHAR(MAX) NOT NULL DEFAULT '',
    [ImagePath]      NVARCHAR(500) NULL,
    [IsRead]         BIT           NOT NULL DEFAULT 0,
    [SentAt]         DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [SenderId]       NVARCHAR(450) NOT NULL,
    [ConversationId] INT           NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_Sender]       FOREIGN KEY ([SenderId])       REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_Messages_Conversation] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Messages_ConversationId] ON [Messages] ([ConversationId]);
CREATE INDEX [IX_Messages_SenderId]       ON [Messages] ([SenderId]);
CREATE INDEX [IX_Messages_SentAt]         ON [Messages] ([SentAt] DESC);
GO

-- ─────────────────────────────────────────────
-- 11. Notifications
-- ─────────────────────────────────────────────

CREATE TABLE [Notifications] (
    [Id]        INT           IDENTITY(1,1) NOT NULL,
    [Title]     NVARCHAR(200) NOT NULL,
    [Body]      NVARCHAR(500) NOT NULL,
    [Link]      NVARCHAR(300) NULL,
    [Type]      NVARCHAR(30)  NOT NULL DEFAULT 'info',
    [IsRead]    BIT           NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [UserId]    NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_User] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
CREATE INDEX [IX_Notifications_IsRead] ON [Notifications] ([UserId], [IsRead]);
GO

-- ─────────────────────────────────────────────
-- 12. Support Tickets
-- ─────────────────────────────────────────────

CREATE TABLE [SupportTickets] (
    [Id]          INT           IDENTITY(1,1) NOT NULL,
    [Subject]     NVARCHAR(300) NOT NULL,
    [Description] NVARCHAR(MAX) NOT NULL,
    [Status]      NVARCHAR(20)  NOT NULL DEFAULT 'Open',
    [Priority]    NVARCHAR(20)  NOT NULL DEFAULT 'Normal',
    [CreatedAt]   DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [ResolvedAt]  DATETIME2     NULL,
    [AdminReply]  NVARCHAR(MAX) NULL,
    [UserId]      NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupportTickets_User] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_SupportTickets_UserId] ON [SupportTickets] ([UserId]);
CREATE INDEX [IX_SupportTickets_Status] ON [SupportTickets] ([Status]);
GO

-- ─────────────────────────────────────────────
-- 13. Reports
-- ─────────────────────────────────────────────

CREATE TABLE [Reports] (
    [Id]             INT           IDENTITY(1,1) NOT NULL,
    [Reason]         NVARCHAR(200) NOT NULL,
    [Description]    NVARCHAR(MAX) NULL,
    [Status]         NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
    [CreatedAt]      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [ReporterId]     NVARCHAR(450) NOT NULL,
    [ReportedUserId] NVARCHAR(450) NULL,
    [ReportedBookId] INT           NULL,
    CONSTRAINT [PK_Reports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reports_Reporter]     FOREIGN KEY ([ReporterId])     REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_Reports_ReportedUser] FOREIGN KEY ([ReportedUserId]) REFERENCES [AspNetUsers]([Id]),
    CONSTRAINT [FK_Reports_ReportedBook] FOREIGN KEY ([ReportedBookId]) REFERENCES [Books]([Id])
);
GO

CREATE INDEX [IX_Reports_Status]     ON [Reports] ([Status]);
CREATE INDEX [IX_Reports_ReporterId] ON [Reports] ([ReporterId]);
GO

-- ─────────────────────────────────────────────
-- 14. EF Core Migrations History Table
-- ─────────────────────────────────────────────

CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId]    NVARCHAR(150) NOT NULL,
    [ProductVersion] NVARCHAR(32)  NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);
GO

INSERT INTO [__EFMigrationsHistory] VALUES ('20250101000000_InitialCreate', '10.0.0');
GO

-- ─────────────────────────────────────────────
-- 15. Seed Data
-- ─────────────────────────────────────────────

-- Roles
INSERT INTO [AspNetRoles] ([Id],[Name],[NormalizedName],[ConcurrencyStamp]) VALUES
(NEWID(), 'Admin',    'ADMIN',    NEWID()),
(NEWID(), 'User',     'USER',     NEWID()),
(NEWID(), 'Seller',   'SELLER',   NEWID()),
(NEWID(), 'Buyer',    'BUYER',    NEWID()),
(NEWID(), 'Donor',    'DONOR',    NEWID()),
(NEWID(), 'Borrower', 'BORROWER', NEWID());
GO

-- Categories (10 pre-seeded)
INSERT INTO [Categories] ([Name],[Description],[Icon]) VALUES
('Fiction',               'Novels, stories, and literary fiction',       'bi-book'),
('Non-Fiction',           'Biographies, self-help, and real-world books','bi-journal-text'),
('Science & Technology',  'Computing, engineering, and sciences',         'bi-cpu'),
('Business',              'Finance, marketing, and entrepreneurship',     'bi-briefcase'),
('Academic',              'Textbooks and study materials',                'bi-mortarboard'),
('Children',              'Books for kids and young readers',             'bi-star'),
('History',               'Historical events and civilizations',          'bi-hourglass'),
('Religion & Philosophy', 'Spiritual and philosophical works',            'bi-heart'),
('Arts & Design',         'Art, architecture, and creative design',       'bi-palette'),
('Health & Wellness',     'Medicine, fitness, and mental health',         'bi-heart-pulse');
GO

-- Admin User
DECLARE @AdminId NVARCHAR(450) = NEWID();
DECLARE @AdminRoleId NVARCHAR(450);
DECLARE @UserRoleId  NVARCHAR(450);

SELECT @AdminRoleId = [Id] FROM [AspNetRoles] WHERE [NormalizedName] = 'ADMIN';
SELECT @UserRoleId  = [Id] FROM [AspNetRoles] WHERE [NormalizedName] = 'USER';

INSERT INTO [AspNetUsers] (
    [Id],[FullName],[City],[IsVerified],[EmailConfirmed],[WalletBalance],
    [UserName],[NormalizedUserName],[Email],[NormalizedEmail],
    [PasswordHash],[SecurityStamp],[ConcurrencyStamp],
    [LockoutEnabled],[AccessFailedCount],[CreatedAt]
) VALUES (
    @AdminId,
    'BookBridge Admin',
    'Lahore',
    1, 1, 0,
    'admin@bookbridge.com',
    'ADMIN@BOOKBRIDGE.COM',
    'admin@bookbridge.com',
    'ADMIN@BOOKBRIDGE.COM',
    'AQAAAAIAAYagAAAAEPlaceholderHashLetAppSeedThisProperlyOnFirstRun==',
    NEWID(), NEWID(),
    1, 0,
    GETUTCDATE()
);

INSERT INTO [AspNetUserRoles] ([UserId],[RoleId]) VALUES (@AdminId, @AdminRoleId);
INSERT INTO [AspNetUserRoles] ([UserId],[RoleId]) VALUES (@AdminId, @UserRoleId);
GO

-- Sample User 1
DECLARE @User1Id NVARCHAR(450) = NEWID();
DECLARE @UserRoleId2 NVARCHAR(450);
SELECT @UserRoleId2 = [Id] FROM [AspNetRoles] WHERE [NormalizedName] = 'USER';

INSERT INTO [AspNetUsers] (
    [Id],[FullName],[City],[IsVerified],[EmailConfirmed],[WalletBalance],[AverageRating],[TotalRatings],
    [UserName],[NormalizedUserName],[Email],[NormalizedEmail],
    [PasswordHash],[SecurityStamp],[ConcurrencyStamp],
    [LockoutEnabled],[AccessFailedCount],[CreatedAt]
) VALUES (
    @User1Id,
    'Ahmed Khan', 'Lahore', 1, 1, 500.00, 4.5, 10,
    'ahmed@example.com','AHMED@EXAMPLE.COM',
    'ahmed@example.com','AHMED@EXAMPLE.COM',
    'AQAAAAIAAYagAAAAEPlaceholder==',
    NEWID(), NEWID(), 1, 0, GETUTCDATE()
);
INSERT INTO [AspNetUserRoles] ([UserId],[RoleId]) VALUES (@User1Id, @UserRoleId2);

-- Sample User 2
DECLARE @User2Id NVARCHAR(450) = NEWID();
INSERT INTO [AspNetUsers] (
    [Id],[FullName],[City],[IsVerified],[EmailConfirmed],[WalletBalance],[AverageRating],[TotalRatings],
    [UserName],[NormalizedUserName],[Email],[NormalizedEmail],
    [PasswordHash],[SecurityStamp],[ConcurrencyStamp],
    [LockoutEnabled],[AccessFailedCount],[CreatedAt]
) VALUES (
    @User2Id,
    'Sara Malik', 'Karachi', 1, 1, 250.00, 5.0, 5,
    'sara@example.com','SARA@EXAMPLE.COM',
    'sara@example.com','SARA@EXAMPLE.COM',
    'AQAAAAIAAYagAAAAEPlaceholder==',
    NEWID(), NEWID(), 1, 0, GETUTCDATE()
);
INSERT INTO [AspNetUserRoles] ([UserId],[RoleId]) VALUES (@User2Id, @UserRoleId2);

-- Sample Books
DECLARE @FictionCatId INT;
DECLARE @AcademicCatId INT;
DECLARE @BusinessCatId INT;
SELECT @FictionCatId  = [Id] FROM [Categories] WHERE [Name] = 'Fiction';
SELECT @AcademicCatId = [Id] FROM [Categories] WHERE [Name] = 'Academic';
SELECT @BusinessCatId = [Id] FROM [Categories] WHERE [Name] = 'Business';

-- Book 1 (For Sale)
INSERT INTO [Books] ([Title],[Author],[Description],[Language],[Condition],[TransactionType],[Status],[Price],[City],[IsFeatured],[OwnerId],[CategoryId],[CreatedAt],[UpdatedAt])
VALUES ('The Alchemist','Paulo Coelho','A classic novel about following your dreams.',
        'English', 1, 0, 0, 350.00, 'Lahore', 1, @User1Id, @FictionCatId, GETUTCDATE(), GETUTCDATE());

-- Book 2 (Borrow)
INSERT INTO [Books] ([Title],[Author],[Description],[Language],[Condition],[TransactionType],[Status],[BorrowDurationDays],[DepositAmount],[City],[IsFeatured],[OwnerId],[CategoryId],[CreatedAt],[UpdatedAt])
VALUES ('Atomic Habits','James Clear','Build good habits and break bad ones.',
        'English', 0, 2, 0, 14, 200.00, 'Lahore', 1, @User1Id, @BusinessCatId, GETUTCDATE(), GETUTCDATE());

-- Book 3 (Donate)
INSERT INTO [Books] ([Title],[Author],[Description],[Language],[Condition],[TransactionType],[Status],[City],[OwnerId],[CategoryId],[CreatedAt],[UpdatedAt])
VALUES ('Introduction to Python','Mark Lutz','Comprehensive Python programming guide.',
        'English', 2, 1, 0, 'Karachi', @User2Id, @AcademicCatId, GETUTCDATE(), GETUTCDATE());

-- Book 4 (Exchange)
INSERT INTO [Books] ([Title],[Author],[Description],[Language],[Condition],[TransactionType],[Status],[City],[OwnerId],[CategoryId],[CreatedAt],[UpdatedAt])
VALUES ('Rich Dad Poor Dad','Robert Kiyosaki','Financial literacy for everyone.',
        'English', 1, 3, 0, 'Karachi', @User2Id, @BusinessCatId, GETUTCDATE(), GETUTCDATE());

-- Book 5 (For Sale)
INSERT INTO [Books] ([Title],[Author],[Description],[Language],[Condition],[TransactionType],[Status],[Price],[City],[OwnerId],[CategoryId],[CreatedAt],[UpdatedAt])
VALUES ('1984','George Orwell','A dystopian masterpiece about surveillance and freedom.',
        'English', 1, 0, 0, 450.00, 'Islamabad', @User1Id, @FictionCatId, GETUTCDATE(), GETUTCDATE());
GO

-- Sample Review
DECLARE @Reviewer NVARCHAR(450);
DECLARE @Reviewed NVARCHAR(450);
SELECT TOP 1 @Reviewer = [Id] FROM [AspNetUsers] WHERE [Email] = 'ahmed@example.com';
SELECT TOP 1 @Reviewed = [Id] FROM [AspNetUsers] WHERE [Email] = 'sara@example.com';

INSERT INTO [Reviews] ([Rating],[Comment],[ReviewerId],[ReviewedUserId],[CreatedAt])
VALUES (5, 'Excellent seller! Book was in perfect condition and delivered quickly. Highly recommended!',
        @Reviewer, @Reviewed, GETUTCDATE());
GO

-- Sample Notification
DECLARE @AdminUserId NVARCHAR(450);
SELECT TOP 1 @AdminUserId = [Id] FROM [AspNetUsers] WHERE [Email] = 'admin@bookbridge.com';
INSERT INTO [Notifications] ([Title],[Body],[Type],[UserId],[CreatedAt])
VALUES ('Welcome to BookBridge!',
        'Your account is ready. Start browsing or list your first book.',
        'success', @AdminUserId, GETUTCDATE());
GO
