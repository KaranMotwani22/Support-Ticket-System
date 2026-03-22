using Dapper;
using Microsoft.Data.SqlClient;
using SupportTicketAPI.Data;
using SupportTicketAPI.DTOs;
using SupportTicketAPI.Models;

namespace SupportTicketAPI.Services;

public class TicketService
{
    private readonly DatabaseContext _db;

    public TicketService(DatabaseContext db) => _db = db;

    // ─────────────────────────────────────────────────────────────
    //  Create Ticket
    // ─────────────────────────────────────────────────────────────
    public async Task<TicketDetailResponse> CreateTicketAsync(CreateTicketRequest req, int userId)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        // Generate ticket number: TKT-XXXXX
        var count = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Tickets");
        var ticketNumber = $"TKT-{(count + 1):D5}";

        const string sql = @"
            INSERT INTO Tickets (TicketNumber, Subject, Description, Priority, Status, CreatedByUserId, CreatedAt, UpdatedAt)
            VALUES (@TicketNumber, @Subject, @Description, @Priority, 'Open', @CreatedByUserId, GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var id = await con.ExecuteScalarAsync<int>(sql, new
        {
            TicketNumber    = ticketNumber,
            req.Subject,
            req.Description,
            req.Priority,
            CreatedByUserId = userId
        });

        // Log initial status in history
        await con.ExecuteAsync(@"
            INSERT INTO TicketStatusHistory (TicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Notes)
            VALUES (@TicketId, NULL, 'Open', @UserId, GETUTCDATE(), 'Ticket created')",
            new { TicketId = id, UserId = userId });

        return (await GetTicketByIdAsync(id, userId, isAdmin: false))!;
    }

    // ─────────────────────────────────────────────────────────────
    //  Get ticket list (role-based)
    // ─────────────────────────────────────────────────────────────
    public async Task<IEnumerable<TicketListItem>> GetTicketsAsync(int userId, bool isAdmin)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var where = isAdmin ? "" : "WHERE t.CreatedByUserId = @UserId";

        var sql = $@"
            SELECT t.Id, t.TicketNumber, t.Subject, t.Priority, t.Status, t.CreatedAt,
                   cu.FullName AS CreatedByName,
                   au.FullName AS AssignedToName
            FROM Tickets t
            INNER JOIN Users cu ON cu.Id = t.CreatedByUserId
            LEFT  JOIN Users au ON au.Id = t.AssignedToUserId
            {where}
            ORDER BY t.CreatedAt DESC";

