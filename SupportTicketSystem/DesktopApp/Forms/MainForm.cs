using SupportTicketDesktop.Models;
using SupportTicketDesktop.Services;

namespace SupportTicketDesktop.Forms;

public class MainForm : Form
{
    private readonly ApiClient _api;

    private DataGridView dgvTickets  = null!;
    private Button       btnRefresh  = null!;
    private Button       btnCreate   = null!;
    private Button       btnDetails  = null!;
    private Button       btnLogout   = null!;
    private Label        lblWelcome  = null!;
    private Label        lblStatus   = null!;

    public MainForm(ApiClient api)
    {
        _api = api;
        InitUI();
        _ = LoadTicketsAsync();
    }

    private void InitUI()
    {
        Text            = $"Support Ticket System – Tickets ({ApiClient.Session?.Role})";
        Size            = new Size(900, 580);
        StartPosition   = FormStartPosition.CenterScreen;
        MinimumSize     = new Size(700, 450);
        BackColor       = Color.FromArgb(245, 247, 250);

        // ── top bar ──────────────────────────────────────────────
        var topPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 56,
            BackColor = Color.FromArgb(37, 99, 235)
        };

        lblWelcome = new Label
        {
            Text      = $"Welcome, {ApiClient.Session?.FullName}  [{ApiClient.Session?.Role}]",
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 11, FontStyle.Bold),
            AutoSize  = true,
            Location  = new Point(14, 16)
        };

        btnLogout = new Button
        {
            Text      = "Logout",
            Size      = new Size(80, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 9),
            Cursor    = Cursors.Hand
        };
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnLogout.Click += (s, e) =>
        {
            _api.Logout();
            new LoginForm(_api).Show();
            Close();
        };

        topPanel.Resize += (s, e) =>
            btnLogout.Location = new Point(topPanel.Width - 96, 13);
        btnLogout.Location = new Point(800 - 96, 13);

        topPanel.Controls.AddRange(new Control[] { lblWelcome, btnLogout });

        // ── tool bar ─────────────────────────────────────────────
        var toolBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 44,
            BackColor = Color.White,
            Padding   = new Padding(8, 6, 8, 6)
        };
        toolBar.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(220, 220, 230));
            e.Graphics.DrawLine(pen, 0, toolBar.Height - 1, toolBar.Width, toolBar.Height - 1);
        };

        btnRefresh = MakeBtn("🔄  Refresh", Color.FromArgb(16, 185, 129));
        btnRefresh.Location = new Point(8, 6);
        btnRefresh.Click += async (s, e) => await LoadTicketsAsync();

        btnCreate = MakeBtn("➕  New Ticket", Color.FromArgb(37, 99, 235));
        btnCreate.Location  = new Point(120, 6);
        btnCreate.Visible   = !ApiClient.IsAdmin;   // Only users can create
        btnCreate.Click += (s, e) =>
        {
            var form = new CreateTicketForm(_api);
            form.FormClosed += async (_, _) => await LoadTicketsAsync();
            form.ShowDialog(this);
        };

        btnDetails = MakeBtn("📋  View Details", Color.FromArgb(107, 33, 168));
        btnDetails.Location = new Point(ApiClient.IsAdmin ? 120 : 232, 6);
        btnDetails.Click += (s, e) => OpenDetails();

        toolBar.Controls.AddRange(new Control[] { btnRefresh, btnCreate, btnDetails });

        // ── grid ──────────────────────────────────────────────────
        dgvTickets = new DataGridView
        {
            Dock                    = DockStyle.Fill,
            ReadOnly                = true,
            AllowUserToAddRows      = false,
            AllowUserToDeleteRows   = false,
            SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect             = false,
            RowHeadersVisible       = false,
            BackgroundColor         = Color.FromArgb(245, 247, 250),
            BorderStyle             = BorderStyle.None,
            GridColor               = Color.FromArgb(220, 220, 230),
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight     = 36,
            AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
            Font                    = new Font("Segoe UI", 9.5f)
        };
        dgvTickets.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        dgvTickets.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        dgvTickets.RowTemplate.Height = 30;
        dgvTickets.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenDetails(); };

        dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "TicketNumber", HeaderText = "Ticket #",     FillWeight = 12 });
        dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subject",      HeaderText = "Subject",      FillWeight = 32 });
        dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Priority",     HeaderText = "Priority",     FillWeight = 10 });
        dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status",       HeaderText = "Status",       FillWeight = 12 });
        dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt",    HeaderText = "Created",      FillWeight = 16 });
        dgvTickets.Columns.Add(new DataGridViewTextBoxColumn { Name = "AssignedTo",   HeaderText = "Assigned To",  FillWeight = 18 });

        dgvTickets.CellFormatting += DgvTickets_CellFormatting;

        // ── status bar ────────────────────────────────────────────
        var statusBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 26,
            BackColor = Color.FromArgb(241, 245, 249)
        };
        lblStatus = new Label
        {
            AutoSize  = true,
            Location  = new Point(10, 5),
            ForeColor = Color.FromArgb(100, 116, 139),
            Font      = new Font("Segoe UI", 8.5f)
        };
        statusBar.Controls.Add(lblStatus);

        Controls.Add(dgvTickets);
        Controls.Add(toolBar);
        Controls.Add(topPanel);
        Controls.Add(statusBar);
    }

    private static Button MakeBtn(string text, Color back)
    {
        var b = new Button
        {
            Text      = text,
            Size      = new Size(110, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 9),
            Cursor    = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private async Task LoadTicketsAsync()
    {
        lblStatus.Text    = "Loading…";
        dgvTickets.Rows.Clear();

        var (ok, error, data) = await _api.GetTicketsAsync();
        if (!ok) { lblStatus.Text = $"Error: {error}"; return; }

        foreach (var t in data!)
        {
            dgvTickets.Rows.Add(
                t.TicketNumber,
                t.Subject,
                t.Priority,
                t.Status,
                t.CreatedAt.ToLocalTime().ToString("dd MMM yyyy  HH:mm"),
                t.AssignedToName ?? "—");
            dgvTickets.Rows[^1].Tag = t;
        }

        lblStatus.Text = $"{data.Count} ticket(s) loaded.  Last refreshed: {DateTime.Now:HH:mm:ss}";
    }

    private void DgvTickets_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (dgvTickets.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
        {
            e.CellStyle.ForeColor = e.Value.ToString() switch
            {
                "Open"        => Color.FromArgb(5, 150, 105),
                "In Progress" => Color.FromArgb(217, 119, 6),
                "Closed"      => Color.FromArgb(107, 114, 128),
                _             => e.CellStyle.ForeColor
            };
            e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        }

        if (dgvTickets.Columns[e.ColumnIndex].Name == "Priority" && e.Value != null)
        {
            e.CellStyle.ForeColor = e.Value.ToString() switch
            {
                "High"   => Color.Crimson,
                "Medium" => Color.DarkOrange,
                "Low"    => Color.SteelBlue,
                _        => e.CellStyle.ForeColor
            };
        }
    }

    private void OpenDetails()
    {
        if (dgvTickets.SelectedRows.Count == 0) return;
        var ticket = (TicketListItem)dgvTickets.SelectedRows[0].Tag!;
        var form = new TicketDetailForm(_api, ticket.Id);
        form.FormClosed += async (_, _) => await LoadTicketsAsync();
        form.ShowDialog(this);
    }
}
