namespace SupportTicketDesktop.Models;

public class LoginResponse
{
    public string Token    { get; set; } = string.Empty;
    public int    UserId   { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role     { get; set; } = string.Empty;
}

public class TicketListItem
{
    public int      Id             { get; set; }
    public string   TicketNumber   { get; set; } = string.Empty;
    public string   Subject        { get; set; } = string.Empty;
    public string   Priority       { get; set; } = string.Empty;
    public string   Status         { get; set; } = string.Empty;
    public DateTime CreatedAt      { get; set; }
    public string?  AssignedToName { get; set; }
    public string   CreatedByName  { get; set; } = string.Empty;
}

public class TicketDetailResponse
{
    public int            Id             { get; set; }
    public string         TicketNumber   { get; set; } = string.Empty;
    public string         Subject        { get; set; } = string.Empty;
    public string         Description    { get; set; } = string.Empty;
    public string         Priority       { get; set; } = string.Empty;
    public string         Status         { get; set; } = string.Empty;
    public DateTime       CreatedAt      { get; set; }
    public string         CreatedByName  { get; set; } = string.Empty;
    public string?        AssignedToName { get; set; }
    public List<HistoryItemDto> History  { get; set; } = new();
    public List<CommentDto>     Comments { get; set; } = new();
}

public class HistoryItemDto
{
    public string?  OldStatus     { get; set; }
    public string   NewStatus     { get; set; } = string.Empty;
    public string   ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt     { get; set; }
    public string?  Notes         { get; set; }
}

public class CommentDto
{
    public int      Id          { get; set; }
    public string   AuthorName  { get; set; } = string.Empty;
    public string   AuthorRole  { get; set; } = string.Empty;
    public string   CommentText { get; set; } = string.Empty;
    public bool     IsInternal  { get; set; }
    public DateTime CreatedAt   { get; set; }
}

public class AdminUserDto
{
    public int    Id       { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public override string ToString() => FullName;
}

public class ApiResponse<T>
{
    public bool    Success { get; set; }
    public string? Message { get; set; }
    public T?      Data    { get; set; }
}
