using SupportTicketDesktop.Models;
using SupportTicketDesktop.Services;

namespace SupportTicketDesktop.Forms;

public class TicketDetailForm : Form
{
    private readonly ApiClient _api;
    private readonly int _ticketId;
    private TicketDetailResponse? _ticket;

    // Info labels
    private Label lblTicketNumber = null!;
    private Label lblSubject      = null!;
    private Label lblPriority     = null!;
    private Label lblStatus       = null!;
    private Label lblCreatedAt    = null!;
    private Label lblAssignedTo   = null!;
    private Label lblCreatedBy    = null!;
    private RichTextBox txtDescription = null!;

    // Admin controls
    private Panel    pnlAdminActions = null!;
    private ComboBox cmbAssignTo     = null!;
    private Button   btnAssign       = null!;
    private ComboBox cmbStatus       = null!;
    private TextBox  txtStatusNote   = null!;
    private Button   btnUpdateStatus = null!;

    // Comments / History
    private TabControl  tabBottom      = null!;
    private DataGridView dgvHistory    = null!;
    private DataGridView dgvComments   = null!;
    private RichTextBox  txtNewComment = null!;
    private CheckBox     chkInternal   = null!;
    private Button       btnAddComment = null!;

    private Label lblError = null!;

    public TicketDetailForm(ApiClient api, int ticketId)
    {
        _api      = api;
        _ticketId = ticketId;
        InitUI();
        _ = LoadAsync();
    }

    private void InitUI()
    {
        Text            = "Ticket Details";
        Size            = new Size(860, 720);
        StartPosition   = FormStartPosition.CenterParent;
        MinimumSize     = new Size(700, 600);
        BackColor       = Color.FromArgb(245, 247, 250);

        // ── Scrollable info panel ─────────────────────────────────
        var pnlInfo = new Panel
        {
            Dock        = DockStyle.Top,
            Height      = 230,
            BackColor   = Color.White,
            Padding     = new Padding(16)
        };
        pnlInfo.Paint += PaintBorder;

        int lx = 16, rx = 420, y = 12;

        lblTicketNumber = InfoLabel("", lx, y, bold: true, size: 14, color: Color.FromArgb(37, 99, 235));
        y += 30;

        lblSubject = InfoLabel("", lx, y, bold: true, size: 12);
        y += 28;

        // Row: Priority | Status | Created
        var lblPriLbl = InfoLabel("Priority:", lx, y, bold: true, color: Color.Gray);
        lblPriority   = InfoLabel("", lx + 60, y);
        var lblStLbl  = InfoLabel("Status:", lx + 160, y, bold: true, color: Color.Gray);
        lblStatus     = InfoLabel("", lx + 220, y);
        var lblDtLbl  = InfoLabel("Created:", lx + 340, y, bold: true, color: Color.Gray);
        lblCreatedAt  = InfoLabel("", lx + 405, y);
        y += 24;

        var lblByLbl  = InfoLabel("Submitted by:", lx, y, bold: true, color: Color.Gray);
        lblCreatedBy  = InfoLabel("", lx + 100, y);
        var lblAsgLbl = InfoLabel("Assigned to:", lx + 260, y, bold: true, color: Color.Gray);
        lblAssignedTo = InfoLabel("", lx + 355, y);
        y += 28;

        var lblDescLbl = new Label { Text = "Description:", Location = new Point(lx, y), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Gray };
        y += 20;

        txtDescription = new RichTextBox
        {
            Location    = new Point(lx, y),
            Size        = new Size(800, 72),
            ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White,
            Font        = new Font("Segoe UI", 9.5f),
            ScrollBars  = RichTextBoxScrollBars.Vertical
        };

        pnlInfo.Controls.AddRange(new Control[]
        {
            lblTicketNumber, lblSubject,
            lblPriLbl, lblPriority, lblStLbl, lblStatus, lblDtLbl, lblCreatedAt,
            lblByLbl, lblCreatedBy, lblAsgLbl, lblAssignedTo,
            lblDescLbl, txtDescription
        });

        // ── Admin actions panel ───────────────────────────────────
        pnlAdminActions = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 90,
            BackColor = Color.FromArgb(239, 246, 255),
            Padding   = new Padding(12, 8, 12, 8),
            Visible   = ApiClient.IsAdmin
        };
        pnlAdminActions.Paint += PaintBorder;

