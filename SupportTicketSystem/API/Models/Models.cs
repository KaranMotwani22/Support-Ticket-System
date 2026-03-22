namespace SupportTicketAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";  // "User" or "Admin"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";   // Low / Medium / High
    public string Status { get; set; } = "Open";        // Open / In Progress / Closed
    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Joined fields (not stored, populated by queries)
    public string? CreatedByName { get; set; }
    public string? AssignedToName { get; set; }
}

public class TicketStatusHistory
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }

    // Joined
    public string? ChangedByName { get; set; }
}

public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int AuthorUserId { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }

    // Joined
    public string? AuthorName { get; set; }
    public string? AuthorRole { get; set; }
}
