-- ============================================================
-- Customer Support Ticket System
-- Microsoft SQL Server (T-SQL) Schema Script
-- Run this in SSMS or Azure Data Studio
-- ============================================================

-- 1. Create & use the database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SupportTicketDB')
BEGIN
    CREATE DATABASE SupportTicketDB;
END
GO

USE SupportTicketDB;
GO

-- ============================================================
-- 2. USERS TABLE
-- ============================================================
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users (
    Id          INT IDENTITY(1,1)   PRIMARY KEY,
    Username    NVARCHAR(100)       NOT NULL UNIQUE,
    Password    NVARCHAR(255)       NOT NULL,   -- BCrypt hash
    FullName    NVARCHAR(200)       NOT NULL,
    Email       NVARCHAR(200)       NOT NULL,
    Role        NVARCHAR(10)        NOT NULL DEFAULT 'User'   -- 'User' or 'Admin'
                CONSTRAINT CK_Users_Role CHECK (Role IN ('User', 'Admin')),
    IsActive    BIT                 NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================================
-- 3. TICKETS TABLE
-- ============================================================
IF OBJECT_ID('dbo.Tickets', 'U') IS NOT NULL DROP TABLE dbo.Tickets;
GO

CREATE TABLE dbo.Tickets (
    Id                  INT IDENTITY(1,1)   PRIMARY KEY,
    TicketNumber        NVARCHAR(20)        NOT NULL UNIQUE,   -- e.g. TKT-00001
    Subject             NVARCHAR(300)       NOT NULL,
    Description         NVARCHAR(MAX)       NOT NULL,
    Priority            NVARCHAR(10)        NOT NULL DEFAULT 'Medium'
                        CONSTRAINT CK_Tickets_Priority CHECK (Priority IN ('Low', 'Medium', 'High')),
    Status              NVARCHAR(15)        NOT NULL DEFAULT 'Open'
                        CONSTRAINT CK_Tickets_Status CHECK (Status IN ('Open', 'In Progress', 'Closed')),
    CreatedByUserId     INT                 NOT NULL,
    AssignedToUserId    INT                 NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Tickets_CreatedBy  FOREIGN KEY (CreatedByUserId)  REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Tickets_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users(Id)
);
GO

-- ============================================================
-- 4. TICKET STATUS HISTORY TABLE
-- ============================================================
IF OBJECT_ID('dbo.TicketStatusHistory', 'U') IS NOT NULL DROP TABLE dbo.TicketStatusHistory;
GO

CREATE TABLE dbo.TicketStatusHistory (
    Id                  INT IDENTITY(1,1)   PRIMARY KEY,
    TicketId            INT                 NOT NULL,
    OldStatus           NVARCHAR(15)        NULL,
    NewStatus           NVARCHAR(15)        NOT NULL,
    ChangedByUserId     INT                 NOT NULL,
    ChangedAt           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    Notes               NVARCHAR(500)       NULL,
    CONSTRAINT FK_History_Ticket FOREIGN KEY (TicketId)        REFERENCES dbo.Tickets(Id),
    CONSTRAINT FK_History_User   FOREIGN KEY (ChangedByUserId) REFERENCES dbo.Users(Id)
);
GO

-- ============================================================
-- 5. TICKET COMMENTS TABLE
-- ============================================================
IF OBJECT_ID('dbo.TicketComments', 'U') IS NOT NULL DROP TABLE dbo.TicketComments;
GO

CREATE TABLE dbo.TicketComments (
    Id              INT IDENTITY(1,1)   PRIMARY KEY,
    TicketId        INT                 NOT NULL,
    AuthorUserId    INT                 NOT NULL,
    CommentText     NVARCHAR(MAX)       NOT NULL,
    IsInternal      BIT                 NOT NULL DEFAULT 0,   -- 1 = admin-only internal note
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Comments_Ticket FOREIGN KEY (TicketId)     REFERENCES dbo.Tickets(Id),
    CONSTRAINT FK_Comments_Author FOREIGN KEY (AuthorUserId) REFERENCES dbo.Users(Id)
);
GO

-- ============================================================
-- 6. INDEXES
-- ============================================================
CREATE INDEX IX_Tickets_Status      ON dbo.Tickets(Status);
CREATE INDEX IX_Tickets_CreatedBy   ON dbo.Tickets(CreatedByUserId);
CREATE INDEX IX_Tickets_AssignedTo  ON dbo.Tickets(AssignedToUserId);
CREATE INDEX IX_History_TicketId    ON dbo.TicketStatusHistory(TicketId);
CREATE INDEX IX_Comments_TicketId   ON dbo.TicketComments(TicketId);
GO

-- ============================================================
-- 7. VERIFY
-- ============================================================
SELECT 'Users'               AS [Table], COUNT(*) AS [Rows] FROM dbo.Users
UNION ALL
SELECT 'Tickets',              COUNT(*) FROM dbo.Tickets
UNION ALL
SELECT 'TicketStatusHistory',  COUNT(*) FROM dbo.TicketStatusHistory
UNION ALL
SELECT 'TicketComments',       COUNT(*) FROM dbo.TicketComments;
GO

PRINT 'SupportTicketDB schema created successfully.';
GO