        var lblAdminHdr = new Label
        {
            Text      = "Admin Actions",
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            AutoSize  = true,
            Location  = new Point(12, 8)
        };

        // Assign section
        var lblAsgn = new Label { Text = "Assign To:", Location = new Point(12, 34), AutoSize = true };
        cmbAssignTo = new ComboBox
        {
            Location      = new Point(80, 30),
            Size          = new Size(170, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Segoe UI", 9)
        };
        btnAssign = SmallBtn("Assign", Color.FromArgb(16, 185, 129), new Point(256, 30));
        btnAssign.Click += BtnAssign_Click;

        // Status section
        var lblStat = new Label { Text = "Status:", Location = new Point(360, 34), AutoSize = true };
        cmbStatus = new ComboBox
        {
            Location      = new Point(410, 30),
            Size          = new Size(130, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Segoe UI", 9)
        };
        cmbStatus.Items.AddRange(new object[] { "In Progress", "Closed" });

        var lblNote = new Label { Text = "Note:", Location = new Point(550, 34), AutoSize = true };
        txtStatusNote = new TextBox
        {
            Location = new Point(590, 30),
            Size     = new Size(130, 26),
            Font     = new Font("Segoe UI", 9),
            PlaceholderText = "optional note"
        };
        btnUpdateStatus = SmallBtn("Update", Color.FromArgb(217, 119, 6), new Point(730, 30));
        btnUpdateStatus.Click += BtnUpdateStatus_Click;

        pnlAdminActions.Controls.AddRange(new Control[]
        {
            lblAdminHdr, lblAsgn, cmbAssignTo, btnAssign,
            lblStat, cmbStatus, lblNote, txtStatusNote, btnUpdateStatus
        });

        // ── Tabs: History + Comments ──────────────────────────────
        tabBottom = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f)
        };

        var tabHistory  = new TabPage("📋  Status History");
        var tabComments = new TabPage("💬  Comments");

        // History grid
        dgvHistory = MakeGrid();
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",   HeaderText = "Date / Time",    FillWeight = 20 });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "From",   HeaderText = "From",           FillWeight = 15 });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "To",     HeaderText = "To",             FillWeight = 15 });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "By",     HeaderText = "Changed By",     FillWeight = 20 });
        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes",  HeaderText = "Notes",          FillWeight = 30 });
        tabHistory.Controls.Add(dgvHistory);

        // Comments tab
        var pnlComments = new Panel { Dock = DockStyle.Fill };
        dgvComments = MakeGrid();
        dgvComments.Dock = DockStyle.Fill;
        dgvComments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date",    HeaderText = "Date",         FillWeight = 18 });
        dgvComments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Author",  HeaderText = "Author",       FillWeight = 18 });
        dgvComments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type",    HeaderText = "Type",         FillWeight = 10 });
        dgvComments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Comment", HeaderText = "Comment",      FillWeight = 54 });

