using Microsoft.Data.SqlClient;

namespace SupportTicketAPI.Data;

public class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}
