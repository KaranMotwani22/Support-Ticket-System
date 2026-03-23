# Customer Support Ticket System

A full-stack Customer Support Ticket Management System built with a **C# WinForms** desktop application as the frontend, **ASP.NET Web API (.NET 8)** as the backend, and **Microsoft SQL Server** as the database. All data access is handled exclusively through **Stored Procedures** using **Dapper**.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [User Roles](#user-roles)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Setup & Installation](#setup--installation)
- [Default Login Credentials](#default-login-credentials)
- [API Endpoints](#api-endpoints)
- [Database Schema](#database-schema)
- [Stored Procedures](#stored-procedures)
- [Business Rules](#business-rules)
- [Assumptions & Design Decisions](#assumptions--design-decisions)

---

## Project Overview

The system allows:
- **Users** to raise support tickets, track their status, and add comments.
- **Admins** to manage all tickets — assign them, update their status, and post internal notes.

The Desktop Application communicates **exclusively through the Web API**. Direct database access from the desktop app is not permitted.

---

## Tech Stack

| Layer             | Technology                        |
|-------------------|-----------------------------------|
| Frontend          | C# WinForms (.NET 8, Windows)     |
| Backend           | ASP.NET Web API (.NET 8)          |
| Database          | Microsoft SQL Server              |
| ORM / Data Access | Dapper (micro-ORM)                |
| Authentication    | JWT Bearer Tokens                 |
| Password Hashing  | BCrypt (BCrypt.Net-Next)          |
| Serialization     | Newtonsoft.Json                   |
| API Docs          | Swagger / Swashbuckle             |

---

## Project Structure

```
SupportTicketSystem/
│
├── SupportTicketSystem.sln               ← Open both projects together
│
├── API/                                  ← ASP.NET Web API (Backend)
│   ├── SupportTicketAPI.sln              ← Open API alone
│   ├── SupportTicketAPI.csproj
│   ├── Program.cs                        ← App bootstrap, DI, middleware
│   ├── appsettings.json                  ← Connection string & JWT config
│   │
│   ├── Controllers/
│   │   ├── AuthController.cs             ← POST /api/auth/login
│   │   └── TicketsController.cs          ← All ticket endpoints
│   │
│   ├── Services/
│   │   ├── AuthService.cs                ← Login logic & JWT generation
│   │   └── TicketService.cs              ← All ticket business logic
│   │
│   ├── Data/
│   │   ├── DatabaseContext.cs            ← SqlConnection factory
│   │   └── DatabaseSeeder.cs            ← Seeds default users on startup
│   │
│   ├── Models/
│   │   └── Models.cs                     ← DB entity models
│   │
│   └── DTOs/
│       └── DTOs.cs                       ← Request/Response shapes
│
├── DesktopApp/                           ← WinForms Desktop Application
│   ├── SupportTicketDesktop.sln          ← Open Desktop App alone
│   ├── SupportTicketDesktop.csproj
│   ├── Program.cs                        ← WinForms entry point
│   │
│   ├── Forms/
│   │   ├── LoginForm.cs                  ← Login screen
│   │   ├── MainForm.cs                   ← Ticket list screen
│   │   ├── CreateTicketForm.cs           ← New ticket screen (Users only)
│   │   └── TicketDetailForm.cs           ← Ticket detail + Admin actions
│   │
│   ├── Services/
│   │   └── ApiClient.cs                  ← HttpClient wrapper for API calls
│   │
│   └── Models/
│       └── Models.cs                     ← Client-side DTOs
│
└── Database/
    ├── schema_sqlserver.sql              ← Run first — creates all tables
    └── stored_procedures.sql            ← Run second — creates all SPs
```

---

## User Roles

### User
- Can create support tickets
- Can view only their own tickets
- Can add public comments to their own tickets
- Cannot modify or comment on closed tickets

### Admin
- Can view all tickets from all users
- Can assign tickets to admin users
- Can update ticket status
- Can add public or internal-only comments
- All actions are logged in ticket history

---

## Features

- **JWT Authentication** — secure login with role-based access
- **Role-based ticket visibility** — users see only their tickets, admins see all
- **Auto-generated ticket numbers** — format: `TKT-00001`, `TKT-00002`, etc.
- **Ticket status workflow** — enforced `Open → In Progress → Closed` flow
- **Full audit history** — every status change and assignment is logged
- **Internal comments** — admin-only notes invisible to regular users
- **Server-side timestamps** — all dates use `GETUTCDATE()` on SQL Server
- **Stored procedures** — all database operations go through SPs, no raw SQL in code
- **Swagger UI** — interactive API documentation at `/swagger`

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or higher)
- [SQL Server Management Studio (SSMS)](https://aka.ms/ssmsfullsetup) or Azure Data Studio
- Windows OS (required for WinForms desktop app)
- Visual Studio 2022 (recommended) or VS Code with C# extension

---

## Setup & Installation

### Step 1 — Database Setup

Open **SSMS**, connect to your SQL Server instance, and run the following scripts **in this exact order**:

**1. Create the database and tables:**
```
Database/schema_sqlserver.sql
```

**2. Create all stored procedures:**
```
Database/stored_procedures.sql
```

> ⚠️ Do **not** manually insert users. Default users are seeded automatically by the API on first startup with correct BCrypt password hashes.

---

### Step 2 — Configure the API

Open `API/appsettings.json` and set your connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SupportTicketDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "SuperSecretKeyForSupportTicketSystem2024!ChangeMe",
    "Issuer": "SupportTicketAPI",
    "Audience": "SupportTicketDesktop"
  },
  "Urls": "http://localhost:5000"
}
```

**Using SQL Server Authentication instead of Windows Authentication?**
```
Server=localhost;Database=SupportTicketDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;
```

> ⚠️ Change the `Jwt:Key` to a long random string before deploying anywhere.

---

### Step 3 — Run the API

**Option A — Command Line:**
```bash
cd SupportTicketSystem/API
dotnet restore
dotnet run
```

**Option B — Visual Studio:**
Open `API/SupportTicketAPI.sln` → Press **F5**

The API starts at `http://localhost:5000`.
Swagger UI is available at `http://localhost:5000/swagger`.

On first run you will see in the console:
```
[Seeder] Default users created.
```

---

### Step 4 — Run the Desktop App

> ⚠️ The API must be running before launching the desktop app.

**Option A — Command Line** (new terminal):
```bash
cd SupportTicketSystem/DesktopApp
dotnet restore
dotnet run
```

**Option B — Visual Studio:**
Open `DesktopApp/SupportTicketDesktop.sln` → Press **F5**

---

## Default Login Credentials

| Username     | Password   | Role  |
|--------------|------------|-------|
| `admin`      | `admin123` | Admin |
| `support1`   | `admin123` | Admin |
| `john.doe`   | `user123`  | User  |
| `jane.smith` | `user123`  | User  |

---

## API Endpoints

All endpoints except login require an `Authorization: Bearer <token>` header.

| Method | Endpoint                          | Role        | Description                         |
|--------|-----------------------------------|-------------|-------------------------------------|
| POST   | `/api/auth/login`                 | Public      | Login and receive JWT token         |
| GET    | `/api/tickets`                    | Any         | List tickets (filtered by role)     |
| GET    | `/api/tickets/{id}`               | Any         | Get ticket detail with history      |
| POST   | `/api/tickets`                    | User only   | Create a new ticket                 |
| PUT    | `/api/tickets/{id}/assign`        | Admin only  | Assign ticket to an admin           |
| PUT    | `/api/tickets/{id}/status`        | Admin only  | Update ticket status                |
| POST   | `/api/tickets/{id}/comments`      | Any         | Add comment (admins can go internal)|
| GET    | `/api/tickets/admins`             | Admin only  | Get admin list for assign dropdown  |

### Example — Login
```json
POST /api/auth/login
{
  "username": "admin",
  "password": "admin123"
}
```

### Example — Create Ticket
```json
POST /api/tickets
Authorization: Bearer <token>

{
  "subject": "Cannot access my account",
  "description": "I have been unable to log in since yesterday.",
  "priority": "High"
}
```

---

## Database Schema

| Table                 | Description                                      |
|-----------------------|--------------------------------------------------|
| `Users`               | All users with hashed passwords and roles        |
| `Tickets`             | Core ticket data — status, priority, assignment  |
| `TicketStatusHistory` | Audit log of every status change and assignment  |
| `TicketComments`      | Public and internal comments per ticket          |

### Ticket Status Flow
```
Open  ──►  In Progress  ──►  Closed
```
- Flow is **one-way only** — no reversals allowed
- **Closed** tickets cannot be modified or commented on

---

## Stored Procedures

| Stored Procedure         | Purpose                                       |
|--------------------------|-----------------------------------------------|
| `sp_GetUserByUsername`   | Fetch user record for login                   |
| `sp_GetUserCount`        | Count users (seeder check)                    |
| `sp_InsertUser`          | Insert new user (seeder)                      |
| `sp_GetTickets`          | Role-based ticket list                        |
| `sp_GetTicketById`       | Single ticket detail with joined names        |
| `sp_GetTicketHistory`    | Full status history for a ticket              |
| `sp_GetTicketComments`   | Comments with internal filtering by role      |
| `sp_GetTicketCount`      | Count tickets (for number generation)         |
| `sp_CreateTicket`        | Insert new ticket, returns new ID             |
| `sp_InsertTicketHistory` | Log a status or assignment change             |
| `sp_AssignTicket`        | Assign/unassign ticket, validates closed state|
| `sp_UpdateTicketStatus`  | Change status, enforces flow rules            |
| `sp_AddTicketComment`    | Add comment, validates access and status      |
| `sp_GetAdminUsers`       | List admins for assign dropdown               |

---

## Business Rules

| Rule                                        | Where Enforced                  |
|---------------------------------------------|---------------------------------|
| Ticket numbers auto-generated as `TKT-XXXXX`| `TicketService.cs`              |
| Status flow: Open → In Progress → Closed    | `sp_UpdateTicketStatus`         |
| Closed tickets cannot be modified           | All write SPs                   |
| Users see only their own tickets            | `sp_GetTickets`                 |
| Internal comments hidden from users         | `sp_GetTicketComments`          |
| All dates use server UTC time               | `GETUTCDATE()` in every SP      |
| Passwords hashed with BCrypt                | `DatabaseSeeder.cs`             |
| JWT expires after 8 hours                   | `AuthService.cs`                |
| All admin actions logged to history         | `sp_AssignTicket`, `sp_UpdateTicketStatus` |

---

## Assumptions & Design Decisions

1. **Dapper over Entity Framework Core** — Since all data access goes through stored procedures, Dapper is the right fit. It maps SP results to typed C# objects cleanly with minimal overhead. EF Core's strengths (LINQ, change tracking) are not needed here.

2. **JWT stored in-memory on the desktop** — The desktop app holds the token in a static session field for the application's lifetime. No file or registry storage is used.

3. **Ticket number generation** — Uses `COUNT(*) + 1` at creation time. Adequate for this project's scope; a dedicated SQL `SEQUENCE` would be safer under very high concurrent load.

4. **No ticket reopening** — Once `Closed`, a ticket cannot be reopened. This is intentional per the assignment specification.

5. **CORS set to AllowAll** — Configured open for local development convenience. Must be restricted to specific origins before any production deployment.

6. **JWT secret in appsettings.json** — Acceptable for development and demonstration. In production this must be stored in environment variables or a secrets manager such as Azure Key Vault.

7. **SP-only data access** — No raw SQL strings exist anywhere in the C# codebase. Every database operation goes through a named stored procedure, keeping the data layer clean, auditable, and secure against SQL injection.