        var pnlAddComment = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 96,
            BackColor = Color.White,
            Padding   = new Padding(8)
        };
        pnlAddComment.Paint += PaintBorder;

        var lblNewCmt = new Label { Text = "Add Comment:", Location = new Point(8, 8), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        txtNewComment = new RichTextBox
        {
            Location    = new Point(8, 28),
            Size        = new Size(560, 56),
            Font        = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars  = RichTextBoxScrollBars.Vertical
        };

        chkInternal = new CheckBox
        {
            Text     = "Internal note\n(Admin only)",
            Location = new Point(580, 28),
            AutoSize = true,
            Font     = new Font("Segoe UI", 8.5f),
            Visible  = ApiClient.IsAdmin
        };

        btnAddComment = new Button
        {
            Text      = "Post",
            Location  = new Point(726, 28),
            Size      = new Size(72, 56),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        btnAddComment.FlatAppearance.BorderSize = 0;
        btnAddComment.Click += BtnAddComment_Click;

        pnlAddComment.Controls.AddRange(new Control[] { lblNewCmt, txtNewComment, chkInternal, btnAddComment });
        pnlComments.Controls.Add(dgvComments);
        pnlComments.Controls.Add(pnlAddComment);
        tabComments.Controls.Add(pnlComments);

        tabBottom.TabPages.Add(tabHistory);
        tabBottom.TabPages.Add(tabComments);

        // ── Error label ────────────────────────────────────────────
        lblError = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 22,
            ForeColor = Color.Crimson,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0),
            Font      = new Font("Segoe UI", 8.5f)
        };

        // ── Add to form ────────────────────────────────────────────
        Controls.Add(tabBottom);
        Controls.Add(pnlAdminActions);
        Controls.Add(pnlInfo);
        Controls.Add(lblError);
    }

    // ── helpers ────────────────────────────────────────────────
    private static Label InfoLabel(string text, int x, int y,
        bool bold = false, float size = 9.5f,
        Color? color = null)
    {
        return new Label
        {
            Text      = text,
            Location  = new Point(x, y),
            AutoSize  = true,
            Font      = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color ?? Color.FromArgb(30, 30, 30)
        };
    }

    private static Button SmallBtn(string text, Color back, Point loc)
    {
        var b = new Button
        {
            Text      = text,
            Location  = loc,
            Size      = new Size(78, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 8.5f),
            Cursor    = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private static DataGridView MakeGrid()
    {
        var g = new DataGridView
        {
            Dock                    = DockStyle.Fill,
            ReadOnly                = true,
            AllowUserToAddRows      = false,
            AllowUserToDeleteRows   = false,
            SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect             = false,
            RowHeadersVisible       = false,
            BackgroundColor         = Color.White,
            BorderStyle             = BorderStyle.None,
            AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight     = 32,
            Font                    = new Font("Segoe UI", 9)
        };
        g.RowTemplate.Height = 26;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        g.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9, FontStyle.Bold);
        return g;
    }

    private static void PaintBorder(object? s, PaintEventArgs e)
    {
        if (s is not Control c) return;
        using var pen = new Pen(Color.FromArgb(220, 220, 230));
        e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
    }

    // ── Load data ─────────────────────────────────────────────
    private async Task LoadAsync()
    {
        var (ok, error, data) = await _api.GetTicketDetailAsync(_ticketId);
        if (!ok || data == null)
        {
            lblError.Text = $"Failed to load ticket: {error}";
            return;
        }

        _ticket = data;
        PopulateInfo();
        PopulateHistory();
        PopulateComments();

        if (ApiClient.IsAdmin)
            await LoadAdminDropdownsAsync();
    }

    private void PopulateInfo()
    {
        if (_ticket == null) return;

        lblTicketNumber.Text = _ticket.TicketNumber;
        lblSubject.Text      = _ticket.Subject;
        lblPriority.Text     = _ticket.Priority;
        lblStatus.Text       = _ticket.Status;
        lblCreatedAt.Text    = _ticket.CreatedAt.ToLocalTime().ToString("dd MMM yyyy  HH:mm");
        lblCreatedBy.Text    = _ticket.CreatedByName;
        lblAssignedTo.Text   = _ticket.AssignedToName ?? "Unassigned";
        txtDescription.Text  = _ticket.Description;

        lblStatus.ForeColor = _ticket.Status switch
        {
            "Open"        => Color.FromArgb(5, 150, 105),
            "In Progress" => Color.FromArgb(217, 119, 6),
            "Closed"      => Color.FromArgb(107, 114, 128),
            _             => Color.Black
        };

        lblPriority.ForeColor = _ticket.Priority switch
        {
            "High"   => Color.Crimson,
            "Medium" => Color.DarkOrange,
            "Low"    => Color.SteelBlue,
            _        => Color.Black
        };

        // Disable comment / admin controls for closed tickets
        bool closed = _ticket.Status == "Closed";
        btnAddComment.Enabled    = !closed;
        btnAssign.Enabled        = !closed;
        btnUpdateStatus.Enabled  = !closed;
        txtNewComment.Enabled    = !closed;

        Text = $"Ticket – {_ticket.TicketNumber}: {_ticket.Subject}";
    }

    private void PopulateHistory()
    {
        dgvHistory.Rows.Clear();
        foreach (var h in _ticket!.History)
        {
            dgvHistory.Rows.Add(
                h.ChangedAt.ToLocalTime().ToString("dd MMM yyyy  HH:mm"),
                h.OldStatus ?? "—",
                h.NewStatus,
                h.ChangedByName,
                h.Notes ?? "");
        }
    }

    private void PopulateComments()
    {
        dgvComments.Rows.Clear();
        foreach (var c in _ticket!.Comments)
        {
            var typeTag = c.IsInternal ? "🔒 Internal" : "💬 Public";
            dgvComments.Rows.Add(
                c.CreatedAt.ToLocalTime().ToString("dd MMM yyyy  HH:mm"),
                $"{c.AuthorName} ({c.AuthorRole})",
                typeTag,
                c.CommentText);

            if (c.IsInternal)
                dgvComments.Rows[^1].DefaultCellStyle.BackColor = Color.FromArgb(254, 249, 195);
        }
    }

    private async Task LoadAdminDropdownsAsync()
    {
        var (ok, _, admins) = await _api.GetAdminsAsync();
        if (!ok || admins == null) return;

        cmbAssignTo.Items.Clear();
        cmbAssignTo.Items.Add(new Models.AdminUserDto { Id = 0, FullName = "— Unassigned —" });
        foreach (var a in admins)
            cmbAssignTo.Items.Add(a);

        // Pre-select current assignee
        if (_ticket?.AssignedToName != null)
        {
            foreach (Models.AdminUserDto item in cmbAssignTo.Items)
            {
                if (item.FullName == _ticket.AssignedToName)
                {
                    cmbAssignTo.SelectedItem = item;
                    break;
                }
            }
        }
        else cmbAssignTo.SelectedIndex = 0;

        // Status dropdown: only show valid next statuses
        cmbStatus.Items.Clear();
        if (_ticket?.Status == "Open")        cmbStatus.Items.Add("In Progress");
        if (_ticket?.Status == "In Progress") cmbStatus.Items.Add("Closed");
        if (cmbStatus.Items.Count > 0)        cmbStatus.SelectedIndex = 0;
    }

    // ── Admin button handlers ──────────────────────────────────
    private async void BtnAssign_Click(object? sender, EventArgs e)
    {
        if (cmbAssignTo.SelectedItem is not Models.AdminUserDto selected) return;

        btnAssign.Enabled = false;
        var userId = selected.Id == 0 ? (int?)null : selected.Id;
        var (ok, error) = await _api.AssignTicketAsync(_ticketId, userId);

        if (ok)
        {
            lblError.ForeColor = Color.Green;
            lblError.Text      = "Ticket assigned successfully.";
            await LoadAsync();
        }
        else
        {
            lblError.ForeColor = Color.Crimson;
            lblError.Text      = error ?? "Assignment failed.";
        }
        btnAssign.Enabled = true;
    }

    private async void BtnUpdateStatus_Click(object? sender, EventArgs e)
    {
        if (cmbStatus.SelectedItem == null) { lblError.Text = "Select a status."; return; }

        btnUpdateStatus.Enabled = false;
        var newStatus = cmbStatus.SelectedItem.ToString()!;
        var note      = txtStatusNote.Text.Trim();

        var (ok, error) = await _api.UpdateStatusAsync(_ticketId, newStatus, note.Length > 0 ? note : null);
        if (ok)
        {
            lblError.ForeColor = Color.Green;
            lblError.Text      = $"Status updated to '{newStatus}'.";
            txtStatusNote.Clear();
            await LoadAsync();
        }
        else
        {
            lblError.ForeColor = Color.Crimson;
            lblError.Text      = error ?? "Status update failed.";
        }
        btnUpdateStatus.Enabled = true;
    }

    private async void BtnAddComment_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNewComment.Text))
        { lblError.ForeColor = Color.Crimson; lblError.Text = "Comment cannot be empty."; return; }

        btnAddComment.Enabled = false;
        var (ok, error) = await _api.AddCommentAsync(
            _ticketId,
            txtNewComment.Text.Trim(),
            chkInternal.Checked);

        if (ok)
        {
            lblError.ForeColor = Color.Green;
            lblError.Text      = "Comment added.";
            txtNewComment.Clear();
            chkInternal.Checked = false;
            await LoadAsync();
            tabBottom.SelectedIndex = 1; // switch to Comments tab
        }
        else
        {
            lblError.ForeColor = Color.Crimson;
            lblError.Text      = error ?? "Failed to add comment.";
        }
        btnAddComment.Enabled = true;
    }
}
