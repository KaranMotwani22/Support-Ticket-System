using SupportTicketDesktop.Services;

namespace SupportTicketDesktop.Forms;

public class LoginForm : Form
{
    private readonly ApiClient _api;

    // Controls
    private TextBox    txtUsername = null!;
    private TextBox    txtPassword = null!;
    private Button     btnLogin    = null!;
    private Label      lblStatus   = null!;

    public LoginForm(ApiClient api)
    {
        _api = api;
        InitUI();
    }

    private void InitUI()
    {
        Text            = "Support Ticket System – Login";
        Size            = new Size(400, 300);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        BackColor       = Color.FromArgb(245, 247, 250);

        var pnl = new Panel
        {
            Size      = new Size(320, 220),
            Location  = new Point(40, 30),
            BackColor = Color.White
        };
        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(200, 200, 210));
            e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
        };

        var lblTitle = new Label
        {
            Text      = "🎫  Support Ticket System",
            Font      = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            AutoSize  = true,
            Location  = new Point(20, 18)
        };

        var lblUser = new Label { Text = "Username", Location = new Point(20, 60), AutoSize = true };
        txtUsername = new TextBox
        {
            Location  = new Point(20, 80),
            Size      = new Size(280, 24),
            Font      = new Font("Segoe UI", 10)
        };

        var lblPass = new Label { Text = "Password", Location = new Point(20, 115), AutoSize = true };
        txtPassword = new TextBox
        {
            Location      = new Point(20, 135),
            Size          = new Size(280, 24),
            Font          = new Font("Segoe UI", 10),
            UseSystemPasswordChar = true
        };

        btnLogin = new Button
        {
            Text      = "Login",
            Location  = new Point(20, 170),
            Size      = new Size(280, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        lblStatus = new Label
        {
            Text      = "",
            ForeColor = Color.Crimson,
            AutoSize  = true,
            Location  = new Point(40, 258),
            Font      = new Font("Segoe UI", 9)
        };

        pnl.Controls.AddRange(new Control[] { lblTitle, lblUser, txtUsername, lblPass, txtPassword, btnLogin });
        Controls.AddRange(new Control[] { pnl, lblStatus });

        AcceptButton = btnLogin;
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblStatus.Text = "";

        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        { lblStatus.Text = "Username is required."; return; }
        if (string.IsNullOrWhiteSpace(txtPassword.Text))
        { lblStatus.Text = "Password is required."; return; }

        btnLogin.Enabled = false;
        btnLogin.Text    = "Logging in…";

        var (ok, error, _) = await _api.LoginAsync(txtUsername.Text.Trim(), txtPassword.Text);

        if (ok)
        {
            var main = new MainForm(_api);
            main.Show();
            Hide();
        }
        else
        {
            lblStatus.Text   = error ?? "Login failed.";
            btnLogin.Enabled = true;
            btnLogin.Text    = "Login";
        }
    }
}
