using Dapper;
using Microsoft.Data.SqlClient;
using SupportTicketAPI.Data;

namespace SupportTicketAPI.Data;

/// <summary>
/// Seeds default users on first startup if the Users table is empty.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(DatabaseContext db)
    {
        using var con = db.CreateConnection();
        await con.OpenAsync();

        var count = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users");
        if (count > 0) return;

        // Create default users with bcrypt hashed passwords
        var users = new[]
        {
            new { Username = "admin",      Password = "admin123", FullName = "System Administrator", Email = "admin@company.com",      Role = "Admin" },
            new { Username = "support1",   Password = "admin123", FullName = "Support Agent One",    Email = "support1@company.com",    Role = "Admin" },
            new { Username = "john.doe",   Password = "user123",  FullName = "John Doe",             Email = "john.doe@company.com",    Role = "User"  },
            new { Username = "jane.smith", Password = "user123",  FullName = "Jane Smith",           Email = "jane.smith@company.com",  Role = "User"  },
        };

        const string sql = @"INSERT INTO Users (Username, Password, FullName, Email, Role)
                             VALUES (@Username, @Password, @FullName, @Email, @Role)";

        foreach (var u in users)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(u.Password);
            await con.ExecuteAsync(sql, new
            {
                u.Username,
                Password = hashed,
                u.FullName,
                u.Email,
                u.Role
            });
        }

        Console.WriteLine("[Seeder] Default users created.");
    }
}
