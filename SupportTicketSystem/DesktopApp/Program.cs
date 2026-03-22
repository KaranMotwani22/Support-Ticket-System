using SupportTicketDesktop.Forms;
using SupportTicketDesktop.Services;

namespace SupportTicketDesktop;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var api = new ApiClient();
        Application.Run(new LoginForm(api));
    }
}
