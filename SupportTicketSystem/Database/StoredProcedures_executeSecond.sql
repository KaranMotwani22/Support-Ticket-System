-- ============================================================
-- Customer Support Ticket System
-- Stored Procedures Script
-- Run this AFTER schema_sqlserver.sql
-- ============================================================

USE SupportTicketDB;
GO

-- ============================================================
-- SP: GetUserByUsername
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetUserByUsername
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, Password, FullName, Email, Role, IsActive, CreatedAt
    FROM dbo.Users
    WHERE Username = @Username AND IsActive = 1;
END
GO

-- ============================================================
-- SP: GetUserCount  (used by seeder)
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetUserCount
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.Users;
END
GO

-- ============================================================
-- SP: InsertUser  (used by seeder)
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_InsertUser
    @Username NVARCHAR(100),
    @Password NVARCHAR(255),
    @FullName NVARCHAR(200),
    @Email    NVARCHAR(200),
    @Role     NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Users (Username, Password, FullName, Email, Role)
    VALUES (@Username, @Password, @FullName, @Email, @Role);
END
GO

-- ============================================================
-- SP: GetTickets  (role-based list)
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetTickets
    @UserId  INT,
    @IsAdmin BIT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        t.Id,
        t.TicketNumber,
        t.Subject,
        t.Priority,
        t.Status,
        t.CreatedAt,
        cu.FullName AS CreatedByName,
        au.FullName AS AssignedToName
    FROM dbo.Tickets t
    INNER JOIN dbo.Users cu ON cu.Id = t.CreatedByUserId
    LEFT  JOIN dbo.Users au ON au.Id = t.AssignedToUserId
    WHERE @IsAdmin = 1 OR t.CreatedByUserId = @UserId
    ORDER BY t.CreatedAt DESC;
END
GO

-- ============================================================
-- SP: GetTicketById
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetTicketById
    @TicketId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        t.Id,
        t.TicketNumber,
        t.Subject,
        t.Description,
        t.Priority,
        t.Status,
        t.CreatedAt,
        t.CreatedByUserId,
        t.AssignedToUserId,
        cu.FullName AS CreatedByName,
        au.FullName AS AssignedToName
    FROM dbo.Tickets t
    INNER JOIN dbo.Users cu ON cu.Id = t.CreatedByUserId
    LEFT  JOIN dbo.Users au ON au.Id = t.AssignedToUserId
    WHERE t.Id = @TicketId;
END
GO

-- ============================================================
-- SP: GetTicketHistory
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetTicketHistory
    @TicketId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        h.OldStatus,
        h.NewStatus,
        u.FullName AS ChangedByName,
        h.ChangedAt,
        h.Notes
    FROM dbo.TicketStatusHistory h
    INNER JOIN dbo.Users u ON u.Id = h.ChangedByUserId
    WHERE h.TicketId = @TicketId
    ORDER BY h.ChangedAt ASC;
END
GO

-- ============================================================
-- SP: GetTicketComments
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetTicketComments
    @TicketId INT,
    @IsAdmin  BIT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        c.Id,
        u.FullName  AS AuthorName,
        u.Role      AS AuthorRole,
        c.CommentText,
        c.IsInternal,
        c.CreatedAt
    FROM dbo.TicketComments c
    INNER JOIN dbo.Users u ON u.Id = c.AuthorUserId
    WHERE c.TicketId = @TicketId
      AND (@IsAdmin = 1 OR c.IsInternal = 0)
    ORDER BY c.CreatedAt ASC;
END
GO

-- ============================================================
-- SP: GetTicketCount  (for ticket number generation)
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetTicketCount
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.Tickets;
END
GO

-- ============================================================
-- SP: CreateTicket
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_CreateTicket
    @TicketNumber    NVARCHAR(20),
    @Subject         NVARCHAR(300),
    @Description     NVARCHAR(MAX),
    @Priority        NVARCHAR(10),
    @CreatedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Tickets
        (TicketNumber, Subject, Description, Priority, Status, CreatedByUserId, CreatedAt, UpdatedAt)
    VALUES
        (@TicketNumber, @Subject, @Description, @Priority, 'Open', @CreatedByUserId, GETUTCDATE(), GETUTCDATE());

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewId;
END
GO

-- ============================================================
-- SP: InsertTicketHistory
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_InsertTicketHistory
    @TicketId        INT,
    @OldStatus       NVARCHAR(15),
    @NewStatus       NVARCHAR(15),
    @ChangedByUserId INT,
    @Notes           NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TicketStatusHistory
        (TicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Notes)
    VALUES
        (@TicketId, @OldStatus, @NewStatus, @ChangedByUserId, GETUTCDATE(), @Notes);
END
GO

