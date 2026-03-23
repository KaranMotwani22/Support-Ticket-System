using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketAPI.DTOs;
using SupportTicketAPI.Services;

namespace SupportTicketAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly TicketService _tickets;

    public TicketsController(TicketService tickets) => _tickets = tickets;

    private int  CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin       => User.IsInRole("Admin");

    // GET /api/tickets
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _tickets.GetTicketsAsync(CurrentUserId, IsAdmin);
        return Ok(ApiResponse<IEnumerable<TicketListItem>>.Ok(list));
    }

    // GET /api/tickets/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var ticket = await _tickets.GetTicketByIdAsync(id, CurrentUserId, IsAdmin);
        if (ticket == null)
            return NotFound(ApiResponse<string>.Fail("Ticket not found or access denied."));

        return Ok(ApiResponse<TicketDetailResponse>.Ok(ticket));
    }

    // POST /api/tickets
    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Subject))
            return BadRequest(ApiResponse<string>.Fail("Subject is required."));
        if (string.IsNullOrWhiteSpace(req.Description))
            return BadRequest(ApiResponse<string>.Fail("Description is required."));

        var valid = new[] { "Low", "Medium", "High" };
        if (!valid.Contains(req.Priority))
            return BadRequest(ApiResponse<string>.Fail("Priority must be Low, Medium, or High."));

        var ticket = await _tickets.CreateTicketAsync(req, CurrentUserId);
        if (ticket == null)
            return StatusCode(500, ApiResponse<string>.Fail("Failed to create ticket."));

        return CreatedAtAction(nameof(Get), new { id = ticket.Id },
            ApiResponse<object>.Ok(ticket, "Ticket created."));
    }

    // PUT /api/tickets/{id}/assign
    [HttpPut("{id:int}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignTicketRequest req)
    {
        var (ok, error) = await _tickets.AssignTicketAsync(id, req.AssignedToUserId, CurrentUserId);
        if (!ok) return BadRequest(ApiResponse<string>.Fail(error!));

        return Ok(ApiResponse<string>.Ok("Assigned", "Ticket assigned successfully."));
    }

    // PUT /api/tickets/{id}/status
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewStatus))
            return BadRequest(ApiResponse<string>.Fail("NewStatus is required."));

        var (ok, error) = await _tickets.UpdateStatusAsync(id, req, CurrentUserId);
        if (!ok) return BadRequest(ApiResponse<string>.Fail(error!));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated."));
    }

    // POST /api/tickets/{id}/comments
    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CommentText))
            return BadRequest(ApiResponse<string>.Fail("Comment text is required."));

        var (ok, error) = await _tickets.AddCommentAsync(id, req, CurrentUserId, IsAdmin);
        if (!ok) return BadRequest(ApiResponse<string>.Fail(error!));

        return Ok(ApiResponse<string>.Ok("Added", "Comment added."));
    }

    // GET /api/tickets/admins
    [HttpGet("admins")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdmins()
    {
        var admins = await _tickets.GetAdminUsersAsync();
        return Ok(ApiResponse<IEnumerable<AdminUserDto>>.Ok(admins));
    }
}
