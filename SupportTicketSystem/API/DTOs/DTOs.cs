namespace SupportTicketAPI.DTOs;

// ---- Auth ----
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// ---- Ticket ----
public class CreateTicketRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
}

public class TicketListItem
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? AssignedToName { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}

public class TicketDetailResponse
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public List<HistoryItemDto> History { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
}

public class HistoryItemDto
{
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Notes { get; set; }
}

public class CommentDto
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---- Admin Actions ----
public class AssignTicketRequest
{
    public int? AssignedToUserId { get; set; }
}

public class UpdateStatusRequest
{
    public string NewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class AddCommentRequest
{
    public string CommentText { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;
}

// ---- Admin list ----
public class AdminUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

// ---- Generic ----
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? msg = null) =>
        new() { Success = true, Data = data, Message = msg };

    public static ApiResponse<T> Fail(string msg) =>
        new() { Success = false, Message = msg };
}
