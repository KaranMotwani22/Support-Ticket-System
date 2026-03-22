using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using SupportTicketDesktop.Models;

namespace SupportTicketDesktop.Services;

/// <summary>
/// Wraps all HTTP calls to the ASP.NET Web API.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private const string BaseUrl = "http://localhost:5000/api/";

    // Current session
    public static string?      Token    { get; private set; }
    public static LoginResponse? Session { get; private set; }
    public static bool IsAdmin => Session?.Role == "Admin";

    public ApiClient()
    {
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ─── Auth ──────────────────────────────────────────────────
    public async Task<(bool ok, string? error, LoginResponse? data)> LoginAsync(string username, string password)
    {
        var payload = JsonConvert.SerializeObject(new { username, password });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync("auth/login", content);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<LoginResponse>>(json);

            if (result?.Success == true && result.Data != null)
            {
                Token   = result.Data.Token;
                Session = result.Data;
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Token);
                return (true, null, result.Data);
            }

            return (false, result?.Message ?? "Login failed.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Cannot reach server: {ex.Message}", null);
        }
    }

    public void Logout()
    {
        Token   = null;
        Session = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    // ─── Tickets ───────────────────────────────────────────────
    public async Task<(bool ok, string? error, List<TicketListItem>? data)> GetTicketsAsync()
    {
        try
        {
            var res  = await _http.GetAsync("tickets");
            var json = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<List<TicketListItem>>>(json);
            if (result?.Success == true)
                return (true, null, result.Data);
            return (false, result?.Message, null);
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    public async Task<(bool ok, string? error, TicketDetailResponse? data)> GetTicketDetailAsync(int id)
    {
        try
        {
            var res  = await _http.GetAsync($"tickets/{id}");
            var json = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<TicketDetailResponse>>(json);
            if (result?.Success == true)
                return (true, null, result.Data);
            return (false, result?.Message, null);
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    public async Task<(bool ok, string? error)> CreateTicketAsync(string subject, string description, string priority)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(new { subject, description, priority });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var res     = await _http.PostAsync("tickets", content);
            var json    = await res.Content.ReadAsStringAsync();
            var result  = JsonConvert.DeserializeObject<ApiResponse<object>>(json);
            return result?.Success == true ? (true, null) : (false, result?.Message);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string? error, List<AdminUserDto>? data)> GetAdminsAsync()
    {
        try
        {
            var res  = await _http.GetAsync("tickets/admins");
            var json = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse<List<AdminUserDto>>>(json);
            if (result?.Success == true)
                return (true, null, result.Data);
            return (false, result?.Message, null);
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    public async Task<(bool ok, string? error)> AssignTicketAsync(int ticketId, int? assignedToUserId)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(new { assignedToUserId });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var res     = await _http.PutAsync($"tickets/{ticketId}/assign", content);
            var json    = await res.Content.ReadAsStringAsync();
            var result  = JsonConvert.DeserializeObject<ApiResponse<string>>(json);
            return result?.Success == true ? (true, null) : (false, result?.Message);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string? error)> UpdateStatusAsync(int ticketId, string newStatus, string? notes)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(new { newStatus, notes });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var res     = await _http.PutAsync($"tickets/{ticketId}/status", content);
            var json    = await res.Content.ReadAsStringAsync();
            var result  = JsonConvert.DeserializeObject<ApiResponse<string>>(json);
            return result?.Success == true ? (true, null) : (false, result?.Message);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string? error)> AddCommentAsync(int ticketId, string commentText, bool isInternal)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(new { commentText, isInternal });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var res     = await _http.PostAsync($"tickets/{ticketId}/comments", content);
            var json    = await res.Content.ReadAsStringAsync();
            var result  = JsonConvert.DeserializeObject<ApiResponse<string>>(json);
            return result?.Success == true ? (true, null) : (false, result?.Message);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }
}
