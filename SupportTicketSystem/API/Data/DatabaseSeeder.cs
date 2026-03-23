using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace SupportTicketAPI.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(DatabaseContext db)
    {
        using var con = db.CreateConnection();
        await con.OpenAsync();

        var count = await con.ExecuteScalarAsync<int>(
            "sp_GetUserCount",
            commandType: CommandType.StoredProcedure);

        if (count > 0) return;

        var users = new[]
        {
            new { Username = "admin",      Password = "admin123", FullName = "System Administrator", Email = "admin@company.com",     Role = "Admin" },
            new { Username = "support1",   Password = "admin123", FullName = "Support Agent One",    Email = "support1@company.com",   Role = "Admin" },
            new { Username = "john.doe",   Password = "user123",  FullName = "John Doe",             Email = "john.doe@company.com",   Role = "User"  },
            new { Username = "jane.smith", Password = "user123",  FullName = "Jane Smith",           Email = "jane.smith@company.com", Role = "User"  },
        };

        foreach (var u in users)
        {
            await con.ExecuteAsync("sp_InsertUser", new
            {
                u.Username,
                Password = BCrypt.Net.BCrypt.HashPassword(u.Password),
                u.FullName,
                u.Email,
                u.Role
            }, commandType: CommandType.StoredProcedure);
        }

        Console.WriteLine("[Seeder] Default users created.");
    }
}