        return await con.QueryAsync<TicketListItem>(sql, new { UserId = userId });
    }

    // ─────────────────────────────────────────────────────────────
    //  Get ticket detail
    // ─────────────────────────────────────────────────────────────
    public async Task<TicketDetailResponse?> GetTicketByIdAsync(int ticketId, int requestingUserId, bool isAdmin)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var ticket = await con.QuerySingleOrDefaultAsync<Ticket>(@"
            SELECT t.*, cu.FullName AS CreatedByName, au.FullName AS AssignedToName
            FROM Tickets t
            INNER JOIN Users cu ON cu.Id = t.CreatedByUserId
            LEFT  JOIN Users au ON au.Id = t.AssignedToUserId
            WHERE t.Id = @Id", new { Id = ticketId });

        if (ticket == null) return null;
        if (!isAdmin && ticket.CreatedByUserId != requestingUserId) return null;

        // History
        var history = await con.QueryAsync<HistoryItemDto>(@"
            SELECT h.OldStatus, h.NewStatus, u.FullName AS ChangedByName, h.ChangedAt, h.Notes
            FROM TicketStatusHistory h
            INNER JOIN Users u ON u.Id = h.ChangedByUserId
            WHERE h.TicketId = @TicketId
            ORDER BY h.ChangedAt ASC", new { TicketId = ticketId });

        // Comments — non-admin users cannot see internal notes
        var commentFilter = isAdmin ? "" : "AND c.IsInternal = 0";
        var comments = await con.QueryAsync<CommentDto>($@"
            SELECT c.Id, u.FullName AS AuthorName, u.Role AS AuthorRole,
                   c.CommentText, c.IsInternal, c.CreatedAt
            FROM TicketComments c
            INNER JOIN Users u ON u.Id = c.AuthorUserId
            WHERE c.TicketId = @TicketId {commentFilter}
            ORDER BY c.CreatedAt ASC", new { TicketId = ticketId });

        return new TicketDetailResponse
        {
            Id             = ticket.Id,
            TicketNumber   = ticket.TicketNumber,
            Subject        = ticket.Subject,
            Description    = ticket.Description,
            Priority       = ticket.Priority,
            Status         = ticket.Status,
            CreatedAt      = ticket.CreatedAt,
            CreatedByName  = ticket.CreatedByName ?? "",
            AssignedToName = ticket.AssignedToName,
            History        = history.ToList(),
            Comments       = comments.ToList()
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  Assign ticket (Admin)
    // ─────────────────────────────────────────────────────────────
    public async Task<bool> AssignTicketAsync(int ticketId, int? assignedToUserId, int adminUserId)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var ticket = await con.QuerySingleOrDefaultAsync<Ticket>(
            "SELECT * FROM Tickets WHERE Id = @Id", new { Id = ticketId });
        if (ticket == null || ticket.Status == "Closed") return false;

        await con.ExecuteAsync(
            "UPDATE Tickets SET AssignedToUserId = @AssignedTo, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { AssignedTo = assignedToUserId, Id = ticketId });

        // Log as a history note
        await con.ExecuteAsync(@"
            INSERT INTO TicketStatusHistory (TicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Notes)
            VALUES (@TicketId, @Status, @Status, @AdminId, GETUTCDATE(), @Note)",
            new
            {
                TicketId = ticketId,
                Status   = ticket.Status,
                AdminId  = adminUserId,
                Note     = assignedToUserId.HasValue
                    ? $"Ticket assigned to user #{assignedToUserId}"
                    : "Ticket unassigned"
            });

        return true;
    }

    // ─────────────────────────────────────────────────────────────
    //  Update status (Admin)
    // ─────────────────────────────────────────────────────────────
    public async Task<(bool ok, string? error)> UpdateStatusAsync(int ticketId, UpdateStatusRequest req, int adminUserId)
    {
        var allowed = new[] { "Open", "In Progress", "Closed" };
        if (!allowed.Contains(req.NewStatus))
            return (false, "Invalid status value.");

        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var ticket = await con.QuerySingleOrDefaultAsync<Ticket>(
            "SELECT * FROM Tickets WHERE Id = @Id", new { Id = ticketId });
        if (ticket == null) return (false, "Ticket not found.");
        if (ticket.Status == "Closed") return (false, "Closed tickets cannot be modified.");

        // Enforce flow: Open → In Progress → Closed only
        var validTransitions = new Dictionary<string, string[]>
        {
            { "Open",        new[] { "In Progress" } },
            { "In Progress", new[] { "Closed" } },
        };
        if (validTransitions.TryGetValue(ticket.Status, out var nextAllowed) &&
            !nextAllowed.Contains(req.NewStatus))
            return (false, $"Cannot transition from '{ticket.Status}' to '{req.NewStatus}'.");

        await con.ExecuteAsync(
            "UPDATE Tickets SET Status = @Status, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { Status = req.NewStatus, Id = ticketId });

        await con.ExecuteAsync(@"
            INSERT INTO TicketStatusHistory (TicketId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Notes)
            VALUES (@TicketId, @Old, @New, @AdminId, GETUTCDATE(), @Notes)",
            new { TicketId = ticketId, Old = ticket.Status, New = req.NewStatus, AdminId = adminUserId, req.Notes });

        return (true, null);
    }

    // ─────────────────────────────────────────────────────────────
    //  Add comment
    // ─────────────────────────────────────────────────────────────
    public async Task<(bool ok, string? error)> AddCommentAsync(int ticketId, AddCommentRequest req, int userId, bool isAdmin)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var ticket = await con.QuerySingleOrDefaultAsync<Ticket>(
            "SELECT * FROM Tickets WHERE Id = @Id", new { Id = ticketId });
        if (ticket == null) return (false, "Ticket not found.");
        if (ticket.Status == "Closed") return (false, "Cannot comment on closed tickets.");
        if (!isAdmin && ticket.CreatedByUserId != userId) return (false, "Access denied.");

        // Regular users cannot post internal comments
        var isInternal = isAdmin && req.IsInternal;

        await con.ExecuteAsync(@"
            INSERT INTO TicketComments (TicketId, AuthorUserId, CommentText, IsInternal, CreatedAt)
            VALUES (@TicketId, @AuthorId, @Text, @Internal, GETUTCDATE())",
            new { TicketId = ticketId, AuthorId = userId, Text = req.CommentText, Internal = isInternal });

        return (true, null);
    }

    // ─────────────────────────────────────────────────────────────
    //  Get admin users (for assign dropdown)
    // ─────────────────────────────────────────────────────────────
    public async Task<IEnumerable<AdminUserDto>> GetAdminUsersAsync()
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        return await con.QueryAsync<AdminUserDto>(
            "SELECT Id, FullName, Username FROM Users WHERE Role = 'Admin' AND IsActive = 1 ORDER BY FullName");
    }
}
