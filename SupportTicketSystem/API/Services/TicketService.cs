using System.Data;
using Dapper;
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
    public async Task<TicketDetailResponse?> CreateTicketAsync(CreateTicketRequest req, int userId)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var count = await con.ExecuteScalarAsync<int>(
            "sp_GetTicketCount",
            commandType: CommandType.StoredProcedure);

        var ticketNumber = $"TKT-{(count + 1):D5}";

        var newId = await con.ExecuteScalarAsync<int>(
            "sp_CreateTicket",
            new
            {
                TicketNumber    = ticketNumber,
                req.Subject,
                req.Description,
                req.Priority,
                CreatedByUserId = userId
            },
            commandType: CommandType.StoredProcedure);

        await con.ExecuteAsync(
            "sp_InsertTicketHistory",
            new
            {
                TicketId        = newId,
                OldStatus       = (string?)null,
                NewStatus       = "Open",
                ChangedByUserId = userId,
                Notes           = "Ticket created"
            },
            commandType: CommandType.StoredProcedure);

        return await GetTicketByIdAsync(newId, userId, isAdmin: false);
    }

    // ─────────────────────────────────────────────────────────────
    //  Get ticket list (role-based)
    // ─────────────────────────────────────────────────────────────
    public async Task<IEnumerable<TicketListItem>> GetTicketsAsync(int userId, bool isAdmin)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        return await con.QueryAsync<TicketListItem>(
            "sp_GetTickets",
            new { UserId = userId, IsAdmin = isAdmin ? 1 : 0 },
            commandType: CommandType.StoredProcedure);
    }

    // ─────────────────────────────────────────────────────────────
    //  Get ticket detail
    // ─────────────────────────────────────────────────────────────
    public async Task<TicketDetailResponse?> GetTicketByIdAsync(int ticketId, int requestingUserId, bool isAdmin)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var ticket = await con.QuerySingleOrDefaultAsync<Ticket>(
            "sp_GetTicketById",
            new { TicketId = ticketId },
            commandType: CommandType.StoredProcedure);

        if (ticket == null) return null;
        if (!isAdmin && ticket.CreatedByUserId != requestingUserId) return null;

        var history = await con.QueryAsync<HistoryItemDto>(
            "sp_GetTicketHistory",
            new { TicketId = ticketId },
            commandType: CommandType.StoredProcedure);

        var comments = await con.QueryAsync<CommentDto>(
            "sp_GetTicketComments",
            new { TicketId = ticketId, IsAdmin = isAdmin ? 1 : 0 },
            commandType: CommandType.StoredProcedure);

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
    public async Task<(bool ok, string? error)> AssignTicketAsync(int ticketId, int? assignedToUserId, int adminUserId)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var result = await con.QuerySingleAsync<SpResult>(
            "sp_AssignTicket",
            new
            {
                TicketId         = ticketId,
                AssignedToUserId = assignedToUserId,
                AdminUserId      = adminUserId
            },
            commandType: CommandType.StoredProcedure);

        return result.Success == 1 ? (true, null) : (false, result.ErrorMessage);
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

        var result = await con.QuerySingleAsync<SpResult>(
            "sp_UpdateTicketStatus",
            new
            {
                TicketId    = ticketId,
                req.NewStatus,
                AdminUserId = adminUserId,
                req.Notes
            },
            commandType: CommandType.StoredProcedure);

        return result.Success == 1 ? (true, null) : (false, result.ErrorMessage);
    }

    // ─────────────────────────────────────────────────────────────
    //  Add comment
    // ─────────────────────────────────────────────────────────────
    public async Task<(bool ok, string? error)> AddCommentAsync(int ticketId, AddCommentRequest req, int userId, bool isAdmin)
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        var result = await con.QuerySingleAsync<SpResult>(
            "sp_AddTicketComment",
            new
            {
                TicketId    = ticketId,
                AuthorId    = userId,
                req.CommentText,
                req.IsInternal,
                IsAdmin     = isAdmin ? 1 : 0
            },
            commandType: CommandType.StoredProcedure);

        return result.Success == 1 ? (true, null) : (false, result.ErrorMessage);
    }

    // ─────────────────────────────────────────────────────────────
    //  Get admin users (for assign dropdown)
    // ─────────────────────────────────────────────────────────────
    public async Task<IEnumerable<AdminUserDto>> GetAdminUsersAsync()
    {
        using var con = _db.CreateConnection();
        await con.OpenAsync();

        return await con.QueryAsync<AdminUserDto>(
            "sp_GetAdminUsers",
            commandType: CommandType.StoredProcedure);
    }

    // ─────────────────────────────────────────────────────────────
    //  Helper: maps SP result columns
    // ─────────────────────────────────────────────────────────────
    private class SpResult
    {
        public int     Success      { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
