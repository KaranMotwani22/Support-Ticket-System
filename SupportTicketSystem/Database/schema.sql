-- Customer Support Ticket System - MySQL Schema
-- Run this script to create the database and seed initial data

CREATE DATABASE IF NOT EXISTS SupportTicketDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE SupportTicketDB;

-- =============================================
-- USERS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS Users (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    Username    VARCHAR(100) NOT NULL UNIQUE,
    Password    VARCHAR(255) NOT NULL,   -- bcrypt hash
    FullName    VARCHAR(200) NOT NULL,
    Email       VARCHAR(200) NOT NULL,
    Role        ENUM('User','Admin') NOT NULL DEFAULT 'User',
    IsActive    TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- =============================================
-- TICKETS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS Tickets (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    TicketNumber    VARCHAR(20) NOT NULL UNIQUE,   -- e.g. TKT-00001
    Subject         VARCHAR(300) NOT NULL,
    Description     TEXT NOT NULL,
    Priority        ENUM('Low','Medium','High') NOT NULL DEFAULT 'Medium',
    Status          ENUM('Open','In Progress','Closed') NOT NULL DEFAULT 'Open',
    CreatedByUserId INT NOT NULL,
    AssignedToUserId INT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_ticket_created_by  FOREIGN KEY (CreatedByUserId)   REFERENCES Users(Id),
    CONSTRAINT fk_ticket_assigned_to FOREIGN KEY (AssignedToUserId)  REFERENCES Users(Id)
);

-- =============================================
-- TICKET STATUS HISTORY TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS TicketStatusHistory (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    TicketId        INT NOT NULL,
    OldStatus       ENUM('Open','In Progress','Closed') NULL,
    NewStatus       ENUM('Open','In Progress','Closed') NOT NULL,
    ChangedByUserId INT NOT NULL,
    ChangedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Notes           VARCHAR(500) NULL,
    CONSTRAINT fk_history_ticket  FOREIGN KEY (TicketId)        REFERENCES Tickets(Id),
    CONSTRAINT fk_history_user    FOREIGN KEY (ChangedByUserId) REFERENCES Users(Id)
);

-- =============================================
-- TICKET COMMENTS TABLE
-- =============================================
CREATE TABLE IF NOT EXISTS TicketComments (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    TicketId        INT NOT NULL,
    AuthorUserId    INT NOT NULL,
    CommentText     TEXT NOT NULL,
    IsInternal      TINYINT(1) NOT NULL DEFAULT 0,   -- 1 = admin-only internal note
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_comment_ticket FOREIGN KEY (TicketId)      REFERENCES Tickets(Id),
    CONSTRAINT fk_comment_author FOREIGN KEY (AuthorUserId)  REFERENCES Users(Id)
);

-- =============================================
-- INDEXES
-- =============================================
CREATE INDEX idx_tickets_status       ON Tickets(Status);
CREATE INDEX idx_tickets_created_by   ON Tickets(CreatedByUserId);
CREATE INDEX idx_tickets_assigned_to  ON Tickets(AssignedToUserId);
CREATE INDEX idx_history_ticket       ON TicketStatusHistory(TicketId);
CREATE INDEX idx_comments_ticket      ON TicketComments(TicketId);

-- =============================================
-- SEED DATA
-- Passwords are bcrypt hashes of:
--   admin123  (for admin)
--   user123   (for john.doe and jane.smith)
-- =============================================
INSERT INTO Users (Username, Password, FullName, Email, Role) VALUES
('admin',      '$2a$11$5M/8uFMSB0.1h4cGaQ0.8eB0R5Lnb7v6kE2VAbcdefghijklmnopqr', 'System Administrator', 'admin@company.com',      'Admin'),
('support1',   '$2a$11$5M/8uFMSB0.1h4cGaQ0.8eB0R5Lnb7v6kE2VAbcdefghijklmnopqr', 'Support Agent One',    'support1@company.com',    'Admin'),
('john.doe',   '$2a$11$ABC123defghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ', 'John Doe',             'john.doe@company.com',    'User'),
('jane.smith', '$2a$11$ABC123defghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ', 'Jane Smith',           'jane.smith@company.com',  'User');

-- NOTE: The seed passwords above use placeholder hashes.
-- The actual hashes are generated at first-run by the API seed method.
-- See SeedData notes in README. Use the API /api/auth/login with:
--   admin / admin123
--   john.doe / user123
--   jane.smith / user123
-- The real hashes are inserted by DatabaseSeeder.cs on startup.