-- ============================================================
-- SP: AssignTicket
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_AssignTicket
    @TicketId        INT,
    @AssignedToUserId INT,      -- pass NULL to unassign
    @AdminUserId     INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatus NVARCHAR(15);
    SELECT @CurrentStatus = Status FROM dbo.Tickets WHERE Id = @TicketId;

    IF @CurrentStatus IS NULL
    BEGIN
        SELECT 0 AS Success, 'Ticket not found.' AS ErrorMessage; RETURN;
    END

    IF @CurrentStatus = 'Closed'
    BEGIN
        SELECT 0 AS Success, 'Cannot modify a closed ticket.' AS ErrorMessage; RETURN;
    END

    UPDATE dbo.Tickets
    SET AssignedToUserId = @AssignedToUserId,
        UpdatedAt        = GETUTCDATE()
    WHERE Id = @TicketId;

    DECLARE @Note NVARCHAR(500) =
        CASE WHEN @AssignedToUserId IS NULL
             THEN 'Ticket unassigned'
             ELSE CONCAT('Ticket assigned to user #', @AssignedToUserId)
        END;

    INSERT INTO dbo.TicketStatusHistory
        (TicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Notes)
    VALUES
        (@TicketId, @CurrentStatus, @CurrentStatus, @AdminUserId, GETUTCDATE(), @Note);

    SELECT 1 AS Success, NULL AS ErrorMessage;
END
GO

-- ============================================================
-- SP: UpdateTicketStatus
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_UpdateTicketStatus
    @TicketId    INT,
    @NewStatus   NVARCHAR(15),
    @AdminUserId INT,
    @Notes       NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatus NVARCHAR(15);
    SELECT @CurrentStatus = Status FROM dbo.Tickets WHERE Id = @TicketId;

    IF @CurrentStatus IS NULL
    BEGIN
        SELECT 0 AS Success, 'Ticket not found.' AS ErrorMessage; RETURN;
    END

    IF @CurrentStatus = 'Closed'
    BEGIN
        SELECT 0 AS Success, 'Closed tickets cannot be modified.' AS ErrorMessage; RETURN;
    END

    -- Enforce flow: Open → In Progress → Closed only
    IF (@CurrentStatus = 'Open'        AND @NewStatus <> 'In Progress') OR
       (@CurrentStatus = 'In Progress' AND @NewStatus <> 'Closed')
    BEGIN
        SELECT 0 AS Success,
               CONCAT('Cannot transition from ''', @CurrentStatus, ''' to ''', @NewStatus, '''.') AS ErrorMessage;
        RETURN;
    END

    UPDATE dbo.Tickets
    SET Status    = @NewStatus,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @TicketId;

    INSERT INTO dbo.TicketStatusHistory
        (TicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Notes)
    VALUES
        (@TicketId, @CurrentStatus, @NewStatus, @AdminUserId, GETUTCDATE(), @Notes);

    SELECT 1 AS Success, NULL AS ErrorMessage;
END
GO

-- ============================================================
-- SP: AddTicketComment
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_AddTicketComment
    @TicketId    INT,
    @AuthorId    INT,
    @CommentText NVARCHAR(MAX),
    @IsInternal  BIT,
    @IsAdmin     BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatus  NVARCHAR(15);
    DECLARE @CreatedByUserId INT;

    SELECT @CurrentStatus   = Status,
           @CreatedByUserId = CreatedByUserId
    FROM dbo.Tickets WHERE Id = @TicketId;

    IF @CurrentStatus IS NULL
    BEGIN
        SELECT 0 AS Success, 'Ticket not found.' AS ErrorMessage; RETURN;
    END

    IF @CurrentStatus = 'Closed'
    BEGIN
        SELECT 0 AS Success, 'Cannot comment on closed tickets.' AS ErrorMessage; RETURN;
    END

    IF @IsAdmin = 0 AND @CreatedByUserId <> @AuthorId
    BEGIN
        SELECT 0 AS Success, 'Access denied.' AS ErrorMessage; RETURN;
    END

    -- Non-admins cannot post internal comments
    DECLARE @FinalInternal BIT = CASE WHEN @IsAdmin = 1 THEN @IsInternal ELSE 0 END;

    INSERT INTO dbo.TicketComments
        (TicketId, AuthorUserId, CommentText, IsInternal, CreatedAt)
    VALUES
        (@TicketId, @AuthorId, @CommentText, @FinalInternal, GETUTCDATE());

    SELECT 1 AS Success, NULL AS ErrorMessage;
END
GO

-- ============================================================
-- SP: GetAdminUsers
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.sp_GetAdminUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, FullName, Username
    FROM dbo.Users
    WHERE Role = 'Admin' AND IsActive = 1
    ORDER BY FullName;
END
GO

PRINT 'All stored procedures created successfully.';
GO