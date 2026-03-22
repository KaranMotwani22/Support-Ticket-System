using SupportTicketDesktop.Services;

namespace SupportTicketDesktop.Forms;

public class CreateTicketForm : Form
{
    private readonly ApiClient _api;

    private TextBox  txtSubject     = null!;
    private RichTextBox txtDesc     = null!;
    private ComboBox cmbPriority    = null!;
    private Button   btnSubmit      = null!;
    private Button   btnCancel      = null!;
    private Label    lblStatus      = null!;

    public CreateTicketForm(ApiClient api)
    {
        _api = api;
        InitUI();
    }

    private void InitUI()
    {
        Text            = "Create New Support Ticket";
        Size            = new Size(520, 420);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        BackColor       = Color.White;

        int y = 20;

        var lblSub = new Label { Text = "Subject *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        y += 22;
        txtSubject = new TextBox { Location = new Point(20, y), Size = new Size(460, 26), Font = new Font("Segoe UI", 10) };
        y += 38;

        var lblDesc = new Label { Text = "Description *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        y += 22;
        txtDesc = new RichTextBox
        {
            Location = new Point(20, y),
            Size     = new Size(460, 140),
            Font     = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        y += 152;

        var lblPri = new Label { Text = "Priority *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        y += 22;
        cmbPriority = new ComboBox
        {
            Location      = new Point(20, y),
            Size          = new Size(200, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Segoe UI", 10)
        };
        cmbPriority.Items.AddRange(new object[] { "Low", "Medium", "High" });
        cmbPriority.SelectedIndex = 1;
        y += 40;

        btnSubmit = new Button
        {
            Text      = "Submit Ticket",
            Location  = new Point(20, y),
            Size      = new Size(140, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        btnSubmit.FlatAppearance.BorderSize = 0;
        btnSubmit.Click += BtnSubmit_Click;

        btnCancel = new Button
        {
            Text      = "Cancel",
            Location  = new Point(172, y),
            Size      = new Size(90, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(220, 220, 220),
            ForeColor = Color.Black,
            Font      = new Font("Segoe UI", 10),
            Cursor    = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => Close();

        lblStatus = new Label
        {
            Text      = "",
            ForeColor = Color.Crimson,
            AutoSize  = true,
            Location  = new Point(20, y + 42),
            Font      = new Font("Segoe UI", 9)
        };

        Controls.AddRange(new Control[]
        {
            lblSub, txtSubject, lblDesc, txtDesc,
            lblPri, cmbPriority, btnSubmit, btnCancel, lblStatus
        });
    }

    private async void BtnSubmit_Click(object? sender, EventArgs e)
    {
        lblStatus.Text = "";

        if (string.IsNullOrWhiteSpace(txtSubject.Text))   { lblStatus.Text = "Subject is required.";     return; }
        if (string.IsNullOrWhiteSpace(txtDesc.Text))      { lblStatus.Text = "Description is required."; return; }

        btnSubmit.Enabled = false;
        btnSubmit.Text    = "Submitting…";

        var (ok, error) = await _api.CreateTicketAsync(
            txtSubject.Text.Trim(),
            txtDesc.Text.Trim(),
            cmbPriority.SelectedItem!.ToString()!);

        if (ok)
        {
            MessageBox.Show("Ticket submitted successfully!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        else
        {
            lblStatus.Text    = error ?? "Failed to submit ticket.";
            btnSubmit.Enabled = true;
            btnSubmit.Text    = "Submit Ticket";
        }
    }
}
