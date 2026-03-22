# Customer Support Ticket System

A full-stack desktop application for managing customer support tickets, built as per the technical assignment specification.

---

## Tech Stack

| Layer        | Technology                          |
|--------------|-------------------------------------|
| Frontend     | C# WinForms (.NET 8, Windows)       |
| Backend      | ASP.NET Web API (.NET 8)            |
| Database     | MySQL 8.x                           |
| ORM / Data   | Dapper (micro-ORM)                  |
| Auth         | JWT Bearer Tokens                   |
| Passwords    | BCrypt hashing (BCrypt.Net-Next)    |
| Serialization| Newtonsoft.Json                     |

---

## Project Structure

```
SupportTicketSystem/
├── SupportTicketSystem.sln
│
├── API/                              ← ASP.NET Web API (backend)
│   ├── Controllers/
│   │   ├── AuthController.cs         POST /api/auth/login
│   │   └── TicketsController.cs      All ticket endpoints
│   ├── Services/
│   │   ├── AuthService.cs            JWT generation & login logic
│   │   └── TicketService.cs          All business logic
│   ├── Models/
│   │   └── Models.cs                 DB entity models
│   ├── DTOs/
│   │   └── DTOs.cs                   Request/Response shapes
│   ├── Data/
│   │   ├── DatabaseContext.cs        MySqlConnection factory
│   │   └── DatabaseSeeder.cs        Seeds default users on startup
│   ├── Program.cs                    App bootstrap & DI
│   └── appsettings.json              Connection string & JWT config
│
├── DesktopApp/                       ← WinForms Desktop Application
│   ├── Forms/
│   │   ├── LoginForm.cs              Login screen
│   │   ├── MainForm.cs               Ticket list screen
│   │   ├── CreateTicketForm.cs       New ticket screen (Users)
│   │   └── TicketDetailForm.cs       Detail + Admin actions screen
│   ├── Services/
│   │   └── ApiClient.cs              HttpClient wrapper for all API calls
│   ├── Models/
│   │   └── Models.cs                 Client-side DTOs (mirrors API)
│   └── Program.cs                    WinForms entry point
│
└── Database/
    └── schema.sql                    MySQL schema + seed comments
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL 8.x](https://dev.mysql.com/downloads/mysql/) (running locally on port 3306)
- Windows OS (for the WinForms desktop app)
- Visual Studio 2022 **or** VS Code with C# extension

---

## Setup Instructions

### Step 1 — Create the MySQL Database

1. Open MySQL Workbench or your preferred MySQL client.
2. Run the schema script:

```sql
SOURCE /path/to/SupportTicketSystem/Database/schema.sql;
```

Or copy-paste the contents of `Database/schema.sql` and execute it.

This creates the `SupportTicketDB` database with all tables.  
> The actual user records (with properly hashed passwords) are inserted automatically by the API on first startup — ignore the placeholder hashes in the SQL file.

---

### Step 2 — Configure the API

Open `API/appsettings.json` and update your MySQL credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=SupportTicketDB;Uid=root;Pwd=YOUR_MYSQL_PASSWORD;"
  },
  "Jwt": {
    "Key": "SuperSecretKeyForSupportTicketSystem2024!ChangeMe",
    "Issuer": "SupportTicketAPI",
    "Audience": "SupportTicketDesktop"
  },
  "Urls": "http://localhost:5000"
}
```

Replace `YOUR_MYSQL_PASSWORD` with your actual MySQL root password (or another user).

---

### Step 3 — Run the API

```bash
cd SupportTicketSystem/API
dotnet restore
dotnet run
```

The API starts at `http://localhost:5000`.  
Swagger UI is available at `http://localhost:5000/swagger` (Development mode).

On first run, the seeder automatically inserts default users.

---

### Step 4 — Run the Desktop App

Open a new terminal:

```bash
cd SupportTicketSystem/DesktopApp
dotnet restore
dotnet run
```

Or open `SupportTicketSystem.sln` in Visual Studio 2022, set `SupportTicketDesktop` as the startup project, and press **F5**.

> **Important:** The API must be running before starting the desktop app.

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

| Method | Endpoint                         | Role         | Description              |
|--------|----------------------------------|--------------|--------------------------|
| POST   | `/api/auth/login`                | Public       | Login, get JWT token     |
| GET    | `/api/tickets`                   | Any          | List tickets (role-based)|
| GET    | `/api/tickets/{id}`              | Any          | Ticket detail            |
| POST   | `/api/tickets`                   | User only    | Create new ticket        |
| PUT    | `/api/tickets/{id}/assign`       | Admin only   | Assign ticket            |
| PUT    | `/api/tickets/{id}/status`       | Admin only   | Update status            |
| POST   | `/api/tickets/{id}/comments`     | Any          | Add comment              |
| GET    | `/api/tickets/admins`            | Admin only   | List admins (for dropdown)|

---

## Business Rules Implemented

- ✅ Ticket numbers auto-generated: `TKT-00001`, `TKT-00002`, ...
- ✅ Status flow enforced: **Open → In Progress → Closed** (no skipping, no reversing)
- ✅ Closed tickets cannot be modified or commented on
- ✅ Users can only see their own tickets
- ✅ Admins can see, assign, and change status on all tickets
- ✅ All status changes are logged to `TicketStatusHistory`
- ✅ Admins can post internal-only comments (hidden from regular users)
- ✅ All dates use server UTC time (`UTC_TIMESTAMP()`)
- ✅ Passwords stored as BCrypt hashes (never plain text)
- ✅ JWT tokens expire after 8 hours

---

## Assumptions & Design Decisions

1. **Dapper over EF Core** — Chosen for simplicity and direct SQL control; fits the project scope well.
2. **JWT stored in-memory** — The desktop app holds the token in a static field for the session lifetime. No file/registry storage used.
3. **Ticket number uniqueness** — Generated using `COUNT(*) + 1` at the time of creation. For production, a dedicated sequence table would be safer under high concurrency.
4. **Status transition enforcement** — Only forward transitions are allowed (Open→In Progress→Closed). Admins cannot reopen a closed ticket by design.
5. **Internal comments** — Only Admins can mark a comment as internal. Regular users never see internal notes in the API response.
6. **CORS** — Set to allow all origins for local development. Restrict in production.
7. **`appsettings.json` JWT Key** — Should be moved to environment variables or secrets in production.